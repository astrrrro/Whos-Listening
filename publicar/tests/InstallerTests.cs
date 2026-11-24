using System;
using System.IO;
using System.Reflection;

namespace WhosListening
{
    public static class InstallerTests
    {
        public static void TestShortcutNames()
        {
            Assert.Equal("Who's Listening.lnk", Path.GetFileName(AppPaths.StartMenuLnk), "menu Inicio");
            Assert.Equal("Who's Listening.lnk", Path.GetFileName(AppPaths.StartupLnk), "carpeta Inicio");
            Assert.Equal("WhosListening.lnk", Path.GetFileName(AppPaths.LegacyStartMenuLnk), "menu Inicio viejo");
            Assert.Equal("WhosListening.lnk", Path.GetFileName(AppPaths.LegacyStartupLnk), "carpeta Inicio vieja");
            Assert.Equal(Path.GetDirectoryName(AppPaths.StartupLnk), Path.GetDirectoryName(AppPaths.LegacyStartupLnk), "misma carpeta Inicio");
        }

        public static void TestIsSameOrNewer()
        {
            string exe = AppPaths.InstalledExe;
            Assert.True(!Installer.IsSameOrNewer(exe, new Version(1, 0, 0, 0)), "sin exe instalado");

            Directory.CreateDirectory(AppPaths.InstallDir);
            File.WriteAllText(exe, "no es un exe");
            Assert.True(!Installer.IsSameOrNewer(exe, new Version(1, 0, 0, 0)), "exe ilegible");

            string self = Assembly.GetExecutingAssembly().Location;
            Version mine = Assembly.GetExecutingAssembly().GetName().Version;
            File.Copy(self, exe, true);
            Assert.True(Installer.IsSameOrNewer(exe, mine), "misma version");
            Assert.True(Installer.IsSameOrNewer(exe, new Version(0, 1, 0, 0)), "instalada mas nueva");
            Assert.True(!Installer.IsSameOrNewer(exe, new Version(mine.Major + 1, 0, 0, 0)), "instalada mas vieja");
        }

        public static void TestSetStartupRemovesLegacyShortcut()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AppPaths.LegacyStartupLnk));
            File.WriteAllText(AppPaths.LegacyStartupLnk, "viejo");

            Installer.SetStartup(true);
            Assert.True(File.Exists(AppPaths.StartupLnk), "crea el acceso nuevo");
            Assert.True(!File.Exists(AppPaths.LegacyStartupLnk), "borra el viejo al encender");
            Assert.True(Installer.IsStartupOn(), "IsStartupOn");

            File.WriteAllText(AppPaths.LegacyStartupLnk, "viejo");
            Installer.SetStartup(false);
            Assert.True(!File.Exists(AppPaths.StartupLnk), "borra el nuevo");
            Assert.True(!File.Exists(AppPaths.LegacyStartupLnk), "borra el viejo al apagar");
            Assert.True(!Installer.IsStartupOn(), "IsStartupOn apagado");
        }
    }
}
