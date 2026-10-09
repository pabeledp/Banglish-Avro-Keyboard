using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Banglish.Core;

namespace Banglish.UI
{
    public class CandidateForm : Form
    {
        private Label lblMode;
        private Label lblRaw;
        private Label lblBangla;
        private PictureBox picLogo;
        private Panel pnlHeader;

        public CandidateForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.White;
            this.Padding = new Padding(2);
            this.Size = new Size(320, 100);

            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(5, 150, 105) // Emerald-600 (Mac Brand Color)
            };

            picLogo = new PictureBox
            {
                Size = new Size(22, 22),
                Location = new Point(6, 4),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            // Load logo from Banglish-Logo.png if available
            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Banglish-Logo.png");
                if (File.Exists(logoPath))
                {
                    byte[] bytes = File.ReadAllBytes(logoPath);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        picLogo.Image = new Bitmap(ms);
                    }
                }
            }
            catch {}

            lblMode = new Label
            {
                Text = "Banglish (বাংলা)  •  F12",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(32, 5),
                AutoSize = true
            };

            pnlHeader.Controls.Add(picLogo);
            pnlHeader.Controls.Add(lblMode);

            lblRaw = new Label
            {
                ForeColor = Color.FromArgb(4, 120, 87), // Emerald-700
                Font = new Font("Consolas", 10.5f, FontStyle.Bold),
                Location = new Point(12, 36),
                Size = new Size(296, 20),
                Text = ""
            };

            lblBangla = new Label
            {
                ForeColor = Color.FromArgb(15, 23, 42), // Slate-900
                Font = new Font("Hind Siliguri", 15f, FontStyle.Bold),
                Location = new Point(10, 58),
                Size = new Size(296, 36),
                Text = ""
            };

            // Font fallback chain for Bangla rendering
            try
            {
                lblBangla.Font = new Font("Nirmala UI", 15f, FontStyle.Bold);
            }
            catch {}

            this.Controls.Add(lblBangla);
            this.Controls.Add(lblRaw);
            this.Controls.Add(pnlHeader);

            // Emerald Border Paint
            this.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(16, 185, 129), 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };
        }

        public void UpdatePreview(string raw, string bangla)
        {
            if (string.IsNullOrEmpty(raw))
            {
                this.Hide();
                return;
            }

            lblRaw.Text = "Phonetic: " + raw;
            lblBangla.Text = bangla;

            PositionNearCaret();

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
            int y = caretPos.Y + 22; // 22px below current typing line

            if (x + this.Width > screen.Right)
            {
                x = screen.Right - this.Width - 10;
            }
            if (y + this.Height > screen.Bottom)
            {
                y = caretPos.Y - this.Height - 10; // Lift above line if near bottom
            }

            if (x < screen.Left) x = screen.Left + 10;
            if (y < screen.Top) y = screen.Top + 10;

            this.Location = new Point(x, y);
        }

        public void SetMode(bool isBangla)
        {
            if (isBangla)
            {
                pnlHeader.BackColor = Color.FromArgb(5, 150, 105);
                lblMode.Text = "Banglish (বাংলা)  •  F12";
            }
            else
            {
                pnlHeader.BackColor = Color.FromArgb(71, 85, 105);
                lblMode.Text = "English Mode  •  F12";
                this.Hide();
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
                CreateParams baseParams = base.CreateParams;
                baseParams.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE (Prevents taking focus)
                baseParams.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return baseParams;
            }
        }
    }
}
