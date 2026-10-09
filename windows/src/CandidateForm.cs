using System;
using System.Drawing;
using System.Windows.Forms;

namespace Banglish.UI
{
    public class CandidateForm : Form
    {
        private Label lblMode;
        private Label lblRaw;
        private Label lblBangla;
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
            this.BackColor = Color.FromArgb(18, 24, 27); // Modern dark surface
            this.Padding = new Padding(2);
            this.Size = new Size(320, 95);

            // Position at bottom-right corner by default
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Right - 340, screen.Bottom - 115);

            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = Color.FromArgb(4, 120, 87) // Emerald-700
            };

            lblMode = new Label
            {
                Text = " Banglish (বাংলা) — Toggle: F12",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblMode);

            lblRaw = new Label
            {
                ForeColor = Color.FromArgb(167, 243, 208), // Emerald-200
                Font = new Font("Consolas", 10f, FontStyle.Regular),
                Location = new Point(12, 33),
                Size = new Size(296, 20),
                Text = ""
            };

            lblBangla = new Label
            {
                ForeColor = Color.White,
                Font = new Font("Vrinda", 14f, FontStyle.Bold), // Vrinda or Nirmala UI for Bangla
                Location = new Point(10, 55),
                Size = new Size(296, 32),
                Text = ""
            };

            // Fallback font check for Bengali
            try
            {
                lblBangla.Font = new Font("Nirmala UI", 14f, FontStyle.Bold);
            }
            catch {}

            this.Controls.Add(lblBangla);
            this.Controls.Add(lblRaw);
            this.Controls.Add(pnlHeader);

            // Border styling
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

            lblRaw.Text = "Raw: " + raw;
            lblBangla.Text = bangla;

            if (!this.Visible)
            {
                this.Show();
            }
        }

        public void SetMode(bool isBangla)
        {
            if (isBangla)
            {
                pnlHeader.BackColor = Color.FromArgb(4, 120, 87);
                lblMode.Text = " Banglish (বাংলা) — Toggle: F12";
            }
            else
            {
                pnlHeader.BackColor = Color.FromArgb(71, 85, 105);
                lblMode.Text = " English Mode — Toggle: F12";
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
                baseParams.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE (Prevents taking focus from active app)
                baseParams.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return baseParams;
            }
        }
    }
}
