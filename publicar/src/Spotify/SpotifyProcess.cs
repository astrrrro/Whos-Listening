using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace WhosListening
{
    public interface ISpotifyProcess
    {
        bool IsRunning();
        bool CloseAndWait(TimeSpan timeout);
        void Start(string exe);
        DateTime LastSelfLaunchUtc { get; }
    }

    public class SpotifyProcess : ISpotifyProcess
    {
        private DateTime lastSelfLaunchUtc = DateTime.MinValue;

        public DateTime LastSelfLaunchUtc { get { return lastSelfLaunchUtc; } }

        public bool IsRunning()
        {
            Process[] list = Process.GetProcessesByName("Spotify");
            try
            {
                return list.Length > 0;
            }
            finally
            {
                foreach (Process p in list) p.Dispose();
            }
        }

        public bool CloseAndWait(TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow + timeout;
            while (true)
            {
                Process[] list = Process.GetProcessesByName("Spotify");
                int remaining = list.Length;
                foreach (Process p in list)
                {
                    try
                    {
                        if (!p.HasExited) p.Kill();
                    }
                    catch (Exception)
                    {
                        // ya habia terminado o no se pudo cerrar; se revisa de nuevo abajo
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }
                if (remaining == 0)
                {
                    Thread.Sleep(500);
                    return true;
                }
                if (DateTime.UtcNow >= deadline) return false;
                Thread.Sleep(100);
            }
        }

        public void Start(string exe)
        {
            ProcessStartInfo psi = new ProcessStartInfo(exe);
            psi.WorkingDirectory = Path.GetDirectoryName(exe);
            psi.UseShellExecute = true;
            DateTime previous = lastSelfLaunchUtc;
            lastSelfLaunchUtc = DateTime.UtcNow;
            Process p;
            try
            {
                p = Process.Start(psi);
            }
            catch (Exception)
            {
                // No se abrio: el vigilante no debe tratar el proximo arranque como propio.
                lastSelfLaunchUtc = previous;
                throw;
            }
            if (p != null) p.Dispose();
        }
    }
}
