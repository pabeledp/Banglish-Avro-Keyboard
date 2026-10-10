using System;
using System.Diagnostics;
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
    public class DarkMenuColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(15, 23, 42); } }
        public override Color ImageMarginGradientBegin { get { return Color.FromArgb(15, 23, 42); } }
        public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(15, 23, 42); } }
        public override Color ImageMarginGradientEnd { get { return Color.FromArgb(15, 23, 42); } }
        public override Color MenuBorder { get { return Color.FromArgb(16, 185, 129); } }
        public override Color MenuItemBorder { get { return Color.FromArgb(16, 185, 129); } }
        public override Color MenuItemSelected { get { return Color.FromArgb(5, 150, 105); } }
        public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(5, 150, 105); } }
        public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(4, 120, 87); } }
        public override Color MenuItemPressedGradientBegin { get { return Color.FromArgb(4, 120, 87); } }
        public override Color MenuItemPressedGradientEnd { get { return Color.FromArgb(6, 78, 59); } }
        public override Color SeparatorDark { get { return Color.FromArgb(51, 65, 85); } }
        public override Color SeparatorLight { get { return Color.FromArgb(30, 41, 59); } }
    }

    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColorTable())
        {
            this.RoundedEdges = true;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.White;
            base.OnRenderItemText(e);
        }
    }

    public class ToggleMenuForm : Form
    {
        #region Win32 Imports
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_NCRBUTTONDOWN = 0x00A4;
        #endregion

        private readonly KeyboardHook _hook;
        private readonly ToggleBarForm _toggleBar;
        private Image _logoImg;

        private LowLevelMouseProc _mouseProc;
        private IntPtr _mouseHook = IntPtr.Zero;

        private int _hoveredIndex = -1;
        private bool _closeHovered = false;

        private readonly Font fontTitle;
        private readonly Font fontItem;
        private readonly Font fontBadge;
        private readonly Font fontShortcut;

        private class MenuItemDef
        {
            public string Icon;
            public string Title;
            public string Shortcut;
            public bool IsExit;
            public Action Action;
            public Rectangle Bounds;
        }

        private System.Collections.Generic.List<MenuItemDef> _items = new System.Collections.Generic.List<MenuItemDef>();

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        public ToggleMenuForm(KeyboardHook hook, ToggleBarForm toggleBar)
        {
            _hook = hook;
            _toggleBar = toggleBar;

            fontTitle = CandidateForm_GetBestFont(new[] { "Plus Jakarta Sans", "Segoe UI", "Inter" }, 9.5f, FontStyle.Bold);
            fontItem = CandidateForm_GetBestFont(new[] { "Hind Siliguri", "Nirmala UI", "Segoe UI" }, 10f, FontStyle.Regular);
            fontBadge = CandidateForm_GetBestFont(new[] { "Plus Jakarta Sans", "Segoe UI" }, 7.5f, FontStyle.Bold);
            fontShortcut = CandidateForm_GetBestFont(new[] { "Consolas", "Cascadia Code", "Segoe UI" }, 8f, FontStyle.Bold);

            LoadLogo();
            InitializeComponent();
            BuildMenuItems();
        }

        private static Font CandidateForm_GetBestFont(string[] fontNames, float size, FontStyle style)
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
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(15, 23, 42); // Clean Slate-900
            this.ForeColor = Color.White;
            this.Cursor = Cursors.Default;

#if VOICE_BETA
            this.Size = new Size(224, 252);
#else
            this.Size = new Size(224, 212);
#endif

            this.Paint += ToggleMenuForm_Paint;
            this.MouseMove += ToggleMenuForm_MouseMove;
            this.MouseLeave += (s, e) => { _hoveredIndex = -1; _closeHovered = false; this.Invalidate(); };
            this.MouseClick += ToggleMenuForm_MouseClick;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) HideMenu(); };
            this.Deactivate += (s, e) => { HideMenu(); };
        }

        private void BuildMenuItems()
        {
            _items.Clear();

            int startY = 46;
            int itemHeight = 36;
            int itemWidth = this.Width - 16;
            int x = 8;
            int y = startY;

            // 1. Language Toggle
            _items.Add(new MenuItemDef
            {
                Icon = "🔄",
                Title = "ভাষা পরিবর্তন",
                Shortcut = "F12",
                Bounds = new Rectangle(x, y, itemWidth, itemHeight),
                Action = () => { if (_hook != null) _hook.ToggleMode(); }
            });
            y += itemHeight + 2;

#if VOICE_BETA
            // 2. Voice Typing (Beta only)
            _items.Add(new MenuItemDef
            {
                Icon = "🎙️",
                Title = "ভয়েস টাইপিং",
                Shortcut = "Ctrl+F12",
                Bounds = new Rectangle(x, y, itemWidth, itemHeight),
                Action = () => { VoiceTypingManager.Shared.ToggleVoiceTyping(); }
            });
            y += itemHeight + 2;
#endif

            // 3. Reset Position
            _items.Add(new MenuItemDef
            {
                Icon = "📍",
                Title = "পজিশন রিসেট",
                Shortcut = "",
                Bounds = new Rectangle(x, y, itemWidth, itemHeight),
                Action = () => { if (_toggleBar != null) _toggleBar.SetDefaultPosition(); }
            });
            y += itemHeight + 2;

            // 4. Hide Toggle Bar
            _items.Add(new MenuItemDef
            {
                Icon = "👁️",
                Title = "টগল লুকান",
                Shortcut = "",
                Bounds = new Rectangle(x, y, itemWidth, itemHeight),
                Action = () => { if (_toggleBar != null) _toggleBar.HideToggle(); }
            });
            y += itemHeight + 6;

            // 5. Exit App
            _items.Add(new MenuItemDef
            {
                Icon = "❌",
                Title = "Exit Banglish",
                Shortcut = "",
                IsExit = true,
                Bounds = new Rectangle(x, y, itemWidth, itemHeight),
                Action = () => { Application.Exit(); }
            });
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
                else
                {
                    this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width + 1, this.Height + 1, 14, 14));
                }
            }
            catch {}
        }

        public void ShowMenuAbove(Form anchorForm)
        {
            if (this.Visible)
            {
                HideMenu();
                return;
            }

            BuildMenuItems();

            Rectangle wa = Screen.FromControl(anchorForm).WorkingArea;
            int x = anchorForm.Right - this.Width;
            int y = anchorForm.Top - this.Height - 8;

            if (x < wa.Left + 8) x = wa.Left + 8;
            if (x + this.Width > wa.Right - 8) x = wa.Right - this.Width - 8;
            if (y < wa.Top + 8) y = anchorForm.Bottom + 8;

            this.Location = new Point(x, y);
            this.Show();
            this.BringToFront();
            this.Activate();

            StartGlobalMouseHook();
        }

        public void HideMenu()
        {
            StopGlobalMouseHook();
            if (this.Visible)
            {
                this.Hide();
            }
        }

        private void StartGlobalMouseHook()
        {
            if (_mouseHook == IntPtr.Zero)
            {
                _mouseProc = HookCallback;
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private void StopGlobalMouseHook()
        {
            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_NCLBUTTONDOWN || msg == WM_NCRBUTTONDOWN)
                {
                    Point cur = Cursor.Position;
                    // If user clicks anywhere outside this menu card, instantly dismiss it!
                    if (!this.Bounds.Contains(cur))
                    {
                        try
                        {
                            this.BeginInvoke(new Action(() => HideMenu()));
                        }
                        catch {}
                    }
                }
            }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
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

        private void ToggleMenuForm_MouseMove(object sender, MouseEventArgs e)
        {
            int oldHovered = _hoveredIndex;
            bool oldClose = _closeHovered;

            _hoveredIndex = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Bounds.Contains(e.Location))
                {
                    _hoveredIndex = i;
                    break;
                }
            }

            Rectangle closeRect = new Rectangle(this.Width - 28, 10, 18, 18);
            _closeHovered = closeRect.Contains(e.Location);

            if (_hoveredIndex != oldHovered || _closeHovered != oldClose)
            {
                this.Cursor = (_hoveredIndex >= 0 || _closeHovered) ? Cursors.Hand : Cursors.Default;
                this.Invalidate();
            }
        }

        private void ToggleMenuForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Rectangle closeRect = new Rectangle(this.Width - 28, 10, 18, 18);
                if (closeRect.Contains(e.Location))
                {
                    HideMenu();
                    return;
                }

                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].Bounds.Contains(e.Location))
                    {
                        Action act = _items[i].Action;
                        HideMenu();
                        if (act != null) act();
                        return;
                    }
                }
            }
        }

        private void ToggleMenuForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            Rectangle borderRect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            // 1. Background Gradient Card
            using (var brush = new LinearGradientBrush(rect, Color.FromArgb(15, 23, 42), Color.FromArgb(24, 33, 47), 90f))
            using (var path = GetRoundedRectPath(borderRect, 14))
            {
                g.FillPath(brush, path);
                using (var pen = new Pen(Color.FromArgb(16, 185, 129), 1.2f)) // Sleek Emerald Border
                {
                    g.DrawPath(pen, path);
                }
            }

            // 2. Header
            if (_logoImg != null)
            {
                g.DrawImage(_logoImg, new Rectangle(12, 11, 18, 18));
            }
            g.DrawString("Banglish কীবোর্ড", fontTitle, Brushes.White, 34, 11);

            // Version Tag Pill
#if VOICE_BETA
            string verText = "Beta";
            Color verBg = Color.FromArgb(16, 185, 129);
#else
            string verText = "v1.0.0";
            Color verBg = Color.FromArgb(51, 65, 85);
#endif
            using (var verBrush = new SolidBrush(verBg))
            using (var verPath = GetRoundedRectPath(new Rectangle(145, 12, 38, 16), 4))
            {
                g.FillPath(verBrush, verPath);
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(verText, fontBadge, Brushes.White, new Rectangle(145, 12, 38, 16), sf);
                }
            }

            // Close '✕' Button
            using (var closeBrush = new SolidBrush(_closeHovered ? Color.FromArgb(248, 113, 113) : Color.FromArgb(148, 163, 184)))
            {
                g.DrawString("✕", fontTitle, closeBrush, this.Width - 26, 10);
            }

            // Header Separator
            using (var sepPen = new Pen(Color.FromArgb(30, 41, 59), 1f))
            {
                g.DrawLine(sepPen, 10, 38, this.Width - 10, 38);
            }

            // 3. Menu Items
            bool isBangla = _hook != null ? _hook.IsEnabled : true;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                bool isHovered = (i == _hoveredIndex);

                // Separator before Exit
                if (item.IsExit)
                {
                    using (var sepPen = new Pen(Color.FromArgb(30, 41, 59), 1f))
                    {
                        g.DrawLine(sepPen, 10, item.Bounds.Top - 4, this.Width - 10, item.Bounds.Top - 4);
                    }
                }

                // Hover Pill
                if (isHovered)
                {
                    Color hoverBg = item.IsExit ? Color.FromArgb(69, 26, 26) : Color.FromArgb(30, 41, 59);
                    Color hoverBorder = item.IsExit ? Color.FromArgb(239, 68, 68) : Color.FromArgb(16, 185, 129);

                    using (var hPath = GetRoundedRectPath(item.Bounds, 8))
                    using (var hBrush = new SolidBrush(hoverBg))
                    using (var hPen = new Pen(hoverBorder, 1f))
                    {
                        g.FillPath(hBrush, hPath);
                        g.DrawPath(hPen, hPath);
                    }
                }

                // Draw Icon
                using (var iconBrush = new SolidBrush(item.IsExit ? Color.FromArgb(248, 113, 113) : Color.White))
                {
                    g.DrawString(item.Icon, fontItem, iconBrush, item.Bounds.X + 8, item.Bounds.Y + 7);
                }

                // Draw Title
                using (var textBrush = new SolidBrush(item.IsExit ? Color.FromArgb(248, 113, 113) : Color.White))
                {
                    g.DrawString(item.Title, fontItem, textBrush, item.Bounds.X + 34, item.Bounds.Y + 7);
                }

                // Dynamic Status Badge for Language Toggle
                if (i == 0)
                {
                    string statusText = isBangla ? "বাংলা" : "ENG";
                    Color statusColor = isBangla ? Color.FromArgb(16, 185, 129) : Color.FromArgb(100, 116, 139);
                    int badgeW = 34;
                    int badgeX = item.Bounds.Right - badgeW - 40;
                    Rectangle stRect = new Rectangle(badgeX, item.Bounds.Y + 9, badgeW, 18);

                    using (var stPath = GetRoundedRectPath(stRect, 4))
                    using (var stBrush = new SolidBrush(statusColor))
                    {
                        g.FillPath(stBrush, stPath);
                        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString(statusText, fontBadge, Brushes.White, stRect, sf);
                        }
                    }
                }

                // Shortcut Keycap Pill
                if (!string.IsNullOrEmpty(item.Shortcut))
                {
                    int scW = item.Shortcut.Length > 5 ? 54 : 32;
                    int scX = item.Bounds.Right - scW - 6;
                    Rectangle scRect = new Rectangle(scX, item.Bounds.Y + 9, scW, 18);

                    using (var scPath = GetRoundedRectPath(scRect, 4))
                    using (var scBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    using (var scPen = new Pen(Color.FromArgb(51, 65, 85), 1f))
                    {
                        g.FillPath(scBrush, scPath);
                        g.DrawPath(scPen, scPath);
                        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString(item.Shortcut, fontShortcut, Brushes.LightGray, scRect, sf);
                        }
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopGlobalMouseHook();
                if (fontTitle != null) fontTitle.Dispose();
                if (fontItem != null) fontItem.Dispose();
                if (fontBadge != null) fontBadge.Dispose();
                if (fontShortcut != null) fontShortcut.Dispose();
                if (_logoImg != null) _logoImg.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
