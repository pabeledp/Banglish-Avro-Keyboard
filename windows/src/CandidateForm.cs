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
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

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
            this.BackColor = Color.FromArgb(11, 34, 23); // Deep Apple Emerald Glass (0x0B2217)
            this.ForeColor = Color.White;
            this.Size = new Size(180, 220);

            this.Paint += CandidateForm_Paint;
            this.MouseDown += CandidateForm_MouseDown;
            this.MouseMove += CandidateForm_MouseMove;
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

            // Calculate dynamic height based on candidate count
            int headerH = 28;
            int rowH = 32;
            int totalH = headerH + (candidates.Count * rowH) + 8;
            int totalW = 190;

            // Measure longest candidate
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
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 18, 18));

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
            // 38px offset below caret line to ensure zero overlap with typing
            int y = caretPos.Y + 38;

            if (x + this.Width > screen.Right)
            {
                x = screen.Right - this.Width - 12;
            }
            if (y + this.Height > screen.Bottom)
            {
                // Position safely above the text line if near bottom
                y = caretPos.Y - this.Height - 14;
            }

            if (x < screen.Left + 8) x = screen.Left + 8;
            if (y < screen.Top + 8) y = screen.Top + 8;

            this.Location = new Point(x, y);
        }

        public string GetSelectedCandidate()
        {
            if (selectedIndex >= 0 && selectedIndex < candidates.Count)
            {
                return candidates[selectedIndex];
            }
            return candidates.Count > 0 ? candidates[0] : "";
        }

        public void SelectNext()
        {
            if (candidates.Count == 0) return;
            selectedIndex = (selectedIndex + 1) % candidates.Count;
            this.Invalidate();
        }

        public void SelectPrevious()
        {
            if (candidates.Count == 0) return;
            selectedIndex = (selectedIndex - 1 + candidates.Count) % candidates.Count;
            this.Invalidate();
        }

        private void CandidateForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Apple Liquid Glass Background
            using (var brush = new LinearGradientBrush(this.ClientRectangle,
                Color.FromArgb(245, 11, 34, 23),
                Color.FromArgb(240, 5, 46, 22),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, this.ClientRectangle);
            }

            // 2. Glass Glow Border
            using (var borderPen = new Pen(Color.FromArgb(160, 45, 106, 79), 1.5f))
            {
                g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }

            // 3. Header bar: "Banglish (বাংলা) • F12"
            using (var headerBrush = new SolidBrush(Color.FromArgb(167, 243, 208))) // Mint Emerald
            {
                g.DrawString("Banglish (বাংলা) • F12", fontEnglish, headerBrush, 12, 7);
            }

            using (var linePen = new Pen(Color.FromArgb(40, 52, 211, 153)))
            {
                g.DrawLine(linePen, 10, 26, this.Width - 10, 26);
            }

            // 4. Candidate Rows
            int yOffset = 30;
            int rowH = 32;

            for (int i = 0; i < candidates.Count; i++)
            {
                bool isSelected = (i == selectedIndex);
                bool isLastRaw = (i == candidates.Count - 1 && candidates[i] == currentRaw);
                Rectangle rowRect = new Rectangle(6, yOffset, this.Width - 12, rowH);

                if (isSelected)
                {
                    // Glowing Emerald Pill for Selected Item
                    using (var pillBrush = new SolidBrush(Color.FromArgb(220, 21, 128, 61)))
                    using (var pillPen = new Pen(Color.FromArgb(180, 52, 211, 153), 1f))
                    {
                        var pillPath = GetRoundedRectPath(rowRect, 8);
                        g.FillPath(pillBrush, pillPath);
                        g.DrawPath(pillPen, pillPath);
                    }
                }

                // Number Badge (1, 2, 3...)
                string numStr = (i + 1).ToString();
                Color numColor = isSelected ? Color.White : Color.FromArgb(167, 243, 208);
                using (var numBrush = new SolidBrush(numColor))
                {
                    g.DrawString(numStr + ".", fontNumber, numBrush, 12, yOffset + 7);
                }

                // Word Text
                string word = candidates[i];
                Color wordColor;
                Font wordFont;

                if (isLastRaw)
                {
                    wordColor = Color.FromArgb(134, 239, 172); // Luminous mint green
                    wordFont = fontEnglish;
                }
                else
                {
                    wordColor = Color.White;
                    wordFont = isSelected ? new Font(fontBangla, FontStyle.Bold) : fontBangla;
                }

                using (var textBrush = new SolidBrush(wordColor))
                {
                    g.DrawString(word, wordFont, textBrush, 34, yOffset + 4);
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
            int rowIdx = (e.Y - 30) / 32;
            if (rowIdx >= 0 && rowIdx < candidates.Count && rowIdx != selectedIndex)
            {
                selectedIndex = rowIdx;
                this.Invalidate();
            }
        }

        private void CandidateForm_MouseDown(object sender, MouseEventArgs e)
        {
            int rowIdx = (e.Y - 30) / 32;
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
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW (Native Apple/Windows soft drop shadow)
                return cp;
            }
        }
    }
}
