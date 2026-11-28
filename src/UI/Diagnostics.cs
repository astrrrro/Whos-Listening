using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace WhosListening
{
    public static class Diagnostics
    {
        public static string BuildReport(SpotifyPaths p, AccountStore s, string appVersion)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Who's Listening? " + appVersion);
            sb.AppendLine("Windows: " + WindowsVersion());
            sb.AppendLine("Spotify installed: " + YesNo(p.IsInstalled));
            sb.AppendLine("Store version: " + YesNo(p.IsStoreVersion));

            sb.AppendLine("prefs:");
            try
            {
                SpotifyPrefs prefs = SpotifyPrefs.Load(p.PrefsFile);
                AppendAuth(sb, prefs.GetAuth(), "  ");
            }
            catch (Exception ex)
            {
                sb.AppendLine("  (unreadable: " + ex.GetType().Name + ")");
            }

            sb.AppendLine("accounts:");
            List<Account> accounts = new List<Account>();
            try
            {
                accounts = s.LoadAll();
            }
            catch (Exception ex)
            {
                sb.AppendLine("  (unreadable: " + ex.GetType().Name + ")");
            }
            foreach (Account a in accounts)
            {
                sb.AppendLine("  " + Log.Redact(a.Label) + " (" + Log.Redact(a.Username) + ")");
                AppendAuth(sb, a.Auth, "    ");
            }

            sb.AppendLine("unreadable:");
            foreach (string f in s.UnreadableFiles)
            {
                sb.AppendLine("  " + f);
            }
            return sb.ToString();
        }

        // Environment.OSVersion dice 6.2 sin manifiesto, asi que se lee el registro.
        public static string WindowsVersion()
        {
            string product = null, display = null, build = null;
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (k != null)
                    {
                        product = RegString(k, "ProductName");
                        display = RegString(k, "DisplayVersion");
                        build = RegString(k, "CurrentBuild");
                    }
                }
            }
            catch (Exception)
            {
            }
            return FormatWindows(product, display, build, Environment.OSVersion.ToString());
        }

        private static string RegString(RegistryKey k, string name)
        {
            object v = k.GetValue(name);
            if (v == null) return null;
            string s = v.ToString().Trim();
            return s.Length == 0 ? null : s;
        }

        public static string FormatWindows(string product, string display, string build, string fallback)
        {
            int buildNumber = 0;
            bool hasBuild = build != null && int.TryParse(build, out buildNumber);
            if (product == null)
            {
                if (!hasBuild) return fallback;
                product = buildNumber >= 22000 ? "Windows 11" : "Windows";
            }
            else if (buildNumber >= 22000 && product.StartsWith("Windows 10", StringComparison.Ordinal))
            {
                // Windows 11 sigue diciendo "Windows 10" en ProductName.
                product = "Windows 11" + product.Substring("Windows 10".Length);
            }
            StringBuilder sb = new StringBuilder(product);
            if (display != null) sb.Append(" " + display);
            if (hasBuild) sb.Append(" (build " + buildNumber + ")");
            return sb.ToString();
        }

        private static void AppendAuth(StringBuilder sb, IDictionary<string, string> auth, string indent)
        {
            if (auth == null) return;
            foreach (string key in SpotifyPrefs.AuthKeys)
            {
                string v;
                if (!auth.TryGetValue(key, out v) || v == null) continue;
                sb.AppendLine(indent + key + " = " + Log.Redact(Unquote(v)));
            }
        }

        private static string Unquote(string v)
        {
            string t = v.Trim();
            if (t.Length >= 2 && t.StartsWith("\"") && t.EndsWith("\"")) return t.Substring(1, t.Length - 2);
            return t;
        }

        private static string YesNo(bool b)
        {
            return b ? "yes" : "no";
        }
    }
}
