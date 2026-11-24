using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace WhosListening
{
    // Una tarjeta del selector: una cuenta guardada, o la tarjeta "Agregar cuenta" (Account == null).
    public class AccountCard : Border
    {
        public const double CardWidth = 190;
        public const double CardHeight = 245;
        public const double AvatarSize = 110;
        private const double RingThickness = 3;
        private const double RingGap = 4;
        private const double AvatarHost = AvatarSize + 2 * (RingThickness + RingGap);

        public static readonly string[] Palette = new string[]
        {
            "#1ED760", "#E91429", "#509BF5", "#F59B23", "#AF2896", "#FF6437", "#27856A", "#E8115B"
        };

        private static readonly Brush CardNormal = Theme.Brush("#1E1E1E");
        private static readonly Brush AddCircle = Theme.Brush("#2A2A2A");
        private static readonly Brush InitialsColor = Theme.Brush("#121212");

        public event Action Picked;
        public event Action EditRequested;
        public event Action DeleteRequested;

        private readonly Account account;
        private bool pressed;
        private FrameworkElement playButton;
        private FrameworkElement actions;
        private TextBlock plus;

        public AccountCard(Account account, bool active)
        {
            if (account == null) throw new ArgumentNullException("account");
            this.account = account;
            SetupCard();
            BuildAccount(active);
        }

        private AccountCard()
        {
            this.account = null;
            SetupCard();
            BuildAdd();
        }

        public static AccountCard CreateAddCard()
        {
            return new AccountCard();
        }

        public Account Account { get { return account; } }

        public bool IsAddCard { get { return account == null; } }

        // Una palabra: su inicial. Dos o mas: la inicial de las dos primeras. En mayusculas.
        public static string Initials(string label)
        {
            if (label == null) return "?";
            string[] words = label.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "?";
            string s = StringInfo.GetNextTextElement(words[0]);
            if (words.Length >= 2) s += StringInfo.GetNextTextElement(words[1]);
            return s.ToUpper(CultureInfo.CurrentCulture);
        }

        // Color estable a partir del id: suma de los caracteres modulo 8.
        public static int PaletteIndex(string id)
        {
            if (id == null) return 0;
            int sum = 0;
            foreach (char c in id) sum += c;
            return sum % Palette.Length;
        }

        private void SetupCard()
        {
            Width = CardWidth;
            Height = CardHeight;
            CornerRadius = new CornerRadius(8);
            Background = CardNormal;
            Cursor = Cursors.Hand;

            MouseEnter += delegate { SetHover(true); };
            MouseLeave += delegate
            {
                pressed = false;
                SetHover(false);
            };
            MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                pressed = true;
                e.Handled = true;
            };
            MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                if (!pressed) return;
                pressed = false;
                e.Handled = true;
                if (Picked != null) Picked();
            };
        }

        private void SetHover(bool on)
        {
            Background = on ? Theme.CardHover : CardNormal;
            Visibility v = on ? Visibility.Visible : Visibility.Collapsed;
            if (playButton != null) playButton.Visibility = v;
            if (actions != null) actions.Visibility = v;
            if (plus != null) plus.Foreground = on ? Theme.TextPrimary : Theme.TextSecondary;
        }

        private void BuildAccount(bool active)
        {
            Grid root = new Grid();

            StackPanel column = new StackPanel();
            column.Orientation = Orientation.Vertical;

            Grid avatar = new Grid();
            avatar.Width = AvatarHost;
            avatar.Height = AvatarHost;
            avatar.Margin = new Thickness(0, 18, 0, 0);
            avatar.HorizontalAlignment = HorizontalAlignment.Center;

            if (active)
            {
                Ellipse ring = new Ellipse();
                ring.Width = AvatarHost;
                ring.Height = AvatarHost;
                ring.Stroke = Theme.Green;
                ring.StrokeThickness = RingThickness;
                avatar.Children.Add(ring);
            }

            Ellipse circle = new Ellipse();
            circle.Width = AvatarSize;
            circle.Height = AvatarSize;
            circle.Fill = Theme.Brush(Palette[PaletteIndex(account.Id)]);
            circle.HorizontalAlignment = HorizontalAlignment.Center;
            circle.VerticalAlignment = VerticalAlignment.Center;
            DropShadowEffect shadow = new DropShadowEffect();
            shadow.BlurRadius = 18;
            shadow.ShadowDepth = 4;
            shadow.Direction = 270;
            shadow.Opacity = 0.45;
            shadow.Color = Colors.Black;
            circle.Effect = shadow;
            avatar.Children.Add(circle);

            TextBlock initials = new TextBlock();
            initials.Text = Initials(account.Label);
            initials.FontFamily = new FontFamily("Segoe UI");
            initials.FontSize = 44;
            initials.FontWeight = FontWeights.Bold;
            initials.Foreground = InitialsColor;
            initials.HorizontalAlignment = HorizontalAlignment.Center;
            initials.VerticalAlignment = VerticalAlignment.Center;
            avatar.Children.Add(initials);

            playButton = BuildPlayButton();
            avatar.Children.Add(playButton);

            column.Children.Add(avatar);
            column.Children.Add(MakeLabel(account.Label));
            column.Children.Add(MakeSubLabel(active ? Strings.Get("inUse") : Strings.Get("profile"), active ? Theme.Green : Theme.TextSecondary));
            root.Children.Add(column);

            StackPanel bar = new StackPanel();
            bar.Orientation = Orientation.Horizontal;
            bar.HorizontalAlignment = HorizontalAlignment.Right;
            bar.VerticalAlignment = VerticalAlignment.Top;
            bar.Margin = new Thickness(0, 6, 6, 0);
            bar.Visibility = Visibility.Collapsed;
            Button edit = IconButton("", 28, 28, 13, Strings.Get("editNameTip"));
            Button delete = IconButton("", 28, 28, 13, Strings.Get("deleteTip"));
            edit.Click += delegate { if (EditRequested != null) EditRequested(); };
            delete.Click += delegate { if (DeleteRequested != null) DeleteRequested(); };
            bar.Children.Add(edit);
            bar.Children.Add(delete);
            actions = bar;
            root.Children.Add(bar);

            Child = root;
        }

        private void BuildAdd()
        {
            StackPanel column = new StackPanel();
            column.Orientation = Orientation.Vertical;

            Grid avatar = new Grid();
            avatar.Width = AvatarHost;
            avatar.Height = AvatarHost;
            avatar.Margin = new Thickness(0, 18, 0, 0);
            avatar.HorizontalAlignment = HorizontalAlignment.Center;

            Ellipse circle = new Ellipse();
            circle.Width = AvatarHost;
            circle.Height = AvatarHost;
            circle.Fill = AddCircle;
            avatar.Children.Add(circle);

            plus = new TextBlock();
            plus.Text = "";
            plus.FontFamily = Theme.IconFont;
            plus.FontSize = 36;
            plus.Foreground = Theme.TextSecondary;
            plus.HorizontalAlignment = HorizontalAlignment.Center;
            plus.VerticalAlignment = VerticalAlignment.Center;
            avatar.Children.Add(plus);

            column.Children.Add(avatar);
            column.Children.Add(MakeLabel(Strings.Get("addAccount")));
            column.Children.Add(MakeSubLabel(Strings.Get("addAccountSub"), Theme.TextSecondary));
            Child = column;
        }

        private static TextBlock MakeLabel(string text)
        {
            TextBlock t = Theme.Text(text, 17, Theme.TextPrimary, true);
            t.TextWrapping = TextWrapping.NoWrap;
            t.TextTrimming = TextTrimming.CharacterEllipsis;
            t.TextAlignment = TextAlignment.Center;
            t.HorizontalAlignment = HorizontalAlignment.Stretch;
            t.Margin = new Thickness(12, 18, 12, 0);
            return t;
        }

        private static TextBlock MakeSubLabel(string text, Brush color)
        {
            TextBlock t = Theme.Text(text, 14, color, false);
            t.TextWrapping = TextWrapping.NoWrap;
            t.TextTrimming = TextTrimming.CharacterEllipsis;
            t.TextAlignment = TextAlignment.Center;
            t.HorizontalAlignment = HorizontalAlignment.Stretch;
            t.Margin = new Thickness(12, 4, 12, 0);
            return t;
        }

        // Boton verde de reproducir. No es un Button: el clic sube a la tarjeta y la elige.
        private static FrameworkElement BuildPlayButton()
        {
            Border play = new Border();
            play.Width = 48;
            play.Height = 48;
            play.CornerRadius = new CornerRadius(24);
            play.Background = Theme.Green;
            play.HorizontalAlignment = HorizontalAlignment.Right;
            play.VerticalAlignment = VerticalAlignment.Bottom;
            play.Visibility = Visibility.Collapsed;
            DropShadowEffect shadow = new DropShadowEffect();
            shadow.BlurRadius = 12;
            shadow.ShadowDepth = 3;
            shadow.Direction = 270;
            shadow.Opacity = 0.5;
            shadow.Color = Colors.Black;
            play.Effect = shadow;

            TextBlock glyph = new TextBlock();
            glyph.Text = "";
            glyph.FontFamily = Theme.IconFont;
            glyph.FontSize = 20;
            glyph.Foreground = Brushes.Black;
            glyph.HorizontalAlignment = HorizontalAlignment.Center;
            glyph.VerticalAlignment = VerticalAlignment.Center;
            glyph.Margin = new Thickness(2, 0, 0, 0);
            play.Child = glyph;
            return play;
        }

        // Boton de icono sin fondo: gris, blanco al pasar el mouse.
        internal static Button IconButton(string glyph, double width, double height, double glyphSize, string tooltip)
        {
            TextBlock t = new TextBlock();
            t.Text = glyph;
            t.FontFamily = Theme.IconFont;
            t.FontSize = glyphSize;
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;

            Button b = new Button();
            b.Content = t;
            b.Width = width;
            b.Height = height;
            b.Foreground = Theme.TextSecondary;
            b.Background = Brushes.Transparent;
            b.BorderThickness = new Thickness(0);
            b.Cursor = Cursors.Hand;
            b.FocusVisualStyle = null;
            b.Focusable = false;
            if (tooltip != null) b.ToolTip = tooltip;

            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            ControlTemplate template = new ControlTemplate(typeof(Button));
            template.VisualTree = border;
            b.Template = template;

            b.MouseEnter += delegate { b.Foreground = Theme.TextPrimary; };
            b.MouseLeave += delegate { b.Foreground = Theme.TextSecondary; };
            return b;
        }
    }
}
