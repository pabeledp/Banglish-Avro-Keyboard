using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Banglish.Core;

namespace Banglish.UI
{
    public class CandidateForm : Form
    {
        #region Win32 Blur & Glass Imports
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
        #endregion

        private readonly Font fontBangla;
        private readonly Font fontEnglish;
        private readonly Font fontNumber;

        private List<string> candidates = new List<string>();
        private int selectedIndex = 0;
        private string currentRaw = "";

        public Action<string> OnSelectCandidate;

        public CandidateForm()
        {
            fontBangla = GetBestFont(new[] { "Hind Siliguri", "Nirmala UI", "Vrinda", "Kalpurush" }, 14f, FontStyle.Regular);
            fontEnglish = GetBestFont(new[] { "Creato Display", "Plus Jakarta Sans", "Segoe UI", "Inter" }, 10f, FontStyle.Regular);
            fontNumber = GetBestFont(new[] { "Creato Display", "Plus Jakarta Sans", "Segoe UI" }, 9f, FontStyle.Bold);

            InitializeComponent();
        }

        private static Font GetBestFont(string[] fontNames, float size, FontStyle style)
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

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(7, 28, 19); // Translucent Dark Emerald
            this.ForeColor = Color.White;
            this.Size = new Size(190, 220);

            this.Paint += CandidateForm_Paint;
            this.MouseDown += CandidateForm_MouseDown;
            this.MouseMove += CandidateForm_MouseMove;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            EnableGlassBlur();
        }

        private void EnableGlassBlur()
        {
            // 1. Windows 11 Acrylic Backdrop
            try
            {
                int backdropType = 3; // DWMSBT_TRANSIENTWINDOW (Acrylic Blur)
                DwmSetWindowAttribute(this.Handle, 38, ref backdropType, sizeof(int));
            }
            catch {}

            // 2. Windows 10 & 11 Acrylic Blur Behind
            try
            {
                var accent = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    AccentFlags = 2,
                    GradientColor = (175 << 24) | (19 << 16) | (28 << 8) | 7 // ABGR Tint
                };

                int size = Marshal.SizeOf(accent);
                IntPtr pData = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(accent, pData, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = 19, // WCA_ACCENT_POLICY
                    Data = pData,
                    SizeOfData = size
                };

                SetWindowCompositionAttribute(this.Handle, ref data);
                Marshal.FreeHGlobal(pData);
            }
            catch {}
        }

        public void UpdateCandidates(string raw, List<string> newCandidates, int selIdx = 0)
        {
            this.currentRaw = raw;
            this.candidates = newCandidates ?? new List<string>();
            this.selectedIndex = Math.Max(0, Math.Min(selIdx, candidates.Count - 1));

            if (string.IsNullOrEmpty(raw) || candidates.Count == 0)
            {
                this.Hide();
                return;
            }

            int headerH = 28;
            int rowH = 32;
            int totalH = headerH + (candidates.Count * rowH) + 10;
            int totalW = 195;

            using (var g = this.CreateGraphics())
            {
                foreach (var c in candidates)
                {
                    var sz = g.MeasureString(c, fontBangla);
                    if (sz.Width + 65 > totalW)
                    {
                        totalW = (int)sz.Width + 65;
                    }
                }
            }

            this.Size = new Size(totalW, totalH);
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 20, 20));

            PositionNearCaret();
            this.Invalidate();

            if (!this.Visible)
            {
                this.Show();
            }
        }

        public void PositionNearCaret()
        {
            Point caretPos = Win32Caret.GetCaretScreenPosition();
            Rectangle screen = Screen.FromPoint(caretPos).WorkingArea;

            int x = caretPos.X;
            // 40px clean distance below typing line to prevent any overlapping
            int y = caretPos.Y + 40;

            if (x + this.Width > screen.Right)
            {
                x = screen.Right - this.Width - 14;
            }
            if (y + this.Height > screen.Bottom)
            {
                // Position safely above caret if near screen bottom
                y = caretPos.Y - this.Height - 16;
            }

            if (x < screen.Left + 8) x = screen.Left + 8;
            if (y < screen.Top + 8) y = screen.Top + 8;

            this.Location = new Point(x, y);
        }

        private void CandidateForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = this.ClientRectangle;

            // 1. Frosted Liquid Glass Base Tint (Translucent Emerald)
            using (var baseBrush = new LinearGradientBrush(rect,
                Color.FromArgb(170, 9, 36, 25),
                Color.FromArgb(185, 4, 22, 15),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(baseBrush, rect);
            }

            // 2. Glossy Specular Light Flare (Apple Liquid Gloss Effect on upper 45%)
            int glossH = (int)(rect.Height * 0.45);
            Rectangle glossRect = new Rectangle(1, 1, rect.Width - 2, glossH);
            using (var glossPath = GetRoundedRectPath(glossRect, 18))
            using (var glossBrush = new LinearGradientBrush(glossRect,
                Color.FromArgb(80, 255, 255, 255), // Luminous gloss sheen
                Color.FromArgb(0, 255, 255, 255),  // Fades down smoothly
                LinearGradientMode.Vertical))
            {
                g.FillPath(glossBrush, glossPath);
            }

            // 3. Top Specular Rim Light (Crisp glass edge reflection)
            using (var rimPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.2f))
            {
                g.DrawLine(rimPen, 18, 1, rect.Width - 18, 1);
            }

            // 4. Outer Glass Glow Border
            using (var outerBorderPen = new Pen(Color.FromArgb(140, 52, 211, 153), 1.5f))
            {
                var fullPath = GetRoundedRectPath(new Rectangle(0, 0, rect.Width - 1, rect.Height - 1), 18);
                g.DrawPath(outerBorderPen, fullPath);
            }

            // 5. Header Title & Divider: "Banglish (বাংলা) • F12"
            using (var headerBrush = new SolidBrush(Color.FromArgb(209, 250, 229))) // Soft Mint Gloss
            {
                g.DrawString("Banglish (বাংলা) • F12", fontEnglish, headerBrush, 14, 8);
            }

            using (var linePen = new Pen(Color.FromArgb(50, 255, 255, 255)))
            {
                g.DrawLine(linePen, 10, 27, rect.Width - 10, 27);
            }

            // 6. Candidate Rows
            int yOffset = 31;
            int rowH = 32;

            for (int i = 0; i < candidates.Count; i++)
            {
                bool isSelected = (i == selectedIndex);
                bool isLastRaw = (i == candidates.Count - 1 && candidates[i] == currentRaw);
                Rectangle rowRect = new Rectangle(7, yOffset, this.Width - 14, rowH);

                if (isSelected)
                {
                    // Glossy Emerald Pill with Specular Reflection
                    using (var pillPath = GetRoundedRectPath(rowRect, 10))
                    using (var pillBrush = new LinearGradientBrush(rowRect,
                        Color.FromArgb(235, 16, 185, 129), // Bright Emerald Top
                        Color.FromArgb(240, 4, 120, 87),   // Rich Emerald Bottom
                        LinearGradientMode.Vertical))
                    using (var pillBorderPen = new Pen(Color.FromArgb(210, 167, 243, 208), 1f))
                    {
                        g.FillPath(pillBrush, pillPath);
                        g.DrawPath(pillBorderPen, pillPath);

                        // Pill specular upper shine
                        Rectangle pillShineRect = new Rectangle(rowRect.X + 1, rowRect.Y + 1, rowRect.Width - 2, rowRect.Height / 2);
                        using (var shineBrush = new LinearGradientBrush(pillShineRect,
                            Color.FromArgb(90, 255, 255, 255),
                            Color.FromArgb(0, 255, 255, 255),
                            LinearGradientMode.Vertical))
                        {
                            g.FillRectangle(shineBrush, pillShineRect);
                        }
                    }
                }

                // Number Badge
                string numStr = (i + 1).ToString();
                Color numColor = isSelected ? Color.White : Color.FromArgb(167, 243, 208);
                using (var numBrush = new SolidBrush(numColor))
                {
                    g.DrawString(numStr + ".", fontNumber, numBrush, 14, yOffset + 7);
                }

                // Text Content
                string word = candidates[i];
                Color wordColor;
                Font wordFont;

                if (isLastRaw)
                {
                    wordColor = Color.FromArgb(134, 239, 172); // Mint green
                    wordFont = fontEnglish;
                }
                else
                {
                    wordColor = Color.White;
                    wordFont = isSelected ? new Font(fontBangla, FontStyle.Bold) : fontBangla;
                }

                using (var textBrush = new SolidBrush(wordColor))
                {
                    g.DrawString(word, wordFont, textBrush, 36, yOffset + 4);
                }

                yOffset += rowH;
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

        private void CandidateForm_MouseMove(object sender, MouseEventArgs e)
        {
            int rowIdx = (e.Y - 31) / 32;
            if (rowIdx >= 0 && rowIdx < candidates.Count && rowIdx != selectedIndex)
            {
                selectedIndex = rowIdx;
                this.Invalidate();
            }
        }

        private void CandidateForm_MouseDown(object sender, MouseEventArgs e)
        {
            int rowIdx = (e.Y - 31) / 32;
            if (rowIdx >= 0 && rowIdx < candidates.Count)
            {
                selectedIndex = rowIdx;
                string chosen = candidates[selectedIndex];
                if (OnSelectCandidate != null)
                {
                    OnSelectCandidate(chosen);
                }
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }
    }
}
