using System;
using System.Windows.Threading;

namespace WhosListening
{
    // Vigilante: cada 700 ms revisa si Spotify esta abierto y avisa cuando se acaba de abrir
    // (salvo si lo abrio el propio programa hace menos de 10 s; eso lo decide LaunchDetector).
    // Corre en el hilo de la interfaz con un DispatcherTimer: el aviso llega en ese hilo.
    public class Watcher
    {
        public const int IntervalMs = 700;

        private readonly ISpotifyProcess proc;
        private readonly Action onSpotifyOpened;
        private DispatcherTimer timer;
        private LaunchDetector detector;
        private bool lastTickFailed;

        public Watcher(ISpotifyProcess proc, Action onSpotifyOpened)
        {
            if (proc == null) throw new ArgumentNullException("proc");
            if (onSpotifyOpened == null) throw new ArgumentNullException("onSpotifyOpened");
            this.proc = proc;
            this.onSpotifyOpened = onSpotifyOpened;
        }

        public bool IsActive { get { return timer != null; } }

        // Un Spotify que ya estaba abierto cuenta como recien abierto en la primera revision.
        public void Start()
        {
            Start(false);
        }

        // ignoreAlreadyRunning: si Spotify ya esta abierto al arrancar, no se avisa (solo la
        // proxima vez que se abra). Si ya estaba activo, no hace nada.
        public void Start(bool ignoreAlreadyRunning)
        {
            if (timer != null) return;
            detector = new LaunchDetector();
            lastTickFailed = false;
            if (ignoreAlreadyRunning)
            {
                try
                {
                    detector.Poll(proc.IsRunning(), DateTime.UtcNow, proc.LastSelfLaunchUtc);
                }
                catch (Exception ex)
                {
                    Log.Error("vigilante: primera revision", ex);
                }
            }
            DispatcherTimer t = new DispatcherTimer();
            t.Interval = TimeSpan.FromMilliseconds(IntervalMs);
            t.Tick += OnTick;
            timer = t;
            t.Start();
            Log.Info("vigilante: activo");
        }

        public void Stop()
        {
            if (timer == null) return;
            timer.Stop();
            timer.Tick -= OnTick;
            timer = null;
            Log.Info("vigilante: detenido");
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (timer == null) return;
            try
            {
                bool opened = detector.Poll(proc.IsRunning(), DateTime.UtcNow, proc.LastSelfLaunchUtc);
                lastTickFailed = false;
                if (opened)
                {
                    Log.Info("vigilante: spotify abierto");
                    onSpotifyOpened();
                }
            }
            catch (Exception ex)
            {
                // Solo se anota el primer error seguido, para no llenar el log cada 700 ms.
                if (!lastTickFailed) Log.Error("vigilante", ex);
                lastTickFailed = true;
            }
        }
    }
}
