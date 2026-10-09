using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Banglish.Core;

namespace Banglish.UI
{
    public class WelcomeForm : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        private readonly Font fontBanglaTitle;
        private readonly Font fontBanglaSub;
        private readonly Font fontEnglishBrand;
        private readonly Font fontEnglishSub;

        private Image logoImg;
        private TextBox txtTestInput;
        private Label lblTestOutput;

        public WelcomeForm()
        {
            fontBanglaTitle = GetBestFont(new[] { "Hind Siliguri", "Nirmala UI", "Vrinda" }, 22f, FontStyle.Bold);
            fontBanglaSub = GetBestFont(new[] { "Hind Siliguri", "Nirmala UI", "Vrinda" }, 12f, FontStyle.Regular);
            fontEnglishBrand = GetBestFont(new[] { "Creato Display", "Plus Jakarta Sans", "Segoe UI" }, 18f, FontStyle.Bold);
            fontEnglishSub = GetBestFont(new[] { "Creato Display", "Plus Jakarta Sans", "Segoe UI" }, 9f, FontStyle.Bold);

            InitializeUI();
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

        private void InitializeUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(720, 520);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;

            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Banglish-Logo.png");
                if (File.Exists(logoPath))
                {
                    byte[] bytes = File.ReadAllBytes(logoPath);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        logoImg = new Bitmap(ms);
                    }
                }
            }
            catch {}

            // Close (X) button
            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(32, 32),
                Location = new Point(this.Width - 44, 12),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            // Live Interactive Test Box container
            Panel pnlTest = new Panel
            {
                Location = new Point(48, 255),
                Size = new Size(624, 150),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlTest.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(209, 250, 229), 1.5f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlTest.Width - 1, pnlTest.Height - 1);
                }
            };

            Label lblPrompt = new Label
            {
                Text = "লাইভ টেস্ট বক্স (এখানে টাইপ করে পরীক্ষা করুন):",
                Font = new Font(fontBanglaSub, FontStyle.Bold),
                ForeColor = Color.FromArgb(4, 120, 87),
                Location = new Point(14, 10),
                AutoSize = true
            };

            txtTestInput = new TextBox
            {
                Location = new Point(16, 36),
                Width = 590,
                Font = new Font("Consolas", 12f),
                Text = "ami banglay gan gai"
            };

            lblTestOutput = new Label
            {
                Location = new Point(16, 75),
                Size = new Size(590, 60),
                Font = new Font("Hind Siliguri", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Text = BanglishEngine.Shared.Transliterate("ami banglay gan gai")
            };

            txtTestInput.TextChanged += (s, e) =>
            {
                lblTestOutput.Text = BanglishEngine.Shared.Transliterate(txtTestInput.Text);
            };

            pnlTest.Controls.Add(lblPrompt);
            pnlTest.Controls.Add(txtTestInput);
            pnlTest.Controls.Add(lblTestOutput);
            this.Controls.Add(pnlTest);

            // Primary Start Button
            Button btnStart = new Button
            {
                Text = "টাইপ করা শুরু করুন  →",
                Font = new Font(fontBanglaSub, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105), // Emerald-600
                FlatStyle = FlatStyle.Flat,
                Size = new Size(240, 46),
                Location = new Point((this.Width - 240) / 2, 425),
                Cursor = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            btnStart.Click += (s, e) => this.Close();
            this.Controls.Add(btnStart);

            // Developer Credit
            Label lblCredit = new Label
            {
                Text = "Crafted with ❤️ by FramEmpire",
                Font = new Font("Creato Display", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(300, 22),
                Location = new Point((this.Width - 300) / 2, 482),
                Cursor = Cursors.Hand
            };
            lblCredit.Click += (s, e) =>
            {
                try { System.Diagnostics.Process.Start("https://github.com/pabeledp"); } catch {}
            };
            this.Controls.Add(lblCredit);

            this.Paint += WelcomeForm_Paint;
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 20, 20));
        }

        private void WelcomeForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Soft Emerald Gradient Background (Website Theme)
            using (var brush = new LinearGradientBrush(this.ClientRectangle,
                Color.FromArgb(240, 253, 244), // Emerald-50
                Color.White,
                LinearGradientMode.ForwardDiagonal))
            {
                g.FillRectangle(brush, this.ClientRectangle);
            }

            // 2. Subtle Watermark Bengali Glyphs (अ, আ, ক, ব) in background
            using (var wmBrush = new SolidBrush(Color.FromArgb(18, 5, 150, 105)))
            using (var wmFont = new Font("Hind Siliguri", 72f, FontStyle.Bold))
            {
                g.DrawString("অ", wmFont, wmBrush, 20, 40);
                g.DrawString("আ", wmFont, wmBrush, this.Width - 110, 80);
                g.DrawString("ক", wmFont, wmBrush, this.Width - 120, 360);
                g.DrawString("ব", wmFont, wmBrush, 30, 370);
            }

            // 3. Header: Logo + "Banglish" + Tagline
            int headerX = 48;
            int headerY = 24;

            if (logoImg != null)
            {
                g.DrawImage(logoImg, new Rectangle(headerX, headerY, 40, 40));
            }

            using (var brandBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("Banglish", fontEnglishBrand, brandBrush, headerX + 48, headerY);
            }

            // Green pulsing dot
            using (var dotBrush = new SolidBrush(Color.FromArgb(16, 185, 129)))
            {
                g.FillEllipse(dotBrush, headerX + 172, headerY + 10, 8, 8);
            }

            using (var tagBrush = new SolidBrush(Color.FromArgb(4, 120, 87)))
            {
                g.DrawString("বাংলায় লিখি বিজয়ের সুর", fontBanglaSub, tagBrush, headerX + 48, headerY + 24);
            }

            // 4. Center Pill Badge
            string pillText = "Windows 10 & 11 • ১০০% নেটিভ ও সম্পূর্ণ ফ্রি";
            var szPill = g.MeasureString(pillText, fontBanglaSub);
            int pillW = (int)szPill.Width + 28;
            int pillH = 28;
            int pillX = (this.Width - pillW) / 2;
            int pillY = 92;

            Rectangle pillRect = new Rectangle(pillX, pillY, pillW, pillH);
            using (var pillBg = new SolidBrush(Color.FromArgb(236, 253, 245))) // Emerald-50
            using (var pillBorder = new Pen(Color.FromArgb(167, 243, 208), 1f)) // Emerald-200
            using (var pillTextBrush = new SolidBrush(Color.FromArgb(6, 95, 70)))
            {
                var path = GetRoundedRectPath(pillRect, 14);
                g.FillPath(pillBg, path);
                g.DrawPath(pillBorder, path);
                g.DrawString(pillText, fontBanglaSub, pillTextBrush, pillX + 14, pillY + 4);
            }

            // 5. Main Hero Heading (Website copy)
            string titleMain = "Windows-এর জন্য দ্রুততম ও নিখুঁত বাংলা কীবোর্ড";
            using (var titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                var szTitle = g.MeasureString(titleMain, fontBanglaTitle);
                g.DrawString(titleMain, fontBanglaTitle, titleBrush, (this.Width - szTitle.Width) / 2, 134);
            }

            // 6. Subheading with Hotkey Notice
            string subText = "যেকোনো সফটওয়্যারে টাইপ করার সময় বাংলা ও ইংরেজির মধ্যে সুইচ করতে শুধু F12 চাপুন।";
            using (var subBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                var szSub = g.MeasureString(subText, fontBanglaSub);
                g.DrawString(subText, fontBanglaSub, subBrush, (this.Width - szSub.Width) / 2, 184);
            }

            // Feature Badges
            DrawFeatureBadge(g, "⌨️ F12 Instant Switch", 120, 218);
            DrawFeatureBadge(g, "⚡ Zero Latency", 310, 218);
            DrawFeatureBadge(g, "🔒 100% Offline", 480, 218);

            // 7. Outer Border
            using (var borderPen = new Pen(Color.FromArgb(209, 250, 229), 1.5f))
            {
                var fullPath = GetRoundedRectPath(new Rectangle(0, 0, this.Width - 1, this.Height - 1), 20);
                g.DrawPath(borderPen, fullPath);
            }
        }

        private void DrawFeatureBadge(Graphics g, string text, int x, int y)
        {
            var sz = g.MeasureString(text, fontEnglishSub);
            Rectangle rect = new Rectangle(x, y, (int)sz.Width + 16, 24);
            using (var bg = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            using (var brush = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                var path = GetRoundedRectPath(rect, 8);
                g.FillPath(bg, path);
                g.DrawPath(pen, path);
                g.DrawString(text, fontEnglishSub, brush, x + 8, y + 4);
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

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }
    }
}
