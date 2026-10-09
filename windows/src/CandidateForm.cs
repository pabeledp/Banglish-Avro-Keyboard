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
            fontEnglish = GetBestFont(new[] { "Creato Display", "Plus Jakarta Sans", "Segoe UI", "Inter" }, 9.5f, FontStyle.Regular);
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
            this.BackColor = Color.FromArgb(16, 24, 28);
            this.ForeColor = Color.White;
            this.Size = new Size(180, 200);

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
                int backdropType = 3; // Acrylic
                DwmSetWindowAttribute(this.Handle, 38, ref backdropType, sizeof(int));
            }
            catch {}

            // 2. Windows 10 & 11 Acrylic Blur Behind (Clean translucent neutral dark glass)
            try
            {
                var accent = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    AccentFlags = 2,
                    // Clean crystal translucent glass tint (alpha=130, dark neutral slate)
                    GradientColor = (130 << 24) | (28 << 16) | (24 << 8) | 16
                };

                int size = Marshal.SizeOf(accent);
                IntPtr pData = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(accent, pData, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = 19,
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

            int headerH = 26;
            int rowH = 30;
            int totalH = headerH + (candidates.Count * rowH) + 8;
            int totalW = 185;

            using (var g = this.CreateGraphics())
            {
                foreach (var c in candidates)
                {
                    var sz = g.MeasureString(c, fontBangla);
                    if (sz.Width + 60 > totalW)
                    {
                        totalW = (int)sz.Width + 60;
                    }
                }
            }

            this.Size = new Size(totalW, totalH);
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 16, 16));

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
            // Generous 40px spacing below typing caret so it never overlaps
            int y = caretPos.Y + 40;

            if (x + this.Width > screen.Right)
            {
                x = screen.Right - this.Width - 14;
            }
            if (y + this.Height > screen.Bottom)
            {
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

            // 1. Clean Translucent Acrylic Glass Surface (No harsh tints, crystal clean blur)
            using (var glassBrush = new SolidBrush(Color.FromArgb(140, 16, 24, 22)))
            {
                g.FillRectangle(glassBrush, rect);
            }

            // 2. Subtle 1px Glass Rim Border (Clean, rounded, smooth)
            using (var borderPen = new Pen(Color.FromArgb(45, 255, 255, 255), 1f))
            {
                var borderPath = GetRoundedRectPath(new Rectangle(0, 0, rect.Width - 1, rect.Height - 1), 16);
                g.DrawPath(borderPen, borderPath);
            }

            // 3. Header Text: "Banglish (বাংলা) • F12" (Clean minimal header, NO divider line)
            using (var headerBrush = new SolidBrush(Color.FromArgb(160, 255, 255, 255)))
            {
                g.DrawString("Banglish (বাংলা) • F12", fontEnglish, headerBrush, 14, 7);
            }

            // 4. Candidate Rows
            int yOffset = 28;
            int rowH = 30;

            for (int i = 0; i < candidates.Count; i++)
            {
                bool isSelected = (i == selectedIndex);
                bool isLastRaw = (i == candidates.Count - 1 && candidates[i] == currentRaw);
                Rectangle rowRect = new Rectangle(5, yOffset, this.Width - 10, rowH);

                if (isSelected)
                {
                    // Selected Item: Rich Dark Green Capsule (Clean, no lines, no specular cut across text)
                    using (var pillPath = GetRoundedRectPath(rowRect, 8))
                    using (var pillBrush = new SolidBrush(Color.FromArgb(220, 6, 78, 59))) // Rich Dark Green (#064E3B)
                    {
                        g.FillPath(pillBrush, pillPath);
                    }
                }

                // Number Badge
                string numStr = (i + 1).ToString();
                Color numColor = isSelected ? Color.FromArgb(167, 243, 208) : Color.FromArgb(130, 255, 255, 255);
                using (var numBrush = new SolidBrush(numColor))
                {
                    g.DrawString(numStr + ".", fontNumber, numBrush, 12, yOffset + 6);
                }

                // Candidate Word Text
                string word = candidates[i];
                Color wordColor;
                Font wordFont;

                if (isLastRaw)
                {
                    wordColor = Color.FromArgb(134, 239, 172); // Soft mint for raw Latin
                    wordFont = fontEnglish;
                }
                else
                {
                    wordColor = isSelected ? Color.White : Color.FromArgb(226, 232, 240); // Clean Slate-200
                    wordFont = isSelected ? new Font(fontBangla, FontStyle.Bold) : fontBangla;
                }

                using (var textBrush = new SolidBrush(wordColor))
                {
                    g.DrawString(word, wordFont, textBrush, 32, yOffset + 3);
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
            int rowIdx = (e.Y - 28) / 30;
            if (rowIdx >= 0 && rowIdx < candidates.Count && rowIdx != selectedIndex)
            {
                selectedIndex = rowIdx;
                this.Invalidate();
            }
        }

        private void CandidateForm_MouseDown(object sender, MouseEventArgs e)
        {
            int rowIdx = (e.Y - 28) / 30;
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
