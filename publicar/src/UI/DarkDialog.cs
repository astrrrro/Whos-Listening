using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WhosListening
{
    public static class DarkDialog
    {
        // Crea la ventana base: sin borde, modal, siempre encima, arrastrable desde el cuerpo.
        private static Window Create(Window owner, double width, UIElement body)
        {
            Window w = new Window();
            w.Width = width;
            w.SizeToContent = SizeToContent.Height;
            w.Topmost = true;
            w.ShowInTaskbar = false;
            if (owner != null)
            {
                w.Owner = owner;
                w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            Border pad = new Border();
            pad.Padding = new Thickness(24);
            pad.Child = body;
            w.Content = pad;
            Theme.MakeBorderless(w, 12, "#181818");
            w.MouseLeftButtonDown += delegate
            {
                try { w.DragMove(); }
                catch (Exception) { }
            };
            return w;
        }

        private static StackPanel ButtonRow(params Button[] buttons)
        {
            StackPanel row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            row.HorizontalAlignment = HorizontalAlignment.Right;
            row.Margin = new Thickness(0, 20, 0, 0);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (i > 0) buttons[i].Margin = new Thickness(8, 0, 0, 0);
                row.Children.Add(buttons[i]);
            }
            return row;
        }

        public static void Message(Window owner, string text)
        {
            StackPanel body = new StackPanel();
            body.Children.Add(Theme.Text(text, 14, Theme.TextPrimary, false));
            Button ok = Theme.PrimaryButton(Strings.Get("ok"));
            body.Children.Add(ButtonRow(ok));
            Window w = Create(owner, 420, body);
            ok.IsDefault = true;
            ok.Click += delegate { w.DialogResult = true; };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { w.DialogResult = false; e.Handled = true; }
            };
            w.ShowDialog();
        }

        public static bool Confirm(Window owner, string text, string yesKey, string noKey)
        {
            StackPanel body = new StackPanel();
            body.Children.Add(Theme.Text(text, 14, Theme.TextPrimary, false));
            Button yes = Theme.PrimaryButton(Strings.Get(yesKey));
            Button no = Theme.SecondaryButton(Strings.Get(noKey));
            body.Children.Add(ButtonRow(no, yes));
            Window w = Create(owner, 420, body);
            bool result = false;
            yes.Click += delegate { result = true; w.Close(); };
            no.Click += delegate { result = false; w.Close(); };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { result = false; w.Close(); e.Handled = true; }
                else if (e.Key == Key.Enter) { result = true; w.Close(); e.Handled = true; }
            };
            w.ShowDialog();
            return result;
        }

        public static string AskText(Window owner, string prompt, string initial)
        {
            StackPanel body = new StackPanel();
            body.Children.Add(Theme.Text(prompt, 14, Theme.TextPrimary, true));

            TextBox box = new TextBox();
            box.Text = initial != null ? initial : "";
            box.Margin = new Thickness(0, 12, 0, 0);
            box.Background = Theme.Brush("#282828");
            box.Foreground = Theme.TextPrimary;
            box.CaretBrush = Theme.TextPrimary;
            box.SelectionBrush = Theme.Green;
            box.FontFamily = new FontFamily("Segoe UI");
            box.FontSize = 14;
            box.BorderThickness = new Thickness(0);
            box.Padding = new Thickness(10, 8, 10, 8);
            box.FocusVisualStyle = null;
            body.Children.Add(box);

            Button ok = Theme.PrimaryButton(Strings.Get("ok"));
            Button cancel = Theme.SecondaryButton(Strings.Get("cancel"));
            body.Children.Add(ButtonRow(cancel, ok));
            Window w = Create(owner, 420, body);

            string result = null;
            Action accept = delegate
            {
                string t = box.Text.Trim();
                if (t.Length == 0) return;
                result = t;
                w.Close();
            };
            ok.Click += delegate { accept(); };
            cancel.Click += delegate { result = null; w.Close(); };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { result = null; w.Close(); e.Handled = true; }
                else if (e.Key == Key.Enter) { accept(); e.Handled = true; }
            };
            w.Loaded += delegate
            {
                box.Focus();
                box.SelectAll();
            };
            w.ShowDialog();
            return result;
        }

        public static int Choose(Window owner, string prompt, string[] optionKeys)
        {
            StackPanel body = new StackPanel();
            body.Children.Add(Theme.Text(prompt, 14, Theme.TextPrimary, true));
            Window w = null;
            int result = -1;
            StackPanel list = new StackPanel();
            list.Margin = new Thickness(0, 16, 0, 0);
            for (int i = 0; i < optionKeys.Length; i++)
            {
                int index = i;
                string label = Strings.Get(optionKeys[i]);
                Button b = i == 0 ? Theme.PrimaryButton(label) : Theme.SecondaryButton(label);
                b.HorizontalAlignment = HorizontalAlignment.Stretch;
                if (i > 0) b.Margin = new Thickness(0, 8, 0, 0);
                b.Click += delegate { result = index; w.Close(); };
                list.Children.Add(b);
            }
            body.Children.Add(list);
            Button cancel = Theme.LinkButton(Strings.Get("cancel"));
            cancel.HorizontalAlignment = HorizontalAlignment.Center;
            cancel.Margin = new Thickness(0, 12, 0, 0);
            body.Children.Add(cancel);

            w = Create(owner, 420, body);
            cancel.Click += delegate { result = -1; w.Close(); };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { result = -1; w.Close(); e.Handled = true; }
                else if (e.Key == Key.Enter && optionKeys.Length > 0) { result = 0; w.Close(); e.Handled = true; }
            };
            w.ShowDialog();
            return result;
        }

        public static void ShowText(Window owner, string title, string monoText)
        {
            Grid body = new Grid();
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock head = Theme.Text(title, 16, Theme.TextPrimary, true);
            Grid.SetRow(head, 0);
            body.Children.Add(head);

            TextBox box = new TextBox();
            box.Text = monoText != null ? monoText : "";
            box.IsReadOnly = true;
            box.FontFamily = new FontFamily("Consolas");
            box.FontSize = 11;
            box.Background = Theme.Brush("#282828");
            box.Foreground = Theme.TextPrimary;
            box.CaretBrush = Theme.TextPrimary;
            box.SelectionBrush = Theme.Green;
            box.BorderThickness = new Thickness(0);
            box.Padding = new Thickness(10);
            box.Margin = new Thickness(0, 12, 0, 0);
            box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            box.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            box.TextWrapping = TextWrapping.NoWrap;
            Grid.SetRow(box, 1);
            body.Children.Add(box);

            Button copy = Theme.PrimaryButton(Strings.Get("copy"));
            Button close = Theme.SecondaryButton(Strings.Get("close"));
            StackPanel row = ButtonRow(close, copy);
            Grid.SetRow(row, 2);
            body.Children.Add(row);

            Window w = Create(owner, 640, body);
            w.SizeToContent = SizeToContent.Manual;
            w.Height = 480;
            copy.Click += delegate
            {
                try { Clipboard.SetText(box.Text); }
                catch (Exception) { }
            };
            close.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { w.Close(); e.Handled = true; }
            };
            w.ShowDialog();
        }
    }
}
