using System;
using System.IO;
using System.Text;

namespace WhosListening
{
    public class Settings
    {
        public Settings()
        {
            ShowOnSpotifyOpen = true;
            LegacyImportOffered = false;
        }

        public bool ShowOnSpotifyOpen { get; set; }
        public bool LegacyImportOffered { get; set; }

        public static Settings Load()
        {
            string file = AppPaths.SettingsFile;
            if (!File.Exists(file)) return new Settings();
            try
            {
                Settings s = Json.Deserialize<Settings>(File.ReadAllText(file, Encoding.UTF8));
                if (s == null) return new Settings();
                return s;
            }
            catch (Exception ex)
            {
                Log.Error("settings.json ilegible, se usan valores por defecto", ex);
                return new Settings();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(AppPaths.DataDir);
            string target = AppPaths.SettingsFile;
            string tmp = target + ".tmp";
            File.WriteAllText(tmp, Json.Serialize(this), new UTF8Encoding(false));
            if (File.Exists(target)) File.Replace(tmp, target, null);
            else File.Move(tmp, target);
        }
    }
}
