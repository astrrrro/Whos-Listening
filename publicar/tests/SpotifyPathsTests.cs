using System;
using System.IO;

namespace WhosListening
{
    public static class SpotifyPathsTests
    {
        public static void TestDetectsMissingAndStore()
        {
            SpotifyPaths paths = SpotifyPaths.Current;
            Assert.True(!paths.IsInstalled, "sin exe no esta instalado");
            Assert.True(!paths.IsStoreVersion, "sin carpeta Packages no es Store");

            Directory.CreateDirectory(Path.Combine(AppPaths.LocalAppData, Path.Combine("Packages", "SpotifyAB.SpotifyMusic_zpdnekdrzrea0")));
            Assert.True(paths.IsStoreVersion, "con paquete es Store");
            Assert.True(!paths.IsInstalled, "Store no es instalado clasico");

            Directory.CreateDirectory(paths.Dir);
            File.WriteAllBytes(paths.Exe, new byte[0]);
            Assert.True(paths.IsInstalled, "con exe esta instalado");
            Assert.True(!paths.IsStoreVersion, "con exe ya no es Store");
        }

        public static void TestUnsafeUsernamesRejected()
        {
            SpotifyPaths p = new SpotifyPaths(@"C:\A", @"C:\L");
            string[] bad = new string[] { "", "..", "a\\b", "C:\\x", "a/b" };
            foreach (string u in bad)
            {
                string name = u;
                Assert.True(!SpotifyPaths.IsSafeUsername(name), "inseguro: " + name);
                Assert.Throws<ArgumentException>(delegate() { p.UserCacheDir(name); });
            }
            Assert.True(SpotifyPaths.IsSafeUsername("31abc"), "seguro");
            Assert.Equal(Path.Combine(p.UsersDir, "31abc-user"), p.UserCacheDir("31abc"), "ruta");
        }

        public static void TestPathLayout()
        {
            SpotifyPaths p = new SpotifyPaths(@"C:\A", @"C:\L");
            Assert.Equal(Path.Combine(Path.Combine(@"C:\A", "Spotify"), "prefs"), p.PrefsFile, "prefs");
            Assert.Equal(Path.Combine(Path.Combine(@"C:\L", "Spotify"), "dbrts"), p.DbrtsFile, "dbrts");
            Assert.Equal(Path.Combine(p.UsersDir, "bob-user"), p.UserCacheDir("bob"), "cache");
        }
    }
}
