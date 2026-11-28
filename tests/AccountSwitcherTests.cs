using System;
using System.IO;
using System.Text;

namespace WhosListening
{
    public static class AccountSwitcherTests
    {
        private static string Q(string s) { return "\"" + s + "\""; }

        private static SpotifyPaths Setup(string prefsText)
        {
            SpotifyPaths paths = SpotifyPaths.Current;
            Directory.CreateDirectory(paths.Dir);
            File.WriteAllBytes(paths.Exe, new byte[0]);
            File.WriteAllBytes(paths.PrefsFile, new UTF8Encoding(false).GetBytes(prefsText));
            return paths;
        }

        private static string CurrentPrefsText()
        {
            return "a=1\nautologin.username=" + Q("ana") + "\nautologin.canonical_username=" + Q("ana")
                + "\nautologin.blob=" + Q("BLOBANA") + "\nautologin.saved_credentials=" + Q("CREDANA") + "\nz=9\n";
        }

        private static Account Target()
        {
            Account t = new Account();
            t.Id = "t"; t.Label = "Beto"; t.Username = "beto";
            t.Auth["autologin.username"] = "beto";
            t.Auth["autologin.canonical_username"] = "beto";
            t.Auth["autologin.blob"] = "BLOBBETO";
            t.Auth["autologin.saved_credentials"] = "CREDBETO";
            return t;
        }

        public static void TestHasCredentials()
        {
            System.Collections.Generic.Dictionary<string, string> auth = new System.Collections.Generic.Dictionary<string, string>();
            Assert.True(!AccountSwitcher.HasCredentials(null), "null");
            auth["autologin.username"] = Q("ana");
            Assert.True(!AccountSwitcher.HasCredentials(auth), "solo usuario");
            auth["autologin.blob"] = Q("  ");
            auth["autologin.saved_credentials"] = "\"\"";
            Assert.True(!AccountSwitcher.HasCredentials(auth), "comillas vacias");
            auth["autologin.saved_credentials"] = Q("CRED");
            Assert.True(AccountSwitcher.HasCredentials(auth), "saved_credentials");
            auth.Remove("autologin.saved_credentials");
            auth["autologin.blob"] = "BLOB";
            Assert.True(AccountSwitcher.HasCredentials(auth), "blob sin comillas");
        }

        public static void TestSwitchWritesAuthDeletesCacheAndStarts()
        {
            SpotifyPaths paths = Setup(CurrentPrefsText());
            Directory.CreateDirectory(Path.GetDirectoryName(paths.DbrtsFile));
            File.WriteAllText(paths.DbrtsFile, "x");
            string cache = paths.UserCacheDir("beto");
            Directory.CreateDirectory(cache);
            File.WriteAllText(Path.Combine(cache, "f"), "x");
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            proc.Running = true;

            SwitchResult r = new AccountSwitcher(paths, proc, null).SwitchTo(Target(), null);

            Assert.Equal(SwitchResult.Switched, r, "resultado");
            SpotifyPrefs after = SpotifyPrefs.Load(paths.PrefsFile);
            Assert.Equal("beto", after.Username, "usuario");
            Assert.Equal(Q("BLOBBETO"), after.Get("autologin.blob"), "blob");
            Assert.Equal("1", after.Get("a"), "otras lineas intactas");
            Assert.True(!File.Exists(paths.DbrtsFile), "dbrts borrado");
            Assert.True(!Directory.Exists(cache), "cache borrada");
            Assert.Equal(1, proc.CloseCalls, "cierra una vez");
            Assert.Equal(1, proc.StartCalls, "inicia una vez");
            Assert.Equal(paths.Exe, proc.LastStartedExe, "exe");
        }

        public static void TestSwitchAbortsWhenSpotifyWontClose()
        {
            SpotifyPaths paths = Setup(CurrentPrefsText());
            byte[] before = File.ReadAllBytes(paths.PrefsFile);
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            proc.Running = true;
            proc.CloseSucceeds = false;

            SwitchResult r = new AccountSwitcher(paths, proc, null).SwitchTo(Target(), null);

            Assert.Equal(SwitchResult.SpotifyWouldNotClose, r, "resultado");
            byte[] after = File.ReadAllBytes(paths.PrefsFile);
            Assert.Equal(Convert.ToBase64String(before), Convert.ToBase64String(after), "prefs igual");
            Assert.Equal(0, proc.StartCalls, "no inicia");
        }

        public static void TestRefreshesOutgoingAccountBeforeSwitch()
        {
            SpotifyPaths paths = Setup(CurrentPrefsText());
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            Account outgoing = new Account();
            outgoing.Id = "o"; outgoing.Label = "Ana"; outgoing.Username = "ANA";
            outgoing.Auth["autologin.blob"] = "viejo";
            Account refreshed = null;
            AccountSwitcher sw = new AccountSwitcher(paths, proc, delegate(Account a) { refreshed = a; });

            SwitchResult r = sw.SwitchTo(Target(), outgoing);

            Assert.Equal(SwitchResult.Switched, r, "resultado");
            Assert.True(refreshed != null, "se llamo refreshSaved");
            Assert.Equal(Q("BLOBANA"), refreshed.Auth["autologin.blob"], "blob saliente");
            Assert.True(refreshed.SavedAtUtc > DateTime.UtcNow.AddMinutes(-1), "SavedAtUtc actualizado");
        }

        public static void TestOutgoingKeepsOldAuthWhenPrefsHasNoCredentials()
        {
            SpotifyPaths paths = Setup("a=1\nautologin.username=" + Q("ana") + "\nautologin.canonical_username=" + Q("ana") + "\nz=9\n");
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            Account outgoing = new Account();
            outgoing.Id = "o"; outgoing.Label = "Ana"; outgoing.Username = "ana";
            outgoing.Auth["autologin.username"] = Q("ana");
            outgoing.Auth["autologin.blob"] = Q("BUENO");
            DateTime savedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            outgoing.SavedAtUtc = savedAt;
            bool called = false;
            AccountSwitcher sw = new AccountSwitcher(paths, proc, delegate(Account a) { called = true; });

            SwitchResult r = sw.SwitchTo(Target(), outgoing);

            Assert.Equal(SwitchResult.Switched, r, "resultado");
            Assert.True(!called, "no se llama refreshSaved");
            Assert.Equal(Q("BUENO"), outgoing.Auth["autologin.blob"], "blob saliente intacto");
            Assert.Equal(savedAt, outgoing.SavedAtUtc, "SavedAtUtc intacto");
        }

        public static void TestDoesNotOverwriteOtherAccountCredentials()
        {
            SpotifyPaths paths = Setup(CurrentPrefsText());
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            Account other = new Account();
            other.Username = "carla";
            other.Auth["autologin.blob"] = "DECARLA";
            bool called = false;
            AccountSwitcher sw = new AccountSwitcher(paths, proc, delegate(Account a) { called = true; });

            sw.SwitchTo(Target(), other);

            Assert.Equal("DECARLA", other.Auth["autologin.blob"], "no se toca");
            Assert.True(!called, "no se llama refreshSaved");
        }

        public static void TestUnsafeTargetUsernameIsErrorAndTouchesNothing()
        {
            SpotifyPaths paths = Setup(CurrentPrefsText());
            byte[] before = File.ReadAllBytes(paths.PrefsFile);
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            proc.Running = true;
            Account t = Target();
            t.Username = "..\\..\\x";

            SwitchResult r = new AccountSwitcher(paths, proc, null).SwitchTo(t, null);

            Assert.Equal(SwitchResult.Error, r, "resultado");
            Assert.Equal(Convert.ToBase64String(before), Convert.ToBase64String(File.ReadAllBytes(paths.PrefsFile)), "prefs igual");
            Assert.Equal(0, proc.CloseCalls, "no cierra");
            Assert.Equal(0, proc.StartCalls, "no inicia");
        }

        public static void TestTargetWithoutCredentialsIsErrorAndTouchesNothing()
        {
            SpotifyPaths paths = Setup(CurrentPrefsText());
            byte[] before = File.ReadAllBytes(paths.PrefsFile);
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            proc.Running = true;
            Account t = Target();
            t.Auth.Remove("autologin.blob");
            t.Auth["autologin.saved_credentials"] = "\"\"";

            SwitchResult r = new AccountSwitcher(paths, proc, null).SwitchTo(t, null);

            Assert.Equal(SwitchResult.Error, r, "resultado");
            Assert.Equal(Convert.ToBase64String(before), Convert.ToBase64String(File.ReadAllBytes(paths.PrefsFile)), "prefs igual");
            Assert.Equal(0, proc.CloseCalls, "no cierra");
            Assert.Equal(0, proc.StartCalls, "no inicia");
        }

        public static void TestSameAccountRunningIsAlreadyActive()
        {
            SpotifyPaths paths = Setup(CurrentPrefsText());
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            proc.Running = true;
            Account t = Target();
            t.Username = "Ana";

            SwitchResult r = new AccountSwitcher(paths, proc, null).SwitchTo(t, null);

            Assert.Equal(SwitchResult.AlreadyActive, r, "resultado");
            Assert.Equal(0, proc.StartCalls, "no inicia");
            Assert.Equal(0, proc.CloseCalls, "no cierra");
        }

        public static void TestSameAccountNotRunningStarts()
        {
            SpotifyPaths paths = Setup(CurrentPrefsText());
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            Account t = Target();
            t.Username = "ana";

            SwitchResult r = new AccountSwitcher(paths, proc, null).SwitchTo(t, null);

            Assert.Equal(SwitchResult.StartedSameAccount, r, "resultado");
            Assert.Equal(1, proc.StartCalls, "inicia");
        }

        public static void TestNotInstalled()
        {
            SpotifyPaths paths = SpotifyPaths.Current;
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            SwitchResult r = new AccountSwitcher(paths, proc, null).SwitchTo(Target(), null);
            Assert.Equal(SwitchResult.SpotifyNotInstalled, r, "resultado");
            Assert.True(!new AccountSwitcher(paths, proc, null).PrepareNewLogin(), "PrepareNewLogin");
        }

        public static void TestPrepareNewLoginRemovesAuthAndStarts()
        {
            SpotifyPaths paths = Setup(CurrentPrefsText());
            Directory.CreateDirectory(Path.GetDirectoryName(paths.DbrtsFile));
            File.WriteAllText(paths.DbrtsFile, "x");
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            proc.Running = true;

            bool ok = new AccountSwitcher(paths, proc, null).PrepareNewLogin();

            Assert.True(ok, "devuelve true");
            SpotifyPrefs after = SpotifyPrefs.Load(paths.PrefsFile);
            Assert.Equal(0, after.GetAuth().Count, "sin claves de auth");
            Assert.Equal("1", after.Get("a"), "otras lineas intactas");
            Assert.True(!File.Exists(paths.DbrtsFile), "dbrts borrado");
            Assert.Equal(1, proc.StartCalls, "inicia");
        }

        public static void TestPrepareNewLoginStartsEvenIfDbrtsIsLocked()
        {
            SpotifyPaths paths = Setup(CurrentPrefsText());
            Directory.CreateDirectory(Path.GetDirectoryName(paths.DbrtsFile));
            File.WriteAllText(paths.DbrtsFile, "x");
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            bool ok;
            using (FileStream lockIt = new FileStream(paths.DbrtsFile, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                ok = new AccountSwitcher(paths, proc, null).PrepareNewLogin();
            }

            Assert.True(ok, "devuelve true");
            Assert.Equal(0, SpotifyPrefs.Load(paths.PrefsFile).GetAuth().Count, "sin claves de auth");
            Assert.Equal(1, proc.StartCalls, "inicia igual");
        }

        public static void TestPrepareNewLoginFailsWhenWontClose()
        {
            SpotifyPaths paths = Setup(CurrentPrefsText());
            FakeSpotifyProcess proc = new FakeSpotifyProcess();
            proc.Running = true;
            proc.CloseSucceeds = false;
            Assert.True(!new AccountSwitcher(paths, proc, null).PrepareNewLogin(), "devuelve false");
            Assert.Equal(0, proc.StartCalls, "no inicia");
        }
    }
}
