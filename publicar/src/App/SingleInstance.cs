using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace WhosListening
{
    // Una sola instancia por sesion de Windows. La primera toma el mutex; las demas le piden
    // que muestre el selector (o que se cierre) con eventos con nombre.
    public static class SingleInstance
    {
        private const string MutexName = @"Local\WhosListening";
        private const string ShowEventName = @"Local\WhosListening.Show";
        private const string ExitEventName = @"Local\WhosListening.Exit";

        // Campos estaticos: el mutex y el evento no deben ser recolectados mientras corre el programa.
        private static Mutex mutex;
        private static EventWaitHandle showEvent;
        private static EventWaitHandle exitEvent;
        private static Thread showListener;
        private static Thread exitListener;

        private const int AsfwAny = -1;

        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int dwProcessId);

        // True si esta instancia es la primaria (o ya lo era).
        public static bool TryBecomePrimary()
        {
            if (mutex != null) return true;
            bool createdNew;
            Mutex m = new Mutex(true, MutexName, out createdNew);
            bool owned = createdNew;
            if (!owned)
            {
                try
                {
                    owned = m.WaitOne(0);
                }
                catch (AbandonedMutexException)
                {
                    // La instancia anterior termino sin soltarlo: ahora es nuestro.
                    owned = true;
                }
            }
            if (!owned)
            {
                m.Dispose();
                return false;
            }
            mutex = m;
            // Los eventos se crean de inmediato, para que una segunda instancia que arranque
            // enseguida ya los encuentre.
            showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ExitEventName);
            return true;
        }

        // Desde una instancia secundaria: pide a la primaria que muestre el selector.
        // Reintenta un momento por si la primaria todavia no creo el evento.
        public static void SignalPrimaryToShow()
        {
            // Esta instancia la abrio el usuario y puede pasar al frente; le cede ese permiso a la
            // primaria para que el selector quede activo (con foco de teclado), no solo encima.
            try
            {
                AllowSetForegroundWindow(AsfwAny);
            }
            catch (Exception)
            {
                // Sin el permiso el selector igual aparece encima (Topmost).
            }
            for (int i = 0; i < 10; i++)
            {
                EventWaitHandle h;
                if (EventWaitHandle.TryOpenExisting(ShowEventName, out h))
                {
                    using (h)
                    {
                        h.Set();
                    }
                    return;
                }
                Thread.Sleep(200);
            }
            Log.Info("instancia: no se encontro la instancia primaria para mostrar el selector");
        }

        // En la primaria: escucha el evento en un hilo de fondo y llama a onShow en el hilo de la
        // interfaz. Debe llamarse desde el hilo de la interfaz (el del Dispatcher de la app).
        public static void ListenForShow(Action onShow)
        {
            if (onShow == null) throw new ArgumentNullException("onShow");
            if (showEvent == null || showListener != null) return;
            showListener = StartListener(showEvent, onShow, "WhosListening.Show");
        }

        // En la primaria: igual que ListenForShow, para el pedido de cierre (instalar encima,
        // desinstalar). onExit debe hacer el cierre normal del programa.
        public static void ListenForExit(Action onExit)
        {
            if (onExit == null) throw new ArgumentNullException("onExit");
            if (exitEvent == null || exitListener != null) return;
            exitListener = StartListener(exitEvent, onExit, "WhosListening.Exit");
        }

        // Desde otro proceso: pide a la primaria que se cierre y espera hasta que suelte el mutex
        // o pase timeoutMs. True si ya no hay primaria (o si este proceso es la primaria).
        public static bool RequestPrimaryExit(int timeoutMs)
        {
            if (mutex != null) return true;
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(0, timeoutMs));
            bool signaled = false;
            while (true)
            {
                if (!signaled) signaled = TrySet(ExitEventName);

                Mutex m;
                try
                {
                    if (!Mutex.TryOpenExisting(MutexName, out m)) return true;
                }
                catch (Exception ex)
                {
                    Log.Error("instancia: abrir mutex", ex);
                    return false;
                }
                using (m)
                {
                    int left = (int)Math.Max(0, (deadline - DateTime.UtcNow).TotalMilliseconds);
                    // Si todavia no se pudo avisar (la primaria aun no crea el evento), se
                    // reintenta seguido.
                    int wait = signaled ? left : Math.Min(200, left);
                    bool acquired;
                    try
                    {
                        acquired = m.WaitOne(wait);
                    }
                    catch (AbandonedMutexException)
                    {
                        acquired = true;
                    }
                    if (acquired)
                    {
                        try
                        {
                            m.ReleaseMutex();
                        }
                        catch (Exception)
                        {
                        }
                        return true;
                    }
                }
                if (DateTime.UtcNow >= deadline)
                {
                    Log.Info("instancia: la primaria no se cerro a tiempo");
                    return false;
                }
            }
        }

        private static bool TrySet(string name)
        {
            try
            {
                EventWaitHandle h;
                if (!EventWaitHandle.TryOpenExisting(name, out h)) return false;
                using (h)
                {
                    h.Set();
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("instancia: avisar " + name, ex);
                return false;
            }
        }

        private static Thread StartListener(EventWaitHandle ev, Action action, string name)
        {
            Dispatcher ui = Dispatcher.CurrentDispatcher;
            Thread t = new Thread(delegate()
            {
                while (true)
                {
                    try
                    {
                        ev.WaitOne();
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("instancia: esperar evento " + name, ex);
                        return;
                    }
                    try
                    {
                        ui.BeginInvoke(action);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("instancia: atender evento " + name, ex);
                    }
                }
            });
            t.IsBackground = true;
            t.Name = name;
            t.Start();
            return t;
        }

        // Suelta el mutex. Llamar desde el mismo hilo que llamo a TryBecomePrimary.
        public static void Release()
        {
            if (mutex == null) return;
            try
            {
                mutex.ReleaseMutex();
            }
            catch (Exception)
            {
                // Si no se puede soltar, Windows lo libera al terminar el proceso.
            }
            mutex.Dispose();
            mutex = null;
        }
    }
}
