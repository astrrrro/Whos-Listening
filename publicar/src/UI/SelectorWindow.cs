using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WhosListening
{
    // Servicios que necesita el selector. Quien lo arma debe crear el Switcher con
    // refreshSaved = account => Store.Save(account), para que al cambiar de cuenta se
    // actualicen las credenciales guardadas de la cuenta que estaba en uso.
    public class AppServices
    {
        public AccountStore Store;
        public AccountSwitcher Switcher;
        public SpotifyPaths Paths;
        public Settings Settings;
        public ISpotifyProcess Process;
    }

    // Ventana "¿Quien esta escuchando?".
    public class SelectorWindow : Window
    {
        private const double Gap = 18;
        private const double SidePadding = 56;
        private const double CellWidth = AccountCard.CardWidth + Gap;
        private const double CellHeight = AccountCard.CardHeight + Gap;
        private const double ScrollBarSpace = 10;
        private const int MinColumns = 3;
        // Margen que se deja libre dentro del area de trabajo de la pantalla.
        private const double ScreenMargin = 40;
        // Margen de 16 por lado que deja Theme.MakeBorderless para la sombra.
        private const double ShadowMargin = 32;

        public event Action<bool> ToggleChanged;

        private readonly AppServices services;
        private readonly Grid root;
        private readonly Border cardsHost;
        private readonly TextBlock status;
        private PillToggle toggle;
        private List<Account> accounts = new List<Account>();
        private bool busy;
        private bool closed;
        private bool closingByProgram;
        private bool positioned;

        public SelectorWindow(AppServices services)
        {
            if (services == null) throw new ArgumentNullException("services");
            this.services = services;

            Title = Strings.Get("appName");
            Topmost = true;
            ShowInTaskbar = true;
            WindowStartupLocation = WindowStartupLocation.Manual;
            SizeToContent = SizeToContent.Height;
            Rect work = SystemParameters.WorkArea;
            MaxHeight = Math.Max(300, work.Height - ScreenMargin);
            MaxWidth = Math.Max(300, work.Width - ScreenMargin);
            FontFamily = new FontFamily("Segoe UI");

            ImageSource spotifyIcon = LoadSpotifyIcon(services.Paths);
            ImageSource ownIcon = LoadOwnIcon();
            if (ownIcon != null) Icon = ownIcon;
            else if (spotifyIcon != null) Icon = spotifyIcon;

            root = new Grid();
            LinearGradientBrush gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(0, 1);
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1F3A2A"), 0));
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#121212"), 0.45));
            gradient.Freeze();
            root.Background = gradient;
            root.SizeChanged += delegate
            {
                root.Clip = new RectangleGeometry(new Rect(0, 0, root.ActualWidth, root.ActualHeight), 12, 12);
            };
            // Fila 2 (tarjetas) en estrella: si no cabe todo, solo esa fila se achica y se desplaza;
            // la barra de titulo y el pie quedan siempre visibles.
            for (int i = 0; i < 5; i++)
            {
                GridLength h = i == 2 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
                root.RowDefinitions.Add(new RowDefinition { Height = h });
            }

            FrameworkElement bar = BuildTitleBar(spotifyIcon);
            Grid.SetRow(bar, 0);
            root.Children.Add(bar);

            FrameworkElement header = BuildHeader();
            Grid.SetRow(header, 1);
            root.Children.Add(header);

            cardsHost = new Border();
            cardsHost.HorizontalAlignment = HorizontalAlignment.Center;
            Grid.SetRow(cardsHost, 2);
            root.Children.Add(cardsHost);

            status = Theme.Text("", 13, Theme.TextSecondary, false);
            status.TextAlignment = TextAlignment.Center;
            status.HorizontalAlignment = HorizontalAlignment.Center;
            status.Margin = new Thickness(SidePadding, 18, SidePadding, 18);
            status.MinHeight = 34;
            Grid.SetRow(status, 3);
            root.Children.Add(status);

            FrameworkElement footer = BuildFooter();
            Grid.SetRow(footer, 4);
            root.Children.Add(footer);

            Content = root;
            Theme.MakeBorderless(this, 12, "#121212");

            // Animacion de entrada: opacidad 0 -> 1 y escala 0,96 -> 1 en 180 ms.
            FrameworkElement outer = Content as FrameworkElement;
            ScaleTransform scale = new ScaleTransform(0.96, 0.96);
            if (outer != null)
            {
                outer.Opacity = 0;
                outer.RenderTransformOrigin = new Point(0.5, 0.5);
                outer.RenderTransform = scale;
            }
            Loaded += delegate
            {
                Activate();
                if (outer == null) return;
                Duration d = new Duration(TimeSpan.FromMilliseconds(180));
                QuadraticEase ease = new QuadraticEase();
                ease.EasingMode = EasingMode.EaseOut;
                DoubleAnimation fade = new DoubleAnimation(0, 1, d);
                fade.EasingFunction = ease;
                DoubleAnimation grow = new DoubleAnimation(0.96, 1, d);
                grow.EasingFunction = ease;
                outer.BeginAnimation(UIElement.OpacityProperty, fade);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
            };

            // La primera vez se centra en el area de trabajo; despues, si cambia el tamano (mas
            // columnas o filas), se mantiene centrada donde estaba. Siempre queda dentro de la pantalla.
            SizeChanged += delegate(object s, SizeChangedEventArgs e)
            {
                Rect wa = SystemParameters.WorkArea;
                double left;
                double top;
                if (!positioned || e.PreviousSize.Width <= 0 || e.PreviousSize.Height <= 0)
                {
                    left = wa.Left + (wa.Width - e.NewSize.Width) / 2;
                    top = wa.Top + (wa.Height - e.NewSize.Height) / 2;
                    positioned = true;
                }
                else
                {
                    left = Left - (e.NewSize.Width - e.PreviousSize.Width) / 2;
                    top = Top - (e.NewSize.Height - e.PreviousSize.Height) / 2;
                }
                Left = Clamp(left, wa.Left, wa.Right - e.NewSize.Width);
                Top = Clamp(top, wa.Top, wa.Bottom - e.NewSize.Height);
            };

            PreviewKeyDown += OnKey;
            // Mientras se cambia de cuenta no se puede cerrar (Esc, Alt+F4, X): el trabajo en segundo
            // plano quedaria a medias. Solo el cierre propio tras abrir Spotify pasa.
            Closing += delegate(object s, System.ComponentModel.CancelEventArgs e)
            {
                if (busy && !closingByProgram) e.Cancel = true;
            };
            Closed += delegate { closed = true; };

            Refresh();
        }

        // True mientras se cambia o se guarda una cuenta (no se debe cerrar el programa).
        public bool IsBusy { get { return busy; } }

        // Pone el interruptor "Mostrar al abrir Spotify" sin disparar ToggleChanged.
        public void SetToggle(bool on)
        {
            if (toggle != null) toggle.SetOn(on);
        }

        // Vuelve a armar las tarjetas desde el almacen y relee la cuenta activa.
        public void Refresh()
        {
            accounts = new List<Account>();
            SetStatus("");
            int columns = MinColumns;
            double maxWindowWidth = Math.Max(300, SystemParameters.WorkArea.Width - ScreenMargin);

            if (services.Paths.IsStoreVersion)
            {
                cardsHost.Child = FixedMessage(Strings.Get("errStoreVersion"));
            }
            else if (!services.Paths.IsInstalled)
            {
                cardsHost.Child = FixedMessage(Strings.Get("errSpotifyNotInstalled"));
            }
            else
            {
                try
                {
                    accounts = services.Store.LoadAll();
                }
                catch (Exception ex)
                {
                    Log.Error("selector: leer cuentas", ex);
                    accounts = new List<Account>();
                }
                string current = SafeCurrentUsername();

                int fitColumns = (int)Math.Floor((maxWindowWidth - ShadowMargin - 2 * SidePadding + Gap) / CellWidth);
                GridLayout layout = CardGrid.Compute(accounts.Count, fitColumns);
                columns = layout.Columns;

                UniformGrid grid = new UniformGrid();
                grid.Columns = layout.Columns;
                grid.Width = layout.Columns * CellWidth;
                grid.HorizontalAlignment = HorizontalAlignment.Center;
                foreach (Account a in accounts)
                {
                    bool active = current != null
                        && string.Equals(a.Username, current, StringComparison.OrdinalIgnoreCase);
                    AccountCard card = new AccountCard(a, active);
                    card.Margin = new Thickness(Gap / 2);
                    Account target = a;
                    card.Picked += delegate { Pick(target); };
                    card.EditRequested += delegate { EditAccount(target); };
                    card.DeleteRequested += delegate { DeleteAccount(target); };
                    grid.Children.Add(card);
                }
                AccountCard addCard = AccountCard.CreateAddCard();
                addCard.Margin = new Thickness(Gap / 2);
                addCard.Picked += delegate { OnAdd(); };
                grid.Children.Add(addCard);

                // Las tarjetas siempre van en un ScrollViewer: se desplazan con mas de 3 filas
                // y tambien cuando la pantalla es demasiado baja para mostrarlas todas.
                ScrollViewer sv = new ScrollViewer();
                sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                sv.Focusable = false;
                if (layout.Scroll) sv.MaxHeight = layout.VisibleRows * CellHeight;
                Style barStyle = DarkScrollBarStyle();
                if (barStyle != null)
                {
                    // La barra oscura ocupa siempre su lugar (sin pulgar si no hay nada que desplazar)
                    // y el margen izquierdo igual la compensa: las tarjetas quedan centradas.
                    sv.Resources[typeof(ScrollBar)] = barStyle;
                    sv.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    grid.Margin = new Thickness(ScrollBarSpace, 0, 0, 0);
                }
                else
                {
                    sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                }
                sv.Content = grid;
                cardsHost.Child = sv;

                if (accounts.Count == 0) SetStatus(Strings.Get("emptyState"));
            }

            // El relleno lateral (56) ya incluye el lugar de la barra de desplazamiento.
            double contentWidth = Math.Max(columns, MinColumns) * CellWidth - Gap + 2 * SidePadding;
            Width = Math.Min(contentWidth + ShadowMargin, maxWindowWidth);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (max < min) return min;
            return Math.Max(min, Math.Min(max, value));
        }

        private TextBlock FixedMessage(string text)
        {
            TextBlock t = Theme.Text(text, 15, Theme.TextPrimary, false);
            t.TextAlignment = TextAlignment.Center;
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.MaxWidth = 480;
            t.Margin = new Thickness(SidePadding, 40, SidePadding, 40);
            return t;
        }

        // ---------- Construccion ----------

        private FrameworkElement BuildTitleBar(ImageSource spotifyIcon)
        {
            Grid bar = new Grid();
            bar.Height = 44;
            bar.Background = Brushes.Transparent;
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState != MouseButtonState.Pressed) return;
                try { DragMove(); }
                catch (Exception) { }
            };

            StackPanel left = new StackPanel();
            left.Orientation = Orientation.Horizontal;
            left.VerticalAlignment = VerticalAlignment.Center;
            left.Margin = new Thickness(20, 0, 0, 0);
            if (spotifyIcon != null)
            {
                Image img = new Image();
                img.Source = spotifyIcon;
                img.Width = 20;
                img.Height = 20;
                img.VerticalAlignment = VerticalAlignment.Center;
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                left.Children.Add(img);
            }
            TextBlock name = Theme.Text(Strings.Get("spotify"), 14, Theme.TextSecondary, false);
            name.FontWeight = FontWeights.SemiBold;
            name.VerticalAlignment = VerticalAlignment.Center;
            name.Margin = new Thickness(spotifyIcon != null ? 10 : 0, 0, 0, 0);
            left.Children.Add(name);
            Grid.SetColumn(left, 0);
            bar.Children.Add(left);

            StackPanel right = new StackPanel();
            right.Orientation = Orientation.Horizontal;
            right.VerticalAlignment = VerticalAlignment.Center;
            right.Margin = new Thickness(0, 0, 12, 0);
            Button min = AccountCard.IconButton("", 36, 32, 12, Strings.Get("minimize"));
            Button close = AccountCard.IconButton("", 36, 32, 12, Strings.Get("close"));
            min.Click += delegate { WindowState = WindowState.Minimized; };
            close.Click += delegate { Close(); };
            right.Children.Add(min);
            right.Children.Add(close);
            Grid.SetColumn(right, 1);
            bar.Children.Add(right);
            return bar;
        }

        private FrameworkElement BuildHeader()
        {
            StackPanel header = new StackPanel();
            header.Orientation = Orientation.Vertical;
            header.Margin = new Thickness(SidePadding, 8, SidePadding, 28);

            TextBlock title = Theme.Text(Strings.Get("title"), 30, Theme.TextPrimary, true);
            title.TextAlignment = TextAlignment.Center;
            title.HorizontalAlignment = HorizontalAlignment.Center;
            header.Children.Add(title);

            TextBlock subtitle = Theme.Text(Strings.Get("subtitle"), 14, Theme.TextSecondary, false);
            subtitle.TextAlignment = TextAlignment.Center;
            subtitle.HorizontalAlignment = HorizontalAlignment.Center;
            subtitle.Margin = new Thickness(0, 10, 0, 0);
            header.Children.Add(subtitle);
            return header;
        }

        private FrameworkElement BuildFooter()
        {
            Border footer = new Border();
            footer.BorderBrush = Theme.Brush("#2A2A2A");
            footer.BorderThickness = new Thickness(0, 1, 0, 0);
            footer.Padding = new Thickness(24, 16, 24, 16);

            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel left = new StackPanel();
            left.Orientation = Orientation.Horizontal;
            left.VerticalAlignment = VerticalAlignment.Center;
            bool initial = services.Settings != null && services.Settings.ShowOnSpotifyOpen;
            toggle = new PillToggle(initial);
            toggle.VerticalAlignment = VerticalAlignment.Center;
            toggle.Changed += delegate(bool on)
            {
                if (ToggleChanged != null) ToggleChanged(on);
            };
            left.Children.Add(toggle);
            TextBlock label = Theme.Text(Strings.Get("toggle"), 14, Theme.TextSecondary, false);
            label.VerticalAlignment = VerticalAlignment.Center;
            label.Margin = new Thickness(12, 0, 0, 0);
            label.Cursor = Cursors.Hand;
            PillToggle pill = toggle;
            label.MouseLeftButtonUp += delegate { pill.Toggle(); };
            left.Children.Add(label);
            Grid.SetColumn(left, 0);
            row.Children.Add(left);

            Button diag = Theme.LinkButton("");
            StackPanel diagContent = new StackPanel();
            diagContent.Orientation = Orientation.Horizontal;
            TextBlock wrench = new TextBlock();
            wrench.Text = "";
            wrench.FontFamily = Theme.IconFont;
            wrench.FontSize = 14;
            wrench.VerticalAlignment = VerticalAlignment.Center;
            diagContent.Children.Add(wrench);
            TextBlock diagText = new TextBlock();
            diagText.Text = Strings.Get("diagnostics");
            diagText.FontFamily = new FontFamily("Segoe UI");
            diagText.FontSize = 14;
            diagText.VerticalAlignment = VerticalAlignment.Center;
            diagText.Margin = new Thickness(8, 0, 0, 0);
            diagContent.Children.Add(diagText);
            diag.Content = diagContent;
            diag.Padding = new Thickness(4);
            diag.Focusable = false;
            diag.VerticalAlignment = VerticalAlignment.Center;
            diag.Click += delegate { ShowDiagnostics(); };
            Grid.SetColumn(diag, 1);
            row.Children.Add(diag);

            footer.Child = row;
            return footer;
        }

        // Icono propio (recurso incrustado) para la barra de tareas y alt-tab.
        private static ImageSource LoadOwnIcon()
        {
            try
            {
                using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("WhosListening.ico"))
                {
                    if (stream == null) return null;
                    BitmapFrame frame = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    frame.Freeze();
                    return frame;
                }
            }
            catch (Exception ex)
            {
                Log.Error("selector: icono propio", ex);
                return null;
            }
        }

        private static ImageSource LoadSpotifyIcon(SpotifyPaths paths)
        {
            try
            {
                if (paths == null || !File.Exists(paths.Exe)) return null;
                using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(paths.Exe))
                {
                    if (icon == null) return null;
                    BitmapSource src = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    src.Freeze();
                    return src;
                }
            }
            catch (Exception ex)
            {
                Log.Error("selector: icono de Spotify", ex);
                return null;
            }
        }

        // Barra de desplazamiento delgada y oscura. Si algo falla, se usa la de Windows.
        private static Style DarkScrollBarStyle()
        {
            const string xaml =
                "<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'"
                + " xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ScrollBar'>"
                + "<Setter Property='Width' Value='10'/>"
                + "<Setter Property='MinWidth' Value='10'/>"
                + "<Setter Property='Template'><Setter.Value>"
                + "<ControlTemplate TargetType='ScrollBar'>"
                + "<Track x:Name='PART_Track' IsDirectionReversed='True'>"
                + "<Track.Thumb><Thumb><Thumb.Template><ControlTemplate TargetType='Thumb'>"
                + "<Border x:Name='bd' CornerRadius='4' Background='#535353' Margin='2,0,2,0'/>"
                + "<ControlTemplate.Triggers>"
                + "<Trigger Property='IsMouseOver' Value='True'><Setter TargetName='bd' Property='Background' Value='#7F7F7F'/></Trigger>"
                + "<Trigger Property='IsDragging' Value='True'><Setter TargetName='bd' Property='Background' Value='#B3B3B3'/></Trigger>"
                + "</ControlTemplate.Triggers>"
                + "</ControlTemplate></Thumb.Template></Thumb></Track.Thumb>"
                + "</Track>"
                + "<ControlTemplate.Triggers>"
                + "<Trigger Property='IsEnabled' Value='False'><Setter TargetName='PART_Track' Property='Visibility' Value='Hidden'/></Trigger>"
                + "</ControlTemplate.Triggers>"
                + "</ControlTemplate>"
                + "</Setter.Value></Setter>"
                + "</Style>";
            try
            {
                return (Style)XamlReader.Parse(xaml);
            }
            catch (Exception ex)
            {
                Log.Error("selector: estilo de la barra de desplazamiento", ex);
                return null;
            }
        }

        // ---------- Acciones ----------

        private void OnKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                if (!busy) Close();
                return;
            }
            int n = -1;
            if (e.Key >= Key.D1 && e.Key <= Key.D9) n = (int)e.Key - (int)Key.D1;
            else if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad9) n = (int)e.Key - (int)Key.NumPad1;
            if (n < 0) return;
            e.Handled = true;
            if (busy || !root.IsEnabled) return;
            if (n < accounts.Count) Pick(accounts[n]);
        }

        private void Pick(Account account)
        {
            if (busy) return;
            string user;
            bool saved;
            try
            {
                user = services.Switcher.CurrentUsername();
                saved = user != null && services.Store.FindByUsername(user) != null;
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
            if (user != null && !saved)
            {
                // La sesion actual no esta guardada: cambiar de cuenta la perderia.
                // Si / No / Cancelar (el enlace propio de Choose, o Esc) cancela el cambio.
                int choice = DarkDialog.Choose(this, Strings.Get("saveFirstPrompt"), new string[] { "yes", "no" });
                if (choice == 0)
                {
                    SaveCurrent(false, delegate { DoPick(account); });
                    return;
                }
                if (choice != 1) return;
            }
            DoPick(account);
        }

        private void DoPick(Account account)
        {
            if (busy) return;
            SetBusy(true);
            SetStatus(Strings.Format("statusSwitching", account.Label));
            RunInBackground<SwitchResult>(
                delegate
                {
                    Account current = services.Store.FindByUsername(services.Switcher.CurrentUsername());
                    return services.Switcher.SwitchTo(account, current);
                },
                delegate(SwitchResult result)
                {
                    switch (result)
                    {
                        case SwitchResult.Switched:
                        case SwitchResult.StartedSameAccount:
                            SetStatus(Strings.Get("statusOpening"));
                            CloseAfter(600);
                            break;
                        case SwitchResult.AlreadyActive:
                            CloseFromProgram();
                            break;
                        case SwitchResult.SpotifyNotInstalled:
                            SetStatus(Strings.Get("errSpotifyNotInstalled"));
                            SetBusy(false);
                            break;
                        case SwitchResult.SpotifyWouldNotClose:
                            SetStatus(Strings.Get("errSpotifyWouldNotClose"));
                            SetBusy(false);
                            break;
                        default:
                            SetStatus(Strings.Get("errSwitchFailed"));
                            SetBusy(false);
                            break;
                    }
                });
        }

        private void OnAdd()
        {
            if (busy) return;
            int choice = DarkDialog.Choose(this, Strings.Get("addAccount"), new string[] { "addSaveCurrent", "addPrepareNew" });
            if (choice == 0) SaveCurrent(true, null);
            else if (choice == 1) PrepareNew();
        }

        // Guarda la cuenta con sesion iniciada en Spotify. Cierra Spotify antes de leer prefs.
        // Si ya existe una cuenta con ese usuario, actualiza sus credenciales y su nombre.
        private void SaveCurrent(bool restartSpotify, Action then)
        {
            string user;
            Account existing;
            try
            {
                user = services.Switcher.CurrentUsername();
                existing = user != null ? services.Store.FindByUsername(user) : null;
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
            if (user == null)
            {
                SetStatus(Strings.Format("errorWithDetail", Strings.Get("errNoCurrentAccount")));
                return;
            }

            string name = DarkDialog.AskText(this, Strings.Get("accountNamePrompt"), existing != null ? existing.Label : user);
            if (name == null) return;

            SetBusy(true);
            RunInBackground<SaveOutcome>(
                delegate { return SaveCurrentInBackground(name, restartSpotify); },
                delegate(SaveOutcome outcome)
                {
                    SetBusy(false);
                    switch (outcome)
                    {
                        case SaveOutcome.Saved:
                            Refresh();
                            SetStatus(Strings.Format("statusSaved", name));
                            if (then != null) then();
                            break;
                        case SaveOutcome.WouldNotClose:
                            SetStatus(Strings.Get("errSpotifyWouldNotClose"));
                            break;
                        case SaveOutcome.NoAccount:
                            SetStatus(Strings.Format("errorWithDetail", Strings.Get("errNoCurrentAccount")));
                            break;
                        default:
                            SetStatus(Strings.Format("errorWithDetail", Strings.Get("errNoCredentials")));
                            break;
                    }
                });
        }

        private enum SaveOutcome
        {
            Saved,
            WouldNotClose,
            NoAccount,
            NoCredentials
        }

        // Corre en segundo plano. Solo guarda si prefs tiene credenciales de verdad, para no pisar
        // una cuenta guardada con valores vacios. Si cerro Spotify y no guardo, lo vuelve a abrir.
        private SaveOutcome SaveCurrentInBackground(string name, bool restartSpotify)
        {
            ISpotifyProcess proc = services.Process;
            bool closedSpotify = false;
            if (proc.IsRunning())
            {
                if (!proc.CloseAndWait(TimeSpan.FromSeconds(10))) return SaveOutcome.WouldNotClose;
                closedSpotify = true;
            }

            SaveOutcome outcome;
            try
            {
                SpotifyPrefs prefs = SpotifyPrefs.Load(services.Paths.PrefsFile);
                string username = prefs.Username;
                Dictionary<string, string> auth = prefs.GetAuth();
                if (username == null)
                {
                    outcome = SaveOutcome.NoAccount;
                }
                else if (!AccountSwitcher.HasCredentials(auth))
                {
                    outcome = SaveOutcome.NoCredentials;
                }
                else
                {
                    Account target = services.Store.FindByUsername(username);
                    if (target != null)
                    {
                        target.Label = name;
                        target.Auth = auth;
                        target.SavedAtUtc = DateTime.UtcNow;
                        services.Store.Save(target);
                    }
                    else
                    {
                        services.Store.Add(name, username, auth);
                    }
                    outcome = SaveOutcome.Saved;
                }
            }
            catch (Exception)
            {
                if (closedSpotify) TryStartSpotify();
                throw;
            }

            bool start = outcome == SaveOutcome.Saved ? restartSpotify : closedSpotify;
            if (start) proc.Start(services.Paths.Exe);
            return outcome;
        }

        private void TryStartSpotify()
        {
            try
            {
                services.Process.Start(services.Paths.Exe);
            }
            catch (Exception ex)
            {
                Log.Error("selector: volver a abrir Spotify", ex);
            }
        }

        private void PrepareNew()
        {
            string user;
            bool saved;
            try
            {
                user = services.Switcher.CurrentUsername();
                saved = user != null && services.Store.FindByUsername(user) != null;
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
            if (user != null && !saved)
            {
                // Si / No / Cancelar: Esc o "Cancelar" (el enlace propio de Choose) cancelan todo,
                // para no borrar por accidente una sesion sin guardar.
                int choice = DarkDialog.Choose(this, Strings.Get("saveFirstPrompt"), new string[] { "yes", "no" });
                if (choice == 0)
                {
                    SaveCurrent(false, DoPrepareNew);
                    return;
                }
                if (choice != 1) return;
            }
            DoPrepareNew();
        }

        private void DoPrepareNew()
        {
            SetBusy(true);
            RunInBackground<bool>(
                delegate
                {
                    // Antes de borrar la sesion, actualizar las credenciales guardadas de la cuenta
                    // en uso (como refreshSaved al cambiar de cuenta). Spotify debe estar cerrado.
                    ISpotifyProcess proc = services.Process;
                    if (proc.IsRunning() && !proc.CloseAndWait(TimeSpan.FromSeconds(10))) return false;
                    SpotifyPrefs prefs = SpotifyPrefs.Load(services.Paths.PrefsFile);
                    string username = prefs.Username;
                    Account current = username != null ? services.Store.FindByUsername(username) : null;
                    if (current != null)
                    {
                        Dictionary<string, string> auth = prefs.GetAuth();
                        if (AccountSwitcher.HasCredentials(auth))
                        {
                            current.Auth = auth;
                            current.SavedAtUtc = DateTime.UtcNow;
                            services.Store.Save(current);
                        }
                    }
                    return services.Switcher.PrepareNewLogin();
                },
                delegate(bool ok)
                {
                    SetBusy(false);
                    SetStatus(ok ? Strings.Get("statusPrepared") : Strings.Get("errSpotifyWouldNotClose"));
                });
        }

        private void EditAccount(Account account)
        {
            if (busy) return;
            string name = DarkDialog.AskText(this, Strings.Get("accountNamePrompt"), account.Label);
            if (name == null) return;
            try
            {
                services.Store.Rename(account.Id, name);
                Refresh();
                SetStatus(Strings.Format("statusRenamed", name));
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }

        private void DeleteAccount(Account account)
        {
            if (busy) return;
            if (!DarkDialog.Confirm(this, Strings.Format("deleteConfirm", account.Label), "delete", "cancel")) return;
            try
            {
                services.Store.Delete(account.Id);
                Refresh();
                SetStatus(Strings.Format("statusDeleted", account.Label));
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }

        private void ShowDiagnostics()
        {
            try
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string report = Diagnostics.BuildReport(services.Paths, services.Store, version);
                DarkDialog.ShowText(this, Strings.Get("diagnosticsTitle"), report);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }

        // ---------- Utilidades ----------

        private void RunInBackground<T>(Func<T> work, Action<T> onDone)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                T result = default(T);
                Exception error = null;
                try
                {
                    result = work();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (closed) return;
                    if (error != null)
                    {
                        Fail(error);
                        return;
                    }
                    try
                    {
                        onDone(result);
                    }
                    catch (Exception ex)
                    {
                        Fail(ex);
                    }
                }));
            });
        }

        private void Fail(Exception ex)
        {
            Log.Error("selector", ex);
            SetStatus(Strings.Format("errorWithDetail", ex.Message));
            SetBusy(false);
        }

        private void SetBusy(bool value)
        {
            busy = value;
            root.IsEnabled = !value;
            cardsHost.Opacity = value ? 0.6 : 1;
        }

        private void SetStatus(string text)
        {
            status.Text = text != null ? text : "";
        }

        private void CloseAfter(int milliseconds)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(milliseconds);
            timer.Tick += delegate
            {
                timer.Stop();
                CloseFromProgram();
            };
            timer.Start();
        }

        // Cierre propio tras abrir Spotify: pasa aunque la ventana siga ocupada.
        private void CloseFromProgram()
        {
            if (closed) return;
            closingByProgram = true;
            Close();
        }

        private string SafeCurrentUsername()
        {
            try
            {
                return services.Switcher.CurrentUsername();
            }
            catch (Exception ex)
            {
                Log.Error("selector: leer usuario actual", ex);
                return null;
            }
        }
    }

    // Interruptor tipo pildora: verde con la perilla a la derecha cuando esta activo.
    internal class PillToggle : Border
    {
        private const double KnobTravel = 18;
        private static readonly Brush OffTrack = Theme.Brush("#535353");
        private static readonly Brush OnKnob = Theme.Brush("#121212");

        private readonly TranslateTransform shift = new TranslateTransform();
        private readonly Ellipse knob;
        private bool isOn;

        public event Action<bool> Changed;

        public PillToggle(bool initial)
        {
            Width = 40;
            Height = 22;
            CornerRadius = new CornerRadius(11);
            Cursor = Cursors.Hand;

            knob = new Ellipse();
            knob.Width = 16;
            knob.Height = 16;
            knob.HorizontalAlignment = HorizontalAlignment.Left;
            knob.VerticalAlignment = VerticalAlignment.Center;
            knob.Margin = new Thickness(3, 0, 0, 0);
            knob.RenderTransform = shift;
            Child = knob;

            isOn = initial;
            Apply(false);
            MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Toggle();
            };
        }

        public bool IsOn { get { return isOn; } }

        // Cambia el estado sin disparar Changed (para reflejar un cambio hecho desde otro lugar).
        public void SetOn(bool on)
        {
            if (isOn == on) return;
            isOn = on;
            Apply(true);
        }

        public void Toggle()
        {
            isOn = !isOn;
            Apply(true);
            if (Changed != null) Changed(isOn);
        }

        private void Apply(bool animate)
        {
            // Encendido: pista verde y perilla oscura (como la referencia). Apagado: gris y blanca.
            Background = isOn ? Theme.Green : OffTrack;
            knob.Fill = isOn ? OnKnob : Brushes.White;
            double x = isOn ? KnobTravel : 0;
            if (animate)
            {
                DoubleAnimation a = new DoubleAnimation(x, new Duration(TimeSpan.FromMilliseconds(120)));
                shift.BeginAnimation(TranslateTransform.XProperty, a);
            }
            else
            {
                shift.BeginAnimation(TranslateTransform.XProperty, null);
                shift.X = x;
            }
        }
    }
}
