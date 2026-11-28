using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WhosListening
{
    public static class SpotifyPrefsTests
    {
        private static string PrefsPath()
        {
            Directory.CreateDirectory(AppPaths.AppData);
            return Path.Combine(AppPaths.AppData, "prefs");
        }

        public static void TestPreservesCrlfAndUnknownLines()
        {
            string path = PrefsPath();
            File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes("a=1\r\nlinea rara\r\nautologin.username=\"x\"\r\n"));
            SpotifyPrefs p = SpotifyPrefs.Load(path);
            p.Set("a", "2");
            p.Save(path);
            string result = new UTF8Encoding(false).GetString(File.ReadAllBytes(path));
            Assert.Equal("a=2\r\nlinea rara\r\nautologin.username=\"x\"\r\n", result, "contenido");
        }

        public static void TestBomIsIgnoredAndNotWritten()
        {
            string path = PrefsPath();
            byte[] body = new UTF8Encoding(false).GetBytes("a=1\nb=2\n");
            byte[] all = new byte[body.Length + 3];
            all[0] = 0xEF; all[1] = 0xBB; all[2] = 0xBF;
            body.CopyTo(all, 3);
            File.WriteAllBytes(path, all);
            SpotifyPrefs p = SpotifyPrefs.Load(path);
            Assert.Equal("1", p.Get("a"), "Get a");
            p.Save(path);
            byte[] saved = File.ReadAllBytes(path);
            Assert.True(saved[0] != 0xEF, "no debe escribir BOM");
        }

        public static void TestMissingFileGivesEmpty()
        {
            string path = Path.Combine(AppPaths.AppData, "sub", "prefs-nuevo");
            SpotifyPrefs p = SpotifyPrefs.Load(path);
            Assert.True(p.Username == null, "Username debe ser null");
            p.Save(path);
            Assert.True(File.Exists(path), "Save debe crear el archivo");
        }

        public static void TestApplyAuthQuotesAndRemoves()
        {
            string path = PrefsPath();
            File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(
                "x=1\nautologin.username=\"old\"\nautologin.blob=\"zzz\"\nautologin.canonical_username=\"old\"\nautologin.saved_credentials=\"c\"\ny=2\n"));
            SpotifyPrefs p = SpotifyPrefs.Load(path);
            Dictionary<string, string> auth = new Dictionary<string, string>();
            auth["autologin.username"] = "bob";
            auth["autologin.blob"] = "";
            p.ApplyAuth(auth);
            p.Save(path);
            string result = new UTF8Encoding(false).GetString(File.ReadAllBytes(path));
            Assert.Equal("x=1\nautologin.username=\"bob\"\ny=2\n", result, "contenido");
            Assert.Equal("bob", p.Username, "Username");
        }

        public static void TestSaveLeavesNoTmpAndBacksUpOnce()
        {
            string path = PrefsPath();
            string bak = path + ".whoslistening.bak";
            File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes("a=original\n"));

            SpotifyPrefs p = SpotifyPrefs.Load(path);
            p.Set("a", "1");
            p.Save(path);
            Assert.True(!File.Exists(path + ".whoslistening.tmp"), "no queda .tmp");
            Assert.True(File.Exists(bak), "crea .bak");
            Assert.Equal("a=original\n", new UTF8Encoding(false).GetString(File.ReadAllBytes(bak)), "bak con el original");
            Assert.Equal("a=1\n", new UTF8Encoding(false).GetString(File.ReadAllBytes(path)), "prefs guardado");

            p.Set("a", "2");
            p.Save(path);
            Assert.True(!File.Exists(path + ".whoslistening.tmp"), "no queda .tmp (2)");
            Assert.Equal("a=original\n", new UTF8Encoding(false).GetString(File.ReadAllBytes(bak)), "bak no se pisa");
            Assert.Equal("a=2\n", new UTF8Encoding(false).GetString(File.ReadAllBytes(path)), "prefs guardado (2)");
        }

        public static void TestSaveTempNameIsWhosListeningTmpAndNotLeftBehind()
        {
            string path = PrefsPath();
            string tmp = path + ".whoslistening.tmp";
            Assert.Equal(path + ".whoslistening.tmp", tmp, "nombre del temporal");
            Assert.True(!File.Exists(path + ".tmp"), "no se usa prefs.tmp");
            SpotifyPrefs p = new SpotifyPrefs();
            p.Set("a", "1");
            p.Save(path);
            Assert.True(!File.Exists(tmp), "no queda temporal tras guardar nuevo");
            p.Set("a", "2");
            p.Save(path);
            Assert.True(!File.Exists(tmp), "no queda temporal tras reemplazar");
            Assert.True(!File.Exists(path + ".tmp"), "no aparece prefs.tmp");
        }

        public static void TestSaveNewFileMakesNoBackup()
        {
            string path = PrefsPath();
            SpotifyPrefs p = new SpotifyPrefs();
            p.Set("a", "1");
            p.Save(path);
            Assert.True(File.Exists(path), "crea prefs");
            Assert.True(!File.Exists(path + ".whoslistening.tmp"), "no queda .tmp");
            Assert.True(!File.Exists(path + ".whoslistening.bak"), "sin .bak si no habia original");
        }

        public static void TestDuplicateKeysAreCollapsed()
        {
            string path = PrefsPath();
            File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(
                "autologin.blob=\"A\"\nx=1\nautologin.blob=\"B\"\nautologin.username=\"u1\"\ny=2\nautologin.username=\"u2\"\n"));
            SpotifyPrefs p = SpotifyPrefs.Load(path);
            p.Set("autologin.blob", "\"NUEVO\"");
            p.Remove("autologin.username");
            p.Save(path);
            string result = new UTF8Encoding(false).GetString(File.ReadAllBytes(path));
            Assert.Equal("autologin.blob=\"NUEVO\"\nx=1\ny=2\n", result, "contenido");
            Assert.True(p.Username == null, "Username quitado del todo");
        }

        public static void TestValueWithEqualsSign()
        {
            string path = PrefsPath();
            File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes("k=a=b\n"));
            SpotifyPrefs p = SpotifyPrefs.Load(path);
            Assert.Equal("a=b", p.Get("k"), "Get k");
        }
    }
}
