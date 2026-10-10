using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Banglish.Core;

namespace Banglish.UI
{
    public class ToggleBarForm : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        private readonly KeyboardHook _hook;
        private bool _isBangla = true;
        private Image _logoImg;

        private bool _isDragging = false;
        private Point _dragStartPoint;
        private bool _isHovered = false;

        private ContextMenuStrip _contextMenu;

        public static Font GetBestFont(string[] fontNames, float size, FontStyle style)
        {
            foreach (var name in fontNames)
            {
                using (var test = new Font(name, size, style))
                {
                    if (test.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return new Font(name, size, style);
                    }
                }
            }
            return new Font(FontFamily.GenericSansSerif, size, style);
        }

        public ToggleBarForm(KeyboardHook hook)
        {
            _hook = hook;
            _isBangla = hook != null ? hook.IsEnabled : true;

            LoadLogo();
            InitializeComponent();
            SetDefaultPosition();
        }

        private void LoadLogo()
        {
            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Banglish-Logo.png");
                if (File.Exists(logoPath))
                {
                    byte[] bytes = File.ReadAllBytes(logoPath);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        _logoImg = new Bitmap(ms);
                    }
                }
                else
                {
                    var asm = System.Reflection.Assembly.GetExecutingAssembly();
                    using (Stream s = asm.GetManifestResourceStream("Banglish-Logo.png"))
                    {
                        if (s != null) _logoImg = new Bitmap(s);
                    }
                }
            }
            catch {}
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.Size = new Size(100, 32);
            this.Cursor = Cursors.Hand;

            // Context Menu on Right Click
            _contextMenu = new ContextMenuStrip();
            _contextMenu.RenderMode = ToolStripRenderMode.System;

            var toggleItem = new ToolStripMenuItem("ভাষা পরিবর্তন (F12)", null, (s, e) =>
            {
                if (_hook != null) _hook.ToggleMode();
            });
            toggleItem.Font = new Font(toggleItem.Font, FontStyle.Bold);

            var resetPosItem = new ToolStripMenuItem("পজিশন রিসেট করুন", null, (s, e) =>
            {
                SetDefaultPosition();
            });

            var hideItem = new ToolStripMenuItem("টগল বার লুকান", null, (s, e) =>
            {
                this.Hide();
            });

            var exitItem = new ToolStripMenuItem("বন্ধ করুন (Exit Banglish)", null, (s, e) =>
            {
                Application.Exit();
            });
            exitItem.ForeColor = Color.Red;

            _contextMenu.Items.Add(toggleItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(resetPosItem);
            _contextMenu.Items.Add(hideItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(exitItem);

            this.ContextMenuStrip = _contextMenu;

            this.Paint += ToggleBarForm_Paint;
            this.MouseDown += ToggleBarForm_MouseDown;
            this.MouseMove += ToggleBarForm_MouseMove;
            this.MouseUp += ToggleBarForm_MouseUp;
            this.MouseEnter += (s, e) => { _isHovered = true; this.Invalidate(); };
            this.MouseLeave += (s, e) => { _isHovered = false; this.Invalidate(); };

            UpdateRegion();
        }

        private void UpdateRegion()
        {
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 14, 14));
        }

        public void SetDefaultPosition()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;

            int taskbarHeight = bounds.Bottom - wa.Bottom;
            int x = bounds.Right - this.Width - 250; // Just to the left of the Windows 10/11 system tray icons
            int y;

            if (taskbarHeight >= 36)
            {
                // Sits centered right on the taskbar!
                y = wa.Bottom + (taskbarHeight - this.Height) / 2;
            }
            else
            {
                // Floating just above the taskbar in the bottom-right corner
                y = wa.Bottom - this.Height - 6;
                x = wa.Right - this.Width - 16;
            }

            if (x < 0) x = wa.Right - this.Width - 16;
            this.Location = new Point(x, y);
        }

        public void SetMode(bool isBangla)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetMode(isBangla)));
                return;
            }

            _isBangla = isBangla;
            this.Invalidate();
        }

        private void ToggleBarForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = false;
                _dragStartPoint = e.Location;
            }
        }

        private void ToggleBarForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int dx = Math.Abs(e.X - _dragStartPoint.X);
                int dy = Math.Abs(e.Y - _dragStartPoint.Y);
                if (dx > 4 || dy > 4)
                {
                    _isDragging = true;
                    this.Left += (e.X - _dragStartPoint.X);
                    this.Top += (e.Y - _dragStartPoint.Y);
                }
            }
        }

        private void ToggleBarForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!_isDragging)
                {
                    // Click without drag -> Toggle Language!
                    if (_hook != null)
                    {
                        _hook.ToggleMode();
                    }
                }
                _isDragging = false;
            }
        }

        private void ToggleBarForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);

            Color bgTop, bgBottom, borderColor;

            if (_isBangla)
            {
                // Vibrant Emerald Green
                bgTop = _isHovered ? Color.FromArgb(16, 185, 129) : Color.FromArgb(5, 150, 105);
                bgBottom = _isHovered ? Color.FromArgb(5, 150, 105) : Color.FromArgb(4, 120, 87);
                borderColor = Color.FromArgb(110, 231, 183);
            }
            else
            {
                // Sleek Neutral Dark Slate
                bgTop = _isHovered ? Color.FromArgb(71, 85, 105) : Color.FromArgb(51, 65, 85);
                bgBottom = _isHovered ? Color.FromArgb(51, 65, 85) : Color.FromArgb(30, 41, 59);
                borderColor = Color.FromArgb(148, 163, 184);
            }

            // Draw Background Gradient
            using (var brush = new LinearGradientBrush(rect, bgTop, bgBottom, 90f))
            {
                g.FillRectangle(brush, rect);
            }

            // Draw Border
            using (var pen = new Pen(borderColor, 1f))
            {
                g.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }

            // Draw Logo
            if (_logoImg != null)
            {
                g.DrawImage(_logoImg, new Rectangle(7, 6, 20, 20));
            }

            // Draw Language Text
            string label = _isBangla ? "বাংলা" : "ENG";
            using (var font = _isBangla
                ? GetBestFont(new[] { "Hind Siliguri", "Nirmala UI", "Segoe UI" }, 10.5f, FontStyle.Bold)
                : GetBestFont(new[] { "Creato Display", "Segoe UI" }, 10f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(label, font, brush, 30, 6);
            }

            // Draw F12 Pill Tag
            using (var f12Brush = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
            using (var f12Font = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            {
                g.DrawString("F12", f12Font, f12Brush, 72, 9);
            }
        }

        #region Win32 Non-Activating Window Properties
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE (Crucial: never steals focus from active typing window)
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW (don't show in taskbar or Alt+Tab)
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }
        #endregion
    }
}
