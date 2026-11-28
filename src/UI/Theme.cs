using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace WhosListening
{
    public static class Theme
    {
        public static readonly FontFamily IconFont = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

        public static readonly Brush Background = Brush("#121212");
        public static readonly Brush Card = Brush("#181818");
        public static readonly Brush CardHover = Brush("#282828");
        public static readonly Brush Green = Brush("#1DB954");
        public static readonly Brush TextPrimary = Brush("#FFFFFF");
        public static readonly Brush TextSecondary = Brush("#B3B3B3");

        public static SolidColorBrush Brush(string hex)
        {
            SolidColorBrush b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }

        // Convierte la ventana en una ventana sin borde, con esquinas redondeadas y sombra.
        public static void MakeBorderless(Window w, double cornerRadius)
        {
            MakeBorderless(w, cornerRadius, "#121212");
        }

        public static void MakeBorderless(Window w, double cornerRadius, string backgroundHex)
        {
            w.WindowStyle = WindowStyle.None;
            w.AllowsTransparency = true;
            w.Background = Brushes.Transparent;
            w.ResizeMode = ResizeMode.NoResize;

            UIElement content = w.Content as UIElement;
            w.Content = null;

            Border outer = new Border();
            outer.CornerRadius = new CornerRadius(cornerRadius);
            outer.Background = Brush(backgroundHex);
            outer.Margin = new Thickness(16);
            DropShadowEffect shadow = new DropShadowEffect();
            shadow.BlurRadius = 24;
            shadow.ShadowDepth = 0;
            shadow.Opacity = 0.6;
            shadow.Color = Colors.Black;
            outer.Effect = shadow;

            // El borde redondeado lleva el fondo; el contenido no debe pintar fondos cuadrados.
            outer.Child = content;
            w.Content = outer;
        }

        public static TextBlock Text(string text, double size, Brush color, bool bold)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.FontSize = size;
            t.Foreground = color;
            t.FontFamily = new FontFamily("Segoe UI");
            t.FontWeight = bold ? FontWeights.Bold : FontWeights.Normal;
            t.TextWrapping = TextWrapping.Wrap;
            return t;
        }

        public static Button PrimaryButton(string text)
        {
            return MakeButton(text, "#1DB954", "#1ED760", "#000000", FontWeights.Bold, 20);
        }

        public static Button SecondaryButton(string text)
        {
            return MakeButton(text, "#282828", "#3E3E3E", "#FFFFFF", FontWeights.SemiBold, 20);
        }

        // Boton tipo enlace: sin fondo, texto gris que se aclara al pasar el mouse.
        public static Button LinkButton(string text)
        {
            Button b = MakeButton(text, "#00000000", "#00000000", "#B3B3B3", FontWeights.Normal, 4);
            b.MouseEnter += delegate { b.Foreground = Brush("#FFFFFF"); };
            b.MouseLeave += delegate { b.Foreground = Brush("#B3B3B3"); };
            return b;
        }

        private static Button MakeButton(string text, string normal, string hover, string fg, FontWeight weight, double radius)
        {
            Button b = new Button();
            b.Content = text;
            b.Foreground = Brush(fg);
            b.FontFamily = new FontFamily("Segoe UI");
            b.FontSize = 14;
            b.FontWeight = weight;
            b.Padding = new Thickness(24, 10, 24, 10);
            b.Cursor = System.Windows.Input.Cursors.Hand;
            b.FocusVisualStyle = null;
            b.BorderThickness = new Thickness(0);
            b.Background = Brush(normal);

            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius > 10 ? 20 : 6));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);

            ControlTemplate template = new ControlTemplate(typeof(Button));
            template.VisualTree = border;
            b.Template = template;

            Brush normalBrush = Brush(normal);
            Brush hoverBrush = Brush(hover);
            b.MouseEnter += delegate { b.Background = hoverBrush; };
            b.MouseLeave += delegate { b.Background = normalBrush; };
            return b;
        }
    }
}
