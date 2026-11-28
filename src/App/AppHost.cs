using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace WhosListening
{
    // Arma el programa: una sola Application de WPF, el selector, el vigilante y la bandeja.
    //
    // - Con ShowOnSpotifyOpen: vigilante y bandeja activos; al cerrar el selector el proceso
    //   queda en la bandeja.
    // - Sin ShowOnSpotifyOpen: sin vigilante ni bandeja; al cerrar el selector el proceso termina.
    // - El interruptor de la ventana y el del menu pasan por SetShowOnOpen.
    // - Solo instalado: antes de mostrar el selector por primera vez a pedido del usuario (nunca
    //   al arrancar con Windows) ofrece importar las cuentas de la version anterior.
    // - Otra instancia puede pedirle que se cierre (instalar encima, desinstalar): SingleInstance.
    public class AppHost
    {
        private Application app;
        private AppServices services;
        private Watcher watcher;
        private TrayIcon tray;
        private SelectorWindow selector;
        private bool shuttingDown;
        private bool legacyPromptsDone;
        private bool legacyPromptsRunning;
        private DispatcherTimer exitRetry;

        // La unica Application de WPF del proceso (WPF no permite crear una segunda). La usan
        // tambien los dialogos de instalar y desinstalar de Program, antes de Run.
        public static Application EnsureApplication()
        {
            if (Application.Current != null) return Application.Current;
            Application a = new Application();
            // Critico: cerrar el selector o un dialogo nunca debe terminar el proceso por si solo.
            a.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            a.DispatcherUnhandledException += delegate(object s, DispatcherUnhandledExceptionEventArgs e)
            {
                Log.Error("error no controlado", e.Exception);
                e.Handled = true;
            };
            return a;
        }

        // background: arranque con Windows (no muestra el selector hasta que se abra Spotify).
        // Vuelve cuando el programa termina.
        public void Run(bool background)
        {
            Settings settings = Settings.Load();
            SyncStartupShortcut(settings);
            if (background && !settings.ShowOnSpotifyOpen)
            {
                Log.Info("segundo plano: desactivado en la configuracion");
                return;
            }

            if (!SingleInstance.TryBecomePrimary())
            {
                // Abrir el exe otra vez (menu Inicio, busqueda) vuelve a mostrar el selector.
                if (!background) SingleInstance.SignalPrimaryToShow();
                Log.Info(background ? "segundo plano: ya hay una instancia" : "instancia: se pidio mostrar el selector");
                return;
            }

            try
            {
                Log.Info(background ? "inicio en segundo plano" : "inicio");
                services = BuildServices(settings);

                app = EnsureApplication();
                app.Exit += delegate { StopBackground(); };
                bool bg = background;
                app.Startup += delegate { OnStartup(bg); };
                app.Run();
            }
            finally
            {
                StopBackground();
                SingleInstance.Release();
                Log.Info("fin");
            }
        }

        private static AppServices BuildServices(Settings settings)
        {
            AppServices s = new AppServices();
            s.Paths = SpotifyPaths.Current;
            // Un solo SpotifyProcess para el selector, el AccountSwitcher y el vigilante: asi
            // LastSelfLaunchUtc evita mostrar el selector cuando el propio programa abre Spotify.
            s.Process = new SpotifyProcess();
            s.Settings = settings;
            s.Store = new AccountStore(AppPaths.AccountsDir);
            AccountStore store = s.Store;
            s.Switcher = new AccountSwitcher(s.Paths, s.Process,
                delegate(Account saved) { store.Save(saved); });
            return s;
        }

        // Instalado: el acceso de Inicio sigue a ShowOnSpotifyOpen (cubre cambios hechos mientras
        // corria sin instalar). Sin instalar nunca se crea uno que apunte al exe instalado.
        private static void SyncStartupShortcut(Settings settings)
        {
            try
            {
                if (!Installer.IsRunningFromInstallDir()) return;
                if (Installer.IsStartupOn() != settings.ShowOnSpotifyOpen) Installer.SetStartup(settings.ShowOnSpotifyOpen);
            }
            catch (Exception ex)
            {
                Log.Error("inicio con Windows: sincronizar", ex);
            }
        }

        private static bool IsInstalledCopy()
        {
            try
            {
                return Installer.IsRunningFromInstallDir();
            }
            catch (Exception ex)
            {
                Log.Error("revisar carpeta de instalacion", ex);
                return false;
            }
        }

        private static string LegacyProfilesDir
        {
            get { return Path.Combine(Path.Combine(AppPaths.AppData, "Spotify"), "AccountProfiles"); }
        }

        // Preguntas de la version anterior, una vez por ejecucion y antes de mostrar el selector
        // (si ya esta abierto se dejan para la proxima vez, para que no quede sin las importadas).
        // Si mientras tanto llega otro pedido de mostrar, se ignora: el selector se muestra al final.
        private void ShowSelectorFromUser()
        {
            if (shuttingDown || legacyPromptsRunning) return;
            // Solo en la copia instalada: una ejecucion sin instalar nunca importa cuentas.
            if (!legacyPromptsDone && selector == null && IsInstalledCopy())
            {
                legacyPromptsDone = true;
                legacyPromptsRunning = true;
                try
                {
                    OfferLegacyImport();
                }
                finally
                {
                    legacyPromptsRunning = false;
                }
            }
            ShowSelector();
        }

        // Ofrece importar las cuentas de %APPDATA%\Spotify\AccountProfiles que todavia no estan
        // (por Username). Se pregunta una sola vez: despues queda LegacyImportOffered = true.
        private void OfferLegacyImport()
        {
            Settings st = services.Settings;
            if (st.LegacyImportOffered) return;
            string dir = LegacyProfilesDir;
            if (!Directory.Exists(dir)) return;

            List<Account> fresh = new List<Account>();
            try
            {
                foreach (Account a in LegacyImporter.Read(dir))
                {
                    if (services.Store.FindByUsername(a.Username) != null) continue;
                    if (ContainsUsername(fresh, a.Username)) continue;
                    fresh.Add(a);
                }
            }
            catch (Exception ex)
            {
                Log.Error("importar cuentas anteriores: leer", ex);
                return;
            }
            if (fresh.Count == 0) return;

            bool yes = DarkDialog.Confirm(null, Strings.Format("importPrompt", fresh.Count), "yes", "no");
            // Un cierre pedido desde fuera tambien cierra el dialogo: no cuenta como respuesta.
            if (shuttingDown) return;

            if (yes)
            {
                int added = 0;
                try
                {
                    foreach (Account a in fresh)
                    {
                        if (services.Store.FindByUsername(a.Username) != null) continue;
                        services.Store.Add(a.Label, a.Username, a.Auth);
                        added++;
                    }
                }
                catch (Exception ex)
                {
                    // No se marca como ofrecido: la proxima vez se reintenta con las que falten.
                    Log.Error("importar cuentas anteriores", ex);
                    DarkDialog.Message(null, Strings.Format("errorWithDetail", ex.Message));
                    return;
                }
                Log.Info("importar cuentas anteriores: " + added);
                DarkDialog.Message(null, Strings.Format("importResult", added));
            }
            else
            {
                Log.Info("importar cuentas anteriores: rechazado");
            }

            st.LegacyImportOffered = true;
            try
            {
                st.Save();
            }
            catch (Exception ex)
            {
                Log.Error("guardar configuracion", ex);
            }
        }

        private static bool ContainsUsername(List<Account> list, string username)
        {
            foreach (Account a in list)
            {
                if (string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private void OnStartup(bool background)
        {
            watcher = new Watcher(services.Process, OnSpotifyOpened);
            try
            {
                SingleInstance.ListenForShow(ShowSelectorFromUser);
            }
            catch (Exception ex)
            {
                Log.Error("instancia: escuchar", ex);
            }
            try
            {
                SingleInstance.ListenForExit(OnExitRequested);
            }
            catch (Exception ex)
            {
                Log.Error("instancia: escuchar cierre", ex);
            }

            if (services.Settings.ShowOnSpotifyOpen)
            {
                // Si el usuario abrio el exe, el selector ya se muestra: un Spotify ya abierto no
                // cuenta como recien abierto. Al arrancar con Windows si cuenta.
                StartBackground(!background);
            }
            if (!background) ShowSelectorFromUser();
        }

        // Otra instancia pide cerrar (instalar encima o desinstalar): cierre normal. Si se esta
        // cambiando de cuenta, se espera a que termine para no dejar el cambio a medias.
        private void OnExitRequested()
        {
            if (shuttingDown) return;
            Log.Info("instancia: se pidio cerrar");
            if (selector != null && selector.IsBusy)
            {
                if (exitRetry != null) return;
                DispatcherTimer t = new DispatcherTimer();
                t.Interval = TimeSpan.FromMilliseconds(250);
                t.Tick += delegate
                {
                    if (selector != null && selector.IsBusy) return;
                    t.Stop();
                    exitRetry = null;
                    Shutdown();
                };
                exitRetry = t;
                t.Start();
                return;
            }
            Shutdown();
        }

        private void OnSpotifyOpened()
        {
            // Durante las preguntas de la version anterior el selector se muestra al terminarlas
            // (asi ya incluye las cuentas importadas).
            if (legacyPromptsRunning) return;
            ShowSelector();
        }

        // Muestra el selector; si ya esta abierto, lo trae al frente. Nunca hay mas de uno.
        public void ShowSelector()
        {
            if (shuttingDown) return;
            try
            {
                if (selector != null)
                {
                    if (selector.WindowState == WindowState.Minimized) selector.WindowState = WindowState.Normal;
                    selector.Activate();
                    selector.Topmost = false;
                    selector.Topmost = true;
                    return;
                }

                SelectorWindow w = new SelectorWindow(services);
                w.ToggleChanged += SetShowOnOpen;
                w.Closed += delegate
                {
                    if (selector == w) selector = null;
                    if (!shuttingDown && !services.Settings.ShowOnSpotifyOpen) Shutdown();
                };
                selector = w;
                w.Show();
            }
            catch (Exception ex)
            {
                Log.Error("mostrar selector", ex);
                selector = null;
                if (!services.Settings.ShowOnSpotifyOpen) Shutdown();
            }
        }

        // Unico lugar que cambia "Mostrar al abrir Spotify".
        public void SetShowOnOpen(bool on)
        {
            if (shuttingDown) return;
            try
            {
                services.Settings.ShowOnSpotifyOpen = on;
                services.Settings.Save();
            }
            catch (Exception ex)
            {
                Log.Error("guardar configuracion", ex);
            }

            // No se crea un acceso de inicio que apunte a un exe sin instalar (la Tarea 10 lo
            // sincroniza al instalar).
            try
            {
                if (Installer.IsRunningFromInstallDir()) Installer.SetStartup(on);
            }
            catch (Exception ex)
            {
                Log.Error("inicio con Windows", ex);
            }

            if (on) StartBackground(true);
            else StopBackground();

            try
            {
                if (selector != null) selector.SetToggle(on);
            }
            catch (Exception ex)
            {
                Log.Error("selector: actualizar interruptor", ex);
            }
            try
            {
                if (tray != null) tray.SetToggle(on);
            }
            catch (Exception ex)
            {
                Log.Error("bandeja: actualizar marca", ex);
            }

            Log.Info(on ? "mostrar al abrir: si" : "mostrar al abrir: no");
            if (!on && selector == null) Shutdown();
        }

        // Vigilante y bandeja activos (no hace nada si ya lo estan).
        private void StartBackground(bool ignoreAlreadyRunning)
        {
            try
            {
                if (watcher != null) watcher.Start(ignoreAlreadyRunning);
            }
            catch (Exception ex)
            {
                Log.Error("vigilante: iniciar", ex);
            }
            if (tray != null) return;
            try
            {
                TrayIcon t = new TrayIcon(services.Paths.Exe, true);
                // Diferido: el menu termina de cerrarse antes de que se pueda liberar el icono.
                t.ShowRequested += delegate { Later(ShowSelectorFromUser); };
                t.ToggleRequested += delegate(bool v) { Later(delegate { SetShowOnOpen(v); }); };
                t.ExitRequested += delegate { Later(ExitFromTray); };
                tray = t;
            }
            catch (Exception ex)
            {
                Log.Error("bandeja: crear", ex);
            }
        }

        // Detiene el vigilante y quita el icono de la bandeja. Se puede llamar varias veces.
        private void StopBackground()
        {
            try
            {
                if (watcher != null) watcher.Stop();
            }
            catch (Exception ex)
            {
                Log.Error("vigilante: detener", ex);
            }
            if (tray == null) return;
            TrayIcon t = tray;
            tray = null;
            try
            {
                t.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error("bandeja: quitar", ex);
            }
        }

        private void ExitFromTray()
        {
            // Mientras se cambia de cuenta no se sale: el cambio quedaria a medias.
            if (selector != null && selector.IsBusy)
            {
                ShowSelector();
                return;
            }
            Shutdown();
        }

        private void Shutdown()
        {
            if (shuttingDown) return;
            shuttingDown = true;
            StopBackground();
            if (app != null) app.Shutdown();
        }

        private void Later(Action a)
        {
            if (app == null) return;
            app.Dispatcher.BeginInvoke(a);
        }
    }
}
