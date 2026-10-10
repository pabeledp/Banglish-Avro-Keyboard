using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Banglish.Voice
{
    public enum VoiceState
    {
        Idle,
        Listening,
        Processing,
        Success,
        Error
    }

    public class VoiceIndicatorForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        private VoiceState _state = VoiceState.Idle;
        private float _currentLevel = 0.05f;
        private float _smoothedLevel = 0.05f;
        private string _statusText = "কথা বলুন...";
        private System.Windows.Forms.Timer _animTimer;
        private int _pulseTick = 0;

        private readonly Font _fontTitle;
        private readonly Font _fontStatus;

        public event Action OnClickStop;

        public VoiceIndicatorForm()
        {
            _fontTitle = GetBestFont(new[] { "Hind Siliguri", "Nirmala UI", "Segoe UI" }, 10f, FontStyle.Bold);
            _fontStatus = GetBestFont(new[] { "Hind Siliguri", "Nirmala UI", "Segoe UI" }, 8.5f, FontStyle.Regular);

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
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.Size = new Size(250, 48);
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.FromArgb(15, 23, 42); // Slate-900

            this.Paint += VoiceIndicatorForm_Paint;
            this.Click += (s, e) => { if (OnClickStop != null) OnClickStop(); };

            _animTimer = new System.Windows.Forms.Timer();
            _animTimer.Interval = 30; // ~33 FPS animation
            _animTimer.Tick += AnimTimer_Tick;

            UpdateRegion();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                if (Environment.OSVersion.Version.Build >= 22000)
                {
                    int cornerPref = 2; // DWMWCP_ROUND (Windows 11 Hardware Rounded Corners)
                    DwmSetWindowAttribute(this.Handle, 33, ref cornerPref, sizeof(int));
                }
            }
            catch {}
        }

        private void UpdateRegion()
        {
            if (Environment.OSVersion.Version.Build < 22000)
            {
                this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width + 1, this.Height + 1, 16, 16));
            }
            else
            {
                this.Region = null;
            }
        }

        public void SetState(VoiceState state, string message = null)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SetState(state, message)));
                return;
            }

            _state = state;
            switch (state)
            {
                case VoiceState.Listening:
                    _statusText = string.IsNullOrEmpty(message) ? "কথা বলুন... (Ctrl+F12)" : message;
                    if (!_animTimer.Enabled) _animTimer.Start();
                    break;
                case VoiceState.Processing:
                    _statusText = string.IsNullOrEmpty(message) ? "প্রসেসিং হচ্ছে..." : message;
                    break;
                case VoiceState.Success:
                    _statusText = string.IsNullOrEmpty(message) ? "টাইপ করা হয়েছে!" : message;
                    break;
                case VoiceState.Error:
                    _statusText = string.IsNullOrEmpty(message) ? "সমস্যা হয়েছে" : message;
                    break;
                case VoiceState.Idle:
                    _animTimer.Stop();
                    this.Hide();
                    return;
            }

            PositionBottomCenter();
            if (!this.Visible) this.Show();
            this.Invalidate();
        }

        public void UpdateAudioLevel(float level)
        {
            _currentLevel = Math.Max(0.05f, Math.Min(1.0f, level));
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            _pulseTick++;
            // Smooth dampening towards current peak
            _smoothedLevel += (_currentLevel - _smoothedLevel) * 0.25f;
            this.Invalidate();
        }

        public void PositionBottomCenter()
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int x = (wa.Width - this.Width) / 2;
            int y = wa.Bottom - this.Height - 32; // Floating nicely at bottom center
            this.Location = new Point(x, y);
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

        private void VoiceIndicatorForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = this.ClientRectangle;
            Rectangle borderRect = new Rectangle(0, 0, rect.Width - 1, rect.Height - 1);

            // 1. Draw Sleek Rounded Acrylic Dark Glass Surface
            using (var path = GetRoundedRectPath(borderRect, 14))
            {
                using (var bgBrush = new SolidBrush(Color.FromArgb(245, 15, 23, 42)))
                {
                    g.FillPath(bgBrush, path);
                }

                Color borderColor;
                if (_state == VoiceState.Listening) borderColor = Color.FromArgb(16, 185, 129); // Emerald
                else if (_state == VoiceState.Processing) borderColor = Color.FromArgb(59, 130, 246); // Blue
                else if (_state == VoiceState.Error) borderColor = Color.FromArgb(239, 68, 68); // Red
                else borderColor = Color.FromArgb(50, 255, 255, 255);

                using (var pen = new Pen(borderColor, 1.2f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 2. Pulse Dot / Microphone Status Icon
            int dotX = 16;
            int dotY = 16;
            int dotSize = 16;

            if (_state == VoiceState.Listening)
            {
                // Pulsing Red/Emerald Recording Glow
                int pulseAlpha = (int)(150 + 105 * Math.Sin(_pulseTick * 0.15));
                using (var glowBrush = new SolidBrush(Color.FromArgb(pulseAlpha / 3, 239, 68, 68)))
                {
                    g.FillEllipse(glowBrush, dotX - 4, dotY - 4, dotSize + 8, dotSize + 8);
                }
                using (var dotBrush = new SolidBrush(Color.FromArgb(239, 68, 68)))
                {
                    g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
                }
            }
            else if (_state == VoiceState.Processing)
            {
                // Pulsing Blue Spinner/Dot
                int pulseAlpha = (int)(160 + 90 * Math.Sin(_pulseTick * 0.2));
                using (var dotBrush = new SolidBrush(Color.FromArgb(pulseAlpha, 59, 130, 246)))
                {
                    g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
                }
            }
            else
            {
                using (var dotBrush = new SolidBrush(Color.FromArgb(16, 185, 129)))
                {
                    g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
                }
            }

            // 3. Status Text
            using (var textBrush = new SolidBrush(Color.White))
            {
                g.DrawString(_statusText, _fontTitle, textBrush, 42, 8);
            }

            string sub = _state == VoiceState.Listening ? "Banglish Voice Typing" : "Google Cloud Speech";
            using (var subBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                g.DrawString(sub, _fontStatus, subBrush, 43, 27);
            }

            // 4. Real-time Audio Waveform Visualizer (7 animated bars)
            if (_state == VoiceState.Listening)
            {
                int waveStartX = 195;
                int waveCenterY = 24;
                int barWidth = 3;
                int barGap = 4;

                for (int i = 0; i < 7; i++)
                {
                    // Staggered sinusoidal heights influenced by live audio volume
                    double waveFactor = Math.Abs(Math.Sin((_pulseTick * 0.2) + (i * 0.8)));
                    float barHeight = Math.Max(4f, (_smoothedLevel * 24f * (float)waveFactor) + (float)(waveFactor * 8f));
                    if (barHeight > 24f) barHeight = 24f;

                    int bx = waveStartX + (i * (barWidth + barGap));
                    int by = (int)(waveCenterY - (barHeight / 2));

                    using (var barBrush = new SolidBrush(Color.FromArgb(16, 185, 129))) // Emerald Green
                    {
                        g.FillRectangle(barBrush, bx, by, barWidth, (int)barHeight);
                    }
                }
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
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE (Crucial: never steals typing focus)
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
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
