using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;

namespace WhosListening
{
    public static class Installer
    {
        private const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\WhosListening";

        private static string CurrentExe()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        private static bool SamePath(string a, string b)
        {
            return string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRunningFromInstallDir()
        {
            return SamePath(CurrentExe(), AppPaths.InstalledExe);
        }

        // True si ya hay un exe instalado con la misma version o una mas nueva que este: abrir
        // otra vez el exe descargado no debe ofrecer instalar de nuevo.
        public static bool InstalledIsSameOrNewer()
        {
            return IsSameOrNewer(AppPaths.InstalledExe, Assembly.GetExecutingAssembly().GetName().Version);
        }

        public static bool IsSameOrNewer(string installedExe, Version current)
        {
            try
            {
                if (!File.Exists(installedExe)) return false;
                Version installed = AssemblyName.GetAssemblyName(installedExe).Version;
                return installed != null && current != null && installed >= current;
            }
            catch (Exception ex)
            {
                // Exe dañado o ilegible: se ofrece instalar como siempre.
                Log.Info("version instalada ilegible (" + ex.GetType().Name + ")");
                return false;
            }
        }

        public static void Install()
        {
            Directory.CreateDirectory(AppPaths.InstallDir);
            string exe = AppPaths.InstalledExe;
            StopInstalledInstances();
            if (!IsRunningFromInstallDir())
            {
                CopyWithRetry(CurrentExe(), exe);
            }

            ShellLink.Create(AppPaths.StartMenuLnk, exe, "", AppPaths.InstallDir, "Who's Listening?");
            // Accesos con el nombre de versiones anteriores: si quedaran, habria duplicados.
            // El de Inicio de Windows lo vuelve a crear SetStartup con el nombre nuevo.
            TryDeleteFile(AppPaths.LegacyStartMenuLnk);
            TryDeleteFile(AppPaths.LegacyStartupLnk);

            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallKeyPath))
            {
                key.SetValue("DisplayName", "Who's Listening?");
                key.SetValue("DisplayIcon", exe);
                key.SetValue("UninstallString", "\"" + exe + "\" --uninstall");
                key.SetValue("Publisher", "astrrrro");
                key.SetValue("DisplayVersion", version);
                key.SetValue("InstallLocation", AppPaths.InstallDir);
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
            Log.Info("instalado");
        }

        // El antivirus puede tener abierto el exe instalado unos instantes (por ejemplo al
        // revisar la copia anterior): se reintenta hasta 5 veces, con 500 ms entre intentos.
        private static void CopyWithRetry(string source, string target)
        {
            const int attempts = 5;
            for (int i = 1; ; i++)
            {
                try
                {
                    File.Copy(source, target, true);
                    return;
                }
                catch (IOException ex)
                {
                    if (i >= attempts) throw;
                    Log.Info("instalar: exe ocupado, reintento " + i + " (" + ex.GetType().Name + ")");
                }
                catch (UnauthorizedAccessException ex)
                {
                    if (i >= attempts) throw;
                    Log.Info("instalar: exe ocupado, reintento " + i + " (" + ex.GetType().Name + ")");
                }
                Thread.Sleep(500);
            }
        }

        public static void SetStartup(bool on)
        {
            if (on)
            {
                ShellLink.Create(AppPaths.StartupLnk, AppPaths.InstalledExe, "--background", AppPaths.InstallDir, "Who's Listening?");
            }
            else if (File.Exists(AppPaths.StartupLnk))
            {
                File.Delete(AppPaths.StartupLnk);
            }
            TryDeleteFile(AppPaths.LegacyStartupLnk);
        }

        public static bool IsStartupOn()
        {
            return File.Exists(AppPaths.StartupLnk);
        }

        public static void Uninstall(bool deleteData)
        {
            Uninstall(deleteData, null);
        }

        // beforeFinish (puede ser null) corre cuando ya se quito todo menos la carpeta de
        // instalacion, justo antes de borrarla (o de lanzar la copia temporal que la borra). Ahi
        // va el aviso final: asi la copia temporal no espera ni cierra un dialogo abierto.
        public static void Uninstall(bool deleteData, Action beforeFinish)
        {
            bool fromInstall = IsRunningFromInstallDir();
            // Se registra antes de borrar los datos: escribir el log despues volveria a crear la carpeta.
            Log.Info(deleteData ? "desinstalando (con datos)" : "desinstalando (se conservan los datos)");
            StopInstalledInstances();

            TryDeleteFile(AppPaths.StartupLnk);
            TryDeleteFile(AppPaths.StartMenuLnk);
            TryDeleteFile(AppPaths.LegacyStartupLnk);
            TryDeleteFile(AppPaths.LegacyStartMenuLnk);
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(UninstallKeyPath, false);
            }
            catch (Exception ex)
            {
                Log.Error("desinstalar-registro", ex);
            }

            if (deleteData)
            {
                try
                {
                    SafeDeleteDir(AppPaths.DataDir, AppPaths.DataDir);
                }
                catch (Exception ex)
                {
                    Log.Error("desinstalar-datos", ex);
                }
            }

            if (beforeFinish != null)
            {
                try
                {
                    beforeFinish();
                }
                catch (Exception ex)
                {
                    Log.Error("desinstalar-aviso", ex);
                }
            }

            if (fromInstall)
            {
                string temp = Path.Combine(Path.GetTempPath(), "WhosListening-uninstall.exe");
                File.Copy(CurrentExe(), temp, true);
                ProcessStartInfo psi = new ProcessStartInfo(temp);
                psi.Arguments = "--finish-uninstall " + Process.GetCurrentProcess().Id + " \"" + AppPaths.InstallDir + "\"";
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WorkingDirectory = Path.GetTempPath();
                Process.Start(psi);
            }
            else
            {
                DeleteInstallDirWithRetry(AppPaths.InstallDir);
            }
        }

        // Lo llama la copia temporal: espera al proceso original y borra la carpeta de instalación.
        public static void FinishUninstall(int pid, string dir)
        {
            try
            {
                Directory.SetCurrentDirectory(Path.GetTempPath());
            }
            catch (Exception ex)
            {
                Log.Error("cambiar-directorio", ex);
            }
            try
            {
                using (Process p = Process.GetProcessById(pid))
                {
                    p.WaitForExit(30000);
                }
            }
            catch (ArgumentException)
            {
                // El proceso ya terminó.
            }
            catch (Exception ex)
            {
                Log.Error("esperar-proceso", ex);
            }
            DeleteInstallDirWithRetry(dir);
        }

        // Cierra las otras instancias de WhosListening que corren desde InstallDir (nunca el
        // proceso actual): primero les pide salir normalmente (SingleInstance) y solo si alguna
        // sigue viva la termina a la fuerza.
        private static void StopInstalledInstances()
        {
            try
            {
                SingleInstance.RequestPrimaryExit(5000);
            }
            catch (Exception ex)
            {
                Log.Error("pedir cierre", ex);
            }

            string prefix = Path.GetFullPath(AppPaths.InstallDir).TrimEnd('\\') + "\\";
            int self = Process.GetCurrentProcess().Id;
            Process[] list = Process.GetProcessesByName("WhosListening");
            foreach (Process p in list)
            {
                try
                {
                    if (p.Id == self) continue;
                    string file = p.MainModule.FileName;
                    if (file == null) continue;
                    if (!Path.GetFullPath(file).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                    // Puede estar terminando de cerrarse despues de soltar el mutex.
                    if (p.WaitForExit(2000)) continue;
                    Log.Info("instancia instalada no se cerro: se termina (pid " + p.Id + ")");
                    p.Kill();
                    p.WaitForExit(5000);
                }
                catch (Win32Exception)
                {
                }
                catch (InvalidOperationException)
                {
                }
                finally
                {
                    p.Dispose();
                }
            }
        }

        private static void DeleteInstallDirWithRetry(string dir)
        {
            StopInstalledInstances();
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    SafeDeleteDir(dir, AppPaths.InstallDir);
                    return;
                }
                catch (IOException)
                {
                    Thread.Sleep(500);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(500);
                }
            }
            Log.Info("desinstalar-no-pudo-borrar");
        }

        // Solo borra si "dir" es exactamente la carpeta permitida y no es vacía ni una raíz.
        private static void SafeDeleteDir(string dir, string allowed)
        {
            if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(allowed)) return;
            string full = Path.GetFullPath(dir).TrimEnd('\\');
            string allowedFull = Path.GetFullPath(allowed).TrimEnd('\\');
            if (full.Length == 0 || Path.GetPathRoot(full + "\\").TrimEnd('\\').Length == full.Length) return;
            if (!string.Equals(full, allowedFull, StringComparison.OrdinalIgnoreCase)) return;
            if (!Directory.Exists(full)) return;
            Directory.Delete(full, true);
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                Log.Error("borrar-archivo", ex);
            }
        }
    }
}
