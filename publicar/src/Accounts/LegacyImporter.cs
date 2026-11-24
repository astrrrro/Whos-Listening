using System;
using System.Collections.Generic;
using System.IO;

namespace WhosListening
{
    public static class LegacyImporter
    {
        public static List<Account> Read(string accountProfilesDir)
        {
            List<Account> result = new List<Account>();
            if (string.IsNullOrEmpty(accountProfilesDir) || !Directory.Exists(accountProfilesDir)) return result;

            string[] dirs = Directory.GetDirectories(accountProfilesDir);
            Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
            int index = 0;
            foreach (string dir in dirs)
            {
                Account a = ReadOne(dir, index);
                if (a == null) continue;
                result.Add(a);
                index++;
            }
            return result;
        }

        private static Account ReadOne(string dir, int order)
        {
            try
            {
                string metaPath = Path.Combine(dir, "meta.json");
                string authPath = Path.Combine(dir, "auth.json");
                if (!File.Exists(metaPath) || !File.Exists(authPath)) return null;

                Dictionary<string, object> meta = Json.Deserialize<Dictionary<string, object>>(File.ReadAllText(metaPath));
                Dictionary<string, object> auth = Json.Deserialize<Dictionary<string, object>>(File.ReadAllText(authPath));
                if (meta == null || auth == null) return null;

                string label = AsString(meta, "label");
                string username = AsString(meta, "username");
                if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(username)) return null;

                Account a = new Account();
                a.Id = Guid.NewGuid().ToString("N");
                a.Label = label;
                a.Username = username;
                a.Order = order;
                a.SavedAtUtc = DateTime.UtcNow;
                foreach (string key in SpotifyPrefs.AuthKeys)
                {
                    string v = AsString(auth, key);
                    if (!string.IsNullOrEmpty(v)) a.Auth[key] = v;
                }
                if (Unquote(a.Auth.ContainsKey("autologin.username") ? a.Auth["autologin.username"] : null).Length == 0) return null;
                bool hasBlob = a.Auth.ContainsKey("autologin.blob") && Unquote(a.Auth["autologin.blob"]).Length > 0;
                bool hasCred = a.Auth.ContainsKey("autologin.saved_credentials") && Unquote(a.Auth["autologin.saved_credentials"]).Length > 0;
                if (!hasBlob && !hasCred) return null;
                return a;
            }
            catch (Exception ex)
            {
                // Sin ex.Message: el error del lector JSON puede traer trozos de credenciales.
                Log.Error("importar " + Path.GetFileName(dir) + ": " + ex.GetType().Name, null);
                return null;
            }
        }

        private static string Unquote(string v)
        {
            if (v == null) return "";
            return v.Trim().Trim('"').Trim();
        }

        private static string AsString(Dictionary<string, object> d, string key)
        {
            object v;
            if (!d.TryGetValue(key, out v) || v == null) return null;
            return v.ToString();
        }
    }
}
