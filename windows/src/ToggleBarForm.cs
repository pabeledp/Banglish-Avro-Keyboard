using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Banglish.Core;
#if VOICE_BETA
using Banglish.Voice;
#endif

namespace Banglish.UI
{
    public class ToggleBarForm : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private readonly KeyboardHook _hook;
        private bool _isBangla = true;
        private Image _logoImg;

        private Point _mouseDownScreenPos;
        private Point _formStartPos;
        private bool _hasDragged = false;
        private bool _isHovered = false;
        private bool _isMicHovered = false;

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
#if VOICE_BETA
            this.Size = new Size(130, 32); // Expanded in Beta to fit Mic button
#else
            this.Size = new Size(100, 32); // Standard Stable width
#endif
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.FromArgb(30, 41, 59);

            // Context Menu on Right Click
            _contextMenu = new ContextMenuStrip();
            _contextMenu.RenderMode = ToolStripRenderMode.System;

            var toggleItem = new ToolStripMenuItem("ভাষা পরিবর্তন (F12)", null, (s, e) =>
            {
                if (_hook != null) _hook.ToggleMode();
            });
            toggleItem.Font = new Font(toggleItem.Font, FontStyle.Bold);

            _contextMenu.Items.Add(toggleItem);

#if VOICE_BETA
            var voiceItem = new ToolStripMenuItem("ভয়েস টাইপিং (Ctrl+F12)", null, (s, e) =>
            {
                VoiceTypingManager.Shared.ToggleVoiceTyping();
            });
            _contextMenu.Items.Add(voiceItem);
#endif

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
            this.MouseLeave += (s, e) => { _isHovered = false; _isMicHovered = false; this.Invalidate(); };

            UpdateRegion();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                if (Environment.OSVersion.Version.Build >= 22000)
                {
                    int cornerPref = 2; // DWMWCP_ROUND (Windows 11 Native Rounded Corners)
                    DwmSetWindowAttribute(this.Handle, 33, ref cornerPref, sizeof(int));
                }
            }
            catch {}
        }

        private void UpdateRegion()
        {
            if (Environment.OSVersion.Version.Build < 22000)
            {
                this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width + 1, this.Height + 1, 14, 14));
            }
            else
            {
                this.Region = null;
            }
        }

        public void SetDefaultPosition()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;

            int taskbarHeight = bounds.Bottom - wa.Bottom;
            int x = bounds.Right - this.Width - 250; // Sits nicely to the left of the Windows 10/11 system tray icons
            int y;

            if (taskbarHeight >= 36)
            {
                // Sits centered right on the taskbar
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
                this.BeginInvoke(new Action(() => SetMode(isBangla)));
                return;
            }

            _isBangla = isBangla;
            this.Invalidate();
        }

        private void ToggleBarForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _mouseDownScreenPos = Cursor.Position;
                _formStartPos = this.Location;
                _hasDragged = false;
            }
        }

        private void ToggleBarForm_MouseMove(object sender, MouseEventArgs e)
        {
#if VOICE_BETA
            bool overMic = (e.X >= 74 && e.X <= 98);
            if (overMic != _isMicHovered)
            {
                _isMicHovered = overMic;
                this.Invalidate();
            }
#endif

            if (e.Button == MouseButtons.Left)
            {
                Point cur = Cursor.Position;
                int dx = Math.Abs(cur.X - _mouseDownScreenPos.X);
                int dy = Math.Abs(cur.Y - _mouseDownScreenPos.Y);
                if (dx > 8 || dy > 8)
                {
                    _hasDragged = true;
                    this.Location = new Point(_formStartPos.X + (cur.X - _mouseDownScreenPos.X),
                                              _formStartPos.Y + (cur.Y - _mouseDownScreenPos.Y));
                }
            }
        }

        private void ToggleBarForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!_hasDragged)
                {
#if VOICE_BETA
                    // Check if clicked the Microphone button (X: 74 to 98)
                    if (e.X >= 74 && e.X <= 98)
                    {
                        VoiceTypingManager.Shared.ToggleVoiceTyping();
                        this.Invalidate();
                        _hasDragged = false;
                        return;
                    }
#endif
                    // Toggle language mode
                    if (_hook != null)
                    {
                        _hook.ToggleMode();
                    }
                }
                _hasDragged = false;
            }
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ToggleBarForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            Rectangle borderRect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

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

            // Draw Background Pill with Anti-Aliasing
            using (var path = GetRoundedRectPath(borderRect, 12))
            {
                using (var brush = new LinearGradientBrush(rect, bgTop, bgBottom, 90f))
                {
                    g.FillPath(brush, path);
                }

                // Draw Border
                using (var pen = new Pen(borderColor, 1.2f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Draw Logo
            if (_logoImg != null)
            {
                g.DrawImage(_logoImg, new Rectangle(6, 6, 20, 20));
            }

            // Draw Language Text
            string label = _isBangla ? "বাংলা" : "ENG";
            using (var font = _isBangla
                ? GetBestFont(new[] { "Hind Siliguri", "Nirmala UI", "Segoe UI" }, 10.5f, FontStyle.Bold)
                : GetBestFont(new[] { "Creato Display", "Segoe UI" }, 10f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(label, font, brush, 28, 6);
            }

#if VOICE_BETA
            // Divider Line before Mic
            using (var divPen = new Pen(Color.FromArgb(90, 255, 255, 255), 1f))
            {
                g.DrawLine(divPen, 72, 7, 72, 25);
            }

            // Draw Microphone Button (X: 74 to 98)
            bool isRecording = VoiceTypingManager.Shared.IsRecording;
            Rectangle micRect = new Rectangle(74, 5, 24, 22);

            if (_isMicHovered || isRecording)
            {
                Color micBg = isRecording ? Color.FromArgb(220, 239, 68, 68) : Color.FromArgb(60, 255, 255, 255);
                using (var micPill = GetRoundedRectPath(micRect, 6))
                using (var brush = new SolidBrush(micBg))
                {
                    g.FillPath(brush, micPill);
                }
            }

            DrawVectorMic(g, micRect, isRecording);

            // Draw F12 Pill Tag
            using (var f12Brush = new SolidBrush(Color.FromArgb(190, 255, 255, 255)))
            using (var f12Font = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            {
                g.DrawString("F12", f12Font, f12Brush, 102, 9);
            }
#else
            // Draw F12 Pill Tag in Stable Mode
            using (var f12Brush = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
            using (var f12Font = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            {
                g.DrawString("F12", f12Font, f12Brush, 72, 9);
            }
#endif
        }

#if VOICE_BETA
        private void DrawVectorMic(Graphics g, Rectangle rect, bool isRecording)
        {
            Color micColor = isRecording ? Color.White : Color.FromArgb(240, 255, 255, 255);
            int cx = rect.X + rect.Width / 2;
            int cy = rect.Y + 3;

            // Mic capsule
            using (var brush = new SolidBrush(micColor))
            {
                g.FillRectangle(brush, cx - 2, cy + 2, 5, 7);
                g.FillEllipse(brush, cx - 2, cy, 5, 5);
                g.FillEllipse(brush, cx - 2, cy + 6, 5, 5);
            }

            // Mic cradle
            using (var pen = new Pen(micColor, 1.2f))
            {
                g.DrawArc(pen, cx - 5, cy + 3, 10, 9, 0, 180);
                g.DrawLine(pen, cx, cy + 12, cx, cy + 15);
                g.DrawLine(pen, cx - 3, cy + 15, cx + 3, cy + 15);
            }
        }
#endif

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
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            const int MA_NOACTIVATE = 3;
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref m);
        }
        #endregion
    }
}
