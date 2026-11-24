using System.IO;

namespace WhosListening
{
    public static class SettingsTests
    {
        public static void TestDefaultsWhenMissingOrCorrupt()
        {
            Settings s = Settings.Load();
            Assert.True(s.ShowOnSpotifyOpen, "por defecto muestra el selector");
            Assert.True(!s.LegacyImportOffered, "por defecto no se ofreció importar");

            Directory.CreateDirectory(AppPaths.DataDir);
            File.WriteAllText(AppPaths.SettingsFile, "{ esto no es json");
            Settings c = Settings.Load();
            Assert.True(c.ShowOnSpotifyOpen, "corrupto: muestra el selector");
            Assert.True(!c.LegacyImportOffered, "corrupto: no se ofreció importar");

            c.ShowOnSpotifyOpen = false;
            c.LegacyImportOffered = true;
            c.Save();
            Settings r = Settings.Load();
            Assert.True(!r.ShowOnSpotifyOpen, "se guardó ShowOnSpotifyOpen");
            Assert.True(r.LegacyImportOffered, "se guardó LegacyImportOffered");
        }
    }
}
