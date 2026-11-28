using System.Collections.Generic;
using System.IO;

namespace WhosListening
{
    public static class LegacyImporterTests
    {
        private static void WriteProfile(string root, string folder, string label, string username, bool quoted, bool withAuth)
        {
            string dir = Path.Combine(root, folder);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "meta.json"), "{\"label\":\"" + label + "\",\"username\":\"" + username + "\"}");
            if (!withAuth) return;
            string q = quoted ? "\\\"" : "";
            File.WriteAllText(Path.Combine(dir, "auth.json"),
                "{\"autologin.username\":\"" + q + username + q + "\","
                + "\"autologin.canonical_username\":\"" + q + username + q + "\","
                + "\"autologin.blob\":\"" + q + "BLOB" + q + "\","
                + "\"autologin.saved_credentials\":\"" + q + "CRED" + q + "\"}");
        }

        public static void TestBrokenAuthDoesNotLogContent()
        {
            string root = Path.Combine(AppPaths.AppData, "AccountProfiles");
            string dir = Path.Combine(root, "roto");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "meta.json"), "{\"label\":\"Ana\",\"username\":\"ana\"}");
            File.WriteAllText(Path.Combine(dir, "auth.json"), "{\"autologin.blob\": SECRETOSECRETO123 }");

            List<Account> list = LegacyImporter.Read(root);

            Assert.Equal(0, list.Count, "cuentas");
            string log = File.Exists(AppPaths.LogFile) ? File.ReadAllText(AppPaths.LogFile) : "";
            Assert.True(log.Contains("roto"), "el log nombra la carpeta");
            Assert.True(!log.Contains("SECRETO"), "el log no trae el contenido");
        }

        public static void TestReadsQuotedAndUnquoted()
        {
            string root = Path.Combine(AppPaths.AppData, "AccountProfiles");
            WriteProfile(root, "a-sin-comillas", "Ana", "ana", false, true);
            WriteProfile(root, "b-con-comillas", "Beto", "beto", true, true);
            WriteProfile(root, "c-sin-auth", "Carla", "carla", false, false);

            List<Account> list = LegacyImporter.Read(root);

            Assert.Equal(2, list.Count, "cuentas");
            Assert.Equal("Ana", list[0].Label, "label 0");
            Assert.Equal(0, list[0].Order, "orden 0");
            Assert.Equal("Beto", list[1].Label, "label 1");
            Assert.Equal(1, list[1].Order, "orden 1");
            Assert.True(list[0].Id != list[1].Id && list[0].Id.Length == 32, "ids GUID");

            SpotifyPrefs p1 = new SpotifyPrefs();
            p1.ApplyAuth(list[0].Auth);
            SpotifyPrefs p2 = new SpotifyPrefs();
            p2.ApplyAuth(list[1].Auth);
            Assert.Equal("\"BLOB\"", p1.Get("autologin.blob"), "blob sin comillas");
            Assert.Equal(p1.Get("autologin.blob"), p2.Get("autologin.blob"), "blob igual");
            Assert.Equal(p1.Get("autologin.saved_credentials"), p2.Get("autologin.saved_credentials"), "cred igual");
            Assert.Equal("\"ana\"", p1.Get("autologin.username"), "usuario con comillas");
        }

        public static void TestSkipsAuthWithoutCredentials()
        {
            string root = Path.Combine(AppPaths.AppData, "AccountProfiles");
            WriteProfile(root, "a-vacio", "A", "a", false, true);
            File.WriteAllText(Path.Combine(root, "a-vacio", "auth.json"), "{}");
            WriteProfile(root, "b-solo-usuario", "B", "b", false, true);
            File.WriteAllText(Path.Combine(root, "b-solo-usuario", "auth.json"), "{\"autologin.username\":\"b\"}");
            WriteProfile(root, "c-con-blob", "C", "c", false, true);
            File.WriteAllText(Path.Combine(root, "c-con-blob", "auth.json"), "{\"autologin.username\":\"c\",\"autologin.blob\":\"XYZ\"}");

            List<Account> list = LegacyImporter.Read(root);

            Assert.Equal(1, list.Count, "solo una cuenta");
            Assert.Equal("C", list[0].Label, "la completa");
        }

        public static void TestMissingDirIsEmpty()
        {
            Assert.Equal(0, LegacyImporter.Read(Path.Combine(AppPaths.AppData, "no-existe")).Count, "vacio");
        }

        public static void TestSkipsBrokenAndIncomplete()
        {
            string root = Path.Combine(AppPaths.AppData, "AccountProfiles");
            WriteProfile(root, "a", "", "x", false, true);
            Directory.CreateDirectory(Path.Combine(root, "b"));
            File.WriteAllText(Path.Combine(root, "b", "meta.json"), "no es json");
            File.WriteAllText(Path.Combine(root, "b", "auth.json"), "{}");
            Assert.Equal(0, LegacyImporter.Read(root).Count, "todas saltadas");
        }
    }
}
