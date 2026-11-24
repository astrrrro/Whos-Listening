using System;
using System.IO;

namespace WhosListening
{
    public class SpotifyPaths
    {
        private readonly string appData;
        private readonly string localAppData;

        public SpotifyPaths(string appData, string localAppData)
        {
            this.appData = appData;
            this.localAppData = localAppData;
        }

        public static SpotifyPaths Current
        {
            get { return new SpotifyPaths(AppPaths.AppData, AppPaths.LocalAppData); }
        }

        public string Dir { get { return Path.Combine(appData, "Spotify"); } }
        public string PrefsFile { get { return Path.Combine(Dir, "prefs"); } }
        public string Exe { get { return Path.Combine(Dir, "Spotify.exe"); } }
        public string UsersDir { get { return Path.Combine(Dir, "Users"); } }
        public string DbrtsFile { get { return Path.Combine(Path.Combine(localAppData, "Spotify"), "dbrts"); } }

        public static bool IsSafeUsername(string u)
        {
            if (string.IsNullOrWhiteSpace(u)) return false;
            if (u == "." || u == "..") return false;
            if (u.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            if (u.IndexOfAny(new char[] { '\\', '/', ':' }) >= 0) return false;
            return true;
        }

        public string UserCacheDir(string username)
        {
            if (!IsSafeUsername(username)) throw new ArgumentException("nombre de usuario no valido");
            string result = Path.Combine(UsersDir, username + "-user");
            string parent = Path.GetDirectoryName(Path.GetFullPath(result));
            string users = Path.GetFullPath(UsersDir).TrimEnd('\\', '/');
            if (parent == null || !string.Equals(parent.TrimEnd('\\', '/'), users, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("ruta de cache fuera de Users");
            return result;
        }

        public bool IsInstalled { get { return File.Exists(Exe); } }

        public bool IsStoreVersion
        {
            get
            {
                if (IsInstalled) return false;
                string packages = Path.Combine(localAppData, "Packages");
                if (!Directory.Exists(packages)) return false;
                try
                {
                    return Directory.GetDirectories(packages, "SpotifyAB.SpotifyMusic_*").Length > 0;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
