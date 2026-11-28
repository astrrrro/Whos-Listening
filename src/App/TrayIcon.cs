using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace WhosListening
{
    // Icono en la bandeja con el menu "Elegir cuenta", "Mostrar al abrir Spotify" (con marca) y "Salir".
    // Solo avisa con eventos; quien lo usa decide que hacer. Se crea y se usa en el hilo de la interfaz.
    public class TrayIcon : IDisposable
    {
        // Limite de NotifyIcon.Text en .NET Framework.
        private const int MaxTooltip = 63;

        private NotifyIcon notify;
        private ContextMenuStrip menu;
        private ToolStripMenuItem toggleItem;
        private Font boldFont;
        private Icon ownedIcon;

        public event Action ShowRequested;
        public event Action<bool> ToggleRequested;
        public event Action ExitRequested;

        public TrayIcon(string spotifyExe, bool toggleOn)
        {
            menu = new ContextMenuStrip();
            menu.ShowImageMargin = true;
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkMenuColors());
            menu.BackColor = ColorTranslator.FromHtml("#181818");
            menu.ForeColor = Color.White;

            ToolStripMenuItem choose = new ToolStripMenuItem(Strings.Get("trayChooseAccount"));
            boldFont = new Font(menu.Font, FontStyle.Bold);
            choose.Font = boldFont;
            choose.ForeColor = Color.White;
            choose.Click += delegate { Raise(ShowRequested); };

            ToolStripMenuItem toggle = new ToolStripMenuItem(Strings.Get("trayToggle"));
            toggle.CheckOnClick = false;
            toggle.Checked = toggleOn;
            toggle.ForeColor = Color.White;
            toggle.Click += delegate
            {
                Action<bool> h = ToggleRequested;
                if (h != null) h(!toggle.Checked);
            };
            toggleItem = toggle;

            ToolStripMenuItem exit = new ToolStripMenuItem(Strings.Get("trayExit"));
            exit.ForeColor = Color.White;
            exit.Click += delegate { Raise(ExitRequested); };

            menu.Items.Add(choose);
            menu.Items.Add(toggle);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exit);

            notify = new NotifyIcon();
            notify.Icon = LoadIcon(spotifyExe);
            string tip = Strings.Get("trayTooltip");
            if (tip.Length > MaxTooltip) tip = tip.Substring(0, MaxTooltip);
            notify.Text = tip;
            notify.ContextMenuStrip = menu;
            notify.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) Raise(ShowRequested);
            };
            notify.MouseDoubleClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) Raise(ShowRequested);
            };
            notify.Visible = true;
        }

        // Refleja el estado del interruptor en la marca del menu (sin disparar eventos).
        public void SetToggle(bool on)
        {
            if (toggleItem != null) toggleItem.Checked = on;
        }

        public void Dispose()
        {
            if (notify != null)
            {
                // Ocultar antes de liberar evita iconos fantasma en la bandeja.
                try { notify.Visible = false; }
                catch (Exception) { }
                notify.Dispose();
                notify = null;
            }
            if (menu != null)
            {
                menu.Dispose();
                menu = null;
            }
            if (boldFont != null)
            {
                boldFont.Dispose();
                boldFont = null;
            }
            if (ownedIcon != null)
            {
                ownedIcon.Dispose();
                ownedIcon = null;
            }
            toggleItem = null;
        }

        private static void Raise(Action h)
        {
            if (h != null) h();
        }

        // Icono propio (recurso incrustado) en el tamano chico del sistema; si falta o falla,
        // el de Spotify.exe, y si tampoco, el icono generico de Windows.
        private Icon LoadIcon(string spotifyExe)
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WhosListening.ico"))
                {
                    if (stream != null)
                    {
                        ownedIcon = new Icon(stream, SystemInformation.SmallIconSize);
                        return ownedIcon;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("bandeja: icono propio", ex);
                if (ownedIcon != null)
                {
                    ownedIcon.Dispose();
                    ownedIcon = null;
                }
            }
            try
            {
                if (!string.IsNullOrEmpty(spotifyExe) && File.Exists(spotifyExe))
                {
                    using (Icon big = Icon.ExtractAssociatedIcon(spotifyExe))
                    {
                        if (big != null)
                        {
                            ownedIcon = new Icon(big, SystemInformation.SmallIconSize);
                            return ownedIcon;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("bandeja: icono de Spotify", ex);
            }
            return SystemIcons.Application;
        }

        // Colores oscuros para el menu. La marca se dibuja sobre fondo verde para que se vea.
        private class DarkMenuColors : ProfessionalColorTable
        {
            private static readonly Color Back = ColorTranslator.FromHtml("#181818");
            private static readonly Color Hover = ColorTranslator.FromHtml("#282828");
            private static readonly Color Line = ColorTranslator.FromHtml("#2A2A2A");
            private static readonly Color Green = ColorTranslator.FromHtml("#1DB954");

            public override Color ToolStripDropDownBackground { get { return Back; } }
            public override Color ImageMarginGradientBegin { get { return Back; } }
            public override Color ImageMarginGradientMiddle { get { return Back; } }
            public override Color ImageMarginGradientEnd { get { return Back; } }
            public override Color MenuBorder { get { return Line; } }
            public override Color MenuItemBorder { get { return Hover; } }
            public override Color MenuItemSelected { get { return Hover; } }
            public override Color MenuItemSelectedGradientBegin { get { return Hover; } }
            public override Color MenuItemSelectedGradientEnd { get { return Hover; } }
            public override Color SeparatorDark { get { return Line; } }
            public override Color SeparatorLight { get { return Line; } }
            public override Color CheckBackground { get { return Green; } }
            public override Color CheckSelectedBackground { get { return Green; } }
            public override Color CheckPressedBackground { get { return Green; } }
        }
    }
}
