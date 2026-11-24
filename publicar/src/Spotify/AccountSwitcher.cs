using System;
using System.IO;

namespace WhosListening
{
    public enum SwitchResult
    {
        Switched,
        AlreadyActive,
        StartedSameAccount,
        SpotifyNotInstalled,
        SpotifyWouldNotClose,
        Error
    }

    public class AccountSwitcher
    {
        private readonly SpotifyPaths paths;
        private readonly ISpotifyProcess proc;
        private readonly Action<Account> refreshSaved;

        public AccountSwitcher(SpotifyPaths paths, ISpotifyProcess proc, Action<Account> refreshSaved)
        {
            this.paths = paths;
            this.proc = proc;
            this.refreshSaved = refreshSaved;
        }

        public string CurrentUsername()
        {
            return SpotifyPrefs.Load(paths.PrefsFile).Username;
        }

        private static bool SameUser(string a, string b)
        {
            return a != null && b != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static string Unquote(string v)
        {
            if (v == null) return "";
            return v.Trim().Trim('"').Trim();
        }

        private static bool HasValue(System.Collections.Generic.IDictionary<string, string> auth, string key)
        {
            string v;
            return auth != null && auth.TryGetValue(key, out v) && Unquote(v).Length > 0;
        }

        public static bool HasCredentials(System.Collections.Generic.IDictionary<string, string> auth)
        {
            return HasValue(auth, "autologin.blob") || HasValue(auth, "autologin.saved_credentials");
        }

        public SwitchResult SwitchTo(Account target, Account currentlySavedOrNull)
        {
            try
            {
                if (!paths.IsInstalled) return SwitchResult.SpotifyNotInstalled;

                if (!SpotifyPaths.IsSafeUsername(target.Username))
                {
                    Log.Info("SwitchTo: nombre de usuario destino no valido");
                    return SwitchResult.Error;
                }
                if (!HasCredentials(target.Auth))
                {
                    Log.Info("SwitchTo: la cuenta destino no tiene credenciales");
                    return SwitchResult.Error;
                }

                SpotifyPrefs prefs = SpotifyPrefs.Load(paths.PrefsFile);
                string current = prefs.Username;

                if (SameUser(current, target.Username))
                {
                    if (proc.IsRunning()) return SwitchResult.AlreadyActive;
                    proc.Start(paths.Exe);
                    return SwitchResult.StartedSameAccount;
                }

                if (proc.IsRunning() && !proc.CloseAndWait(TimeSpan.FromSeconds(10)))
                    return SwitchResult.SpotifyWouldNotClose;

                // Spotify ya cerro: releer prefs por si cambio al cerrarse.
                prefs = SpotifyPrefs.Load(paths.PrefsFile);
                current = prefs.Username;

                if (currentlySavedOrNull != null && SameUser(currentlySavedOrNull.Username, current))
                {
                    // Solo se renueva si prefs trae credenciales; si no, se
                    // pisaria una cuenta buena con claves vacias.
                    System.Collections.Generic.Dictionary<string, string> fresh = prefs.GetAuth();
                    if (HasCredentials(fresh))
                    {
                        currentlySavedOrNull.Auth = fresh;
                        currentlySavedOrNull.SavedAtUtc = DateTime.UtcNow;
                        if (refreshSaved != null) refreshSaved(currentlySavedOrNull);
                    }
                    else
                    {
                        Log.Info("SwitchTo: prefs sin credenciales, no se renueva la cuenta saliente");
                    }
                }

                prefs.ApplyAuth(target.Auth);
                prefs.Save(paths.PrefsFile);

                try
                {
                    if (File.Exists(paths.DbrtsFile)) File.Delete(paths.DbrtsFile);
                }
                catch (IOException ex) { Log.Error("borrar dbrts", ex); }
                catch (UnauthorizedAccessException ex) { Log.Error("borrar dbrts", ex); }

                try
                {
                    string cache = paths.UserCacheDir(target.Username);
                    if (Directory.Exists(cache)) Directory.Delete(cache, true);
                }
                catch (IOException ex) { Log.Error("borrar cache", ex); }
                catch (UnauthorizedAccessException ex) { Log.Error("borrar cache", ex); }

                proc.Start(paths.Exe);
                return SwitchResult.Switched;
            }
            catch (Exception ex)
            {
                Log.Error("SwitchTo", ex);
                return SwitchResult.Error;
            }
        }

        public bool PrepareNewLogin()
        {
            try
            {
                if (!paths.IsInstalled) return false;
                if (proc.IsRunning() && !proc.CloseAndWait(TimeSpan.FromSeconds(10))) return false;

                SpotifyPrefs prefs = SpotifyPrefs.Load(paths.PrefsFile);
                prefs.ApplyAuth(new System.Collections.Generic.Dictionary<string, string>());
                prefs.Save(paths.PrefsFile);

                // Las credenciales ya se quitaron: borrar dbrts es "mejor esfuerzo"
                // y Spotify se abre igual, como en SwitchTo.
                try
                {
                    if (File.Exists(paths.DbrtsFile)) File.Delete(paths.DbrtsFile);
                }
                catch (IOException ex) { Log.Error("borrar dbrts", ex); }
                catch (UnauthorizedAccessException ex) { Log.Error("borrar dbrts", ex); }

                proc.Start(paths.Exe);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("PrepareNewLogin", ex);
                return false;
            }
        }
    }
}
