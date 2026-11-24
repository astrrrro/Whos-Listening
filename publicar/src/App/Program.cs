using System;
using System.Diagnostics;

namespace WhosListening
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            string mode = args.Length > 0 ? args[0] : "";
            try
            {
                if (mode == "--uninstall")
                {
                    RunUninstall();
                    return 0;
                }
                if (mode == "--finish-uninstall")
                {
                    int pid;
                    if (args.Length < 3 || !int.TryParse(args[1], out pid)) return 1;
                    Installer.FinishUninstall(pid, args[2]);
                    return 0;
                }

                bool background = mode == "--background";
                // Abierto a mano fuera de la carpeta de instalacion: se ofrece instalar. Si se
                // rechaza, sigue funcionando desde donde esta y la proxima vez se vuelve a preguntar.
                if (!background && !Installer.IsRunningFromInstallDir())
                {
                    // Ya instalada la misma version o una mas nueva: se abre esa, sin preguntar.
                    // Si la instalada es mas vieja, se ofrece instalar (es una actualizacion).
                    if (Installer.InstalledIsSameOrNewer())
                    {
                        Log.Info("ya instalado (misma version o mas nueva): se abre el instalado");
                        AppHost.EnsureApplication();
                        LaunchInstalled();
                        return 0;
                    }
                    if (OfferInstall()) return 0;
                }

                // Sin argumentos: flujo normal (muestra el selector). --background: arranque con
                // Windows, solo vigilante y bandeja.
                new AppHost().Run(background);
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error("main " + mode, ex);
                return 1;
            }
        }

        // True si quedo instalado y se lanzo el exe instalado (este proceso debe terminar).
        private static bool OfferInstall()
        {
            AppHost.EnsureApplication();
            if (!DarkDialog.Confirm(null, Strings.Get("installPrompt"), "yes", "no"))
            {
                Log.Info("instalar: rechazado, sigue sin instalar");
                return false;
            }

            bool startup;
            try
            {
                // Install cierra antes la instancia abierta (instalada o no) para reemplazar el exe.
                Installer.Install();
                startup = Settings.Load().ShowOnSpotifyOpen;
                Installer.SetStartup(startup);
            }
            catch (Exception ex)
            {
                Log.Error("instalar", ex);
                DarkDialog.Message(null, Strings.Format("errorWithDetail", ex.Message));
                return false;
            }

            // Sin "Mostrar al abrir Spotify" no hay acceso en Inicio: el aviso no promete que arranque con Windows.
            DarkDialog.Message(null, Strings.Get(startup ? "installed" : "installedNoStartup"));
            LaunchInstalled();
            return true;
        }

        // Abre el exe instalado sin argumentos (flujo normal: muestra el selector).
        private static void LaunchInstalled()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(AppPaths.InstalledExe);
                psi.UseShellExecute = false;
                psi.WorkingDirectory = AppPaths.InstallDir;
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Log.Error("abrir el exe instalado", ex);
                DarkDialog.Message(null, Strings.Format("errorWithDetail", ex.Message));
            }
        }

        // Desde Aplicaciones (UninstallString): confirma, pregunta por las cuentas y desinstala.
        private static void RunUninstall()
        {
            AppHost.EnsureApplication();
            if (!DarkDialog.Confirm(null, Strings.Get("uninstallConfirm"), "yes", "no")) return;
            bool deleteData = DarkDialog.Confirm(null, Strings.Get("uninstallDeleteAccounts"), "yes", "no");

            // Uninstall cierra antes la instancia abierta (StopInstalledInstances).
            try
            {
                // El aviso va antes de borrar la carpeta de instalacion (ver Installer.Uninstall).
                Installer.Uninstall(deleteData, delegate { DarkDialog.Message(null, Strings.Get("uninstalled")); });
            }
            catch (Exception ex)
            {
                Log.Error("desinstalar", ex);
                DarkDialog.Message(null, Strings.Format("errorWithDetail", ex.Message));
            }
        }
    }
}
