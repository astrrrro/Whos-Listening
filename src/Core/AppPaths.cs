using System;
using System.IO;

namespace WhosListening
{
    public static class AppPaths
    {
        private const string AppName = "WhosListening";
        private static string overrideAppData;
        private static string overrideLocalAppData;

        public static void OverrideRoots(string appData, string localAppData)
        {
            overrideAppData = appData;
            overrideLocalAppData = localAppData;
        }

        public static string AppData
        {
            get
            {
                if (overrideAppData != null) return overrideAppData;
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
        }

        public static string LocalAppData
        {
            get
            {
                if (overrideLocalAppData != null) return overrideLocalAppData;
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
        }

        public static string DataDir { get { return Path.Combine(AppData, AppName); } }
        public static string AccountsDir { get { return Path.Combine(DataDir, "accounts"); } }
        public static string SettingsFile { get { return Path.Combine(DataDir, "settings.json"); } }
        public static string LogFile { get { return Path.Combine(DataDir, "log.txt"); } }

        public static string InstallDir
        {
            get { return Path.Combine(Path.Combine(LocalAppData, "Programs"), AppName); }
        }

        public static string InstalledExe { get { return Path.Combine(InstallDir, AppName + ".exe"); } }

        // Los accesos directos usan un nombre legible porque es lo que muestra la busqueda
        // de Windows. El apostrofo es valido en nombres de archivo.
        private const string LnkName = "Who's Listening.lnk";
        // Nombre usado por versiones anteriores: se borra al instalar y desinstalar.
        private const string LegacyLnkName = AppName + ".lnk";

        public static string StartMenuLnk
        {
            get { return Path.Combine(StartMenuPrograms, LnkName); }
        }

        public static string StartupLnk
        {
            get { return Path.Combine(StartupDir, LnkName); }
        }

        public static string LegacyStartMenuLnk
        {
            get { return Path.Combine(StartMenuPrograms, LegacyLnkName); }
        }

        public static string LegacyStartupLnk
        {
            get { return Path.Combine(StartupDir, LegacyLnkName); }
        }

        private static string StartupDir
        {
            get { return Path.Combine(StartMenuPrograms, "Startup"); }
        }

        private static string StartMenuPrograms
        {
            get { return Path.Combine(AppData, Path.Combine("Microsoft", Path.Combine("Windows", Path.Combine("Start Menu", "Programs")))); }
        }
    }
}
