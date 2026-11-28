using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WhosListening
{
    public static class DiagnosticsTests
    {
        public static void TestNoSecretLonger8()
        {
            string blob = "QWxhZGRpbjpvcGVuIHNlc2FtZTEyMzQ1Njc4OTA=";
            Assert.Equal(40, blob.Length, "largo del blob");

            SpotifyPaths paths = new SpotifyPaths(AppPaths.AppData, AppPaths.LocalAppData);
            Directory.CreateDirectory(Path.GetDirectoryName(paths.PrefsFile));
            File.WriteAllBytes(paths.PrefsFile, new UTF8Encoding(false).GetBytes("autologin.blob=\"" + blob + "\"\n"));

            AccountStore store = new AccountStore(AppPaths.AccountsDir);
            Dictionary<string, string> auth = new Dictionary<string, string>();
            auth["autologin.blob"] = blob;
            store.Add("Cuenta", "usuario", auth);
            string user = "31abcdefghijklmnopqrstuvwxyz";
            Assert.Equal(28, user.Length, "largo del usuario");
            store.Add("Otra", user, new Dictionary<string, string>());

            string report = Diagnostics.BuildReport(paths, store, "0.0-test");
            for (int i = 0; i + 9 <= blob.Length; i++)
            {
                string piece = blob.Substring(i, 9);
                Assert.True(!report.Contains(piece), "el informe contiene un fragmento de 9 caracteres: " + piece);
            }
            Assert.True(report.Contains(blob.Substring(0, 8)), "falta el prefijo de 8 caracteres");

            for (int i = 0; i + 9 <= user.Length; i++)
            {
                string piece = user.Substring(i, 9);
                Assert.True(!report.Contains(piece), "el informe contiene un fragmento del usuario: " + piece);
            }
            Assert.True(report.Contains(user.Substring(0, 8)), "falta el prefijo del usuario");
        }

        public static void TestLabelsAreRedacted()
        {
            SpotifyPaths paths = new SpotifyPaths(AppPaths.AppData, AppPaths.LocalAppData);
            AccountStore store = new AccountStore(AppPaths.AccountsDir);
            store.Add("Mamá", "usuario1", new Dictionary<string, string>());
            store.Add("Pedro Pablo Perez", "usuario2", new Dictionary<string, string>());

            string report = Diagnostics.BuildReport(paths, store, "0.0-test");
            Assert.True(!report.Contains("Mamá"), "nombre corto visible");
            Assert.True(!report.Contains("Pedro Pablo"), "nombre largo visible");
            Assert.True(report.Contains("Pedro Pa"), "falta el prefijo de 8 del nombre largo");
        }

        public static void TestFormatWindows()
        {
            Assert.Equal("Windows 11 Pro 23H2 (build 22631)", Diagnostics.FormatWindows("Windows 10 Pro", "23H2", "22631", "fb"), "win11");
            Assert.Equal("Windows 10 Home 22H2 (build 19045)", Diagnostics.FormatWindows("Windows 10 Home", "22H2", "19045", "fb"), "win10");
            Assert.Equal("Windows 10 Pro (build 19041)", Diagnostics.FormatWindows("Windows 10 Pro", null, "19041", "fb"), "sin DisplayVersion");
            Assert.Equal("Windows 10 Pro", Diagnostics.FormatWindows("Windows 10 Pro", null, null, "fb"), "sin build");
            Assert.Equal("Windows 11 (build 22000)", Diagnostics.FormatWindows(null, null, "22000", "fb"), "sin ProductName");
            Assert.Equal("fb", Diagnostics.FormatWindows(null, null, null, "fb"), "nada: respaldo");
            Assert.True(Diagnostics.WindowsVersion().Length > 0, "lee algo en este PC");
        }
    }
}
