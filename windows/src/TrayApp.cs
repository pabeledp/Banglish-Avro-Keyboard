using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Banglish.Core;
using Banglish.UI;

namespace Banglish
{
    public class TrayApplication : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private KeyboardHook hook;
        private CandidateForm candidateWindow;

        public TrayApplication()
        {
            InitializeApp();
        }

        private void InitializeApp()
        {
            candidateWindow = new CandidateForm();

            hook = new KeyboardHook();
            hook.BufferChanged += (raw, bangla) =>
            {
                if (candidateWindow.InvokeRequired)
                {
                    candidateWindow.Invoke(new Action(() => candidateWindow.UpdatePreview(raw, bangla)));
                }
                else
                {
                    candidateWindow.UpdatePreview(raw, bangla);
                }
            };

            hook.ModeToggled += (enabled) =>
            {
                if (candidateWindow.InvokeRequired)
                {
                    candidateWindow.Invoke(new Action(() => candidateWindow.SetMode(enabled)));
                }
                else
                {
                    candidateWindow.SetMode(enabled);
                }

                string msg = enabled ? "Banglish (বাংলা) Mode Active" : "English Mode Active";
                trayIcon.ShowBalloonTip(1500, "Banglish Keyboard", msg + "\nPress F12 anytime to switch.", ToolTipIcon.Info);
            };

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem toggleItem = new ToolStripMenuItem("Toggle Banglish (F12)", null, (s, e) =>
            {
                hook.IsEnabled = !hook.IsEnabled;
                candidateWindow.SetMode(hook.IsEnabled);
            });
            ToolStripMenuItem testItem = new ToolStripMenuItem("Open Transliteration Tester", null, (s, e) => OpenTestWindow());
            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About Banglish Windows", null, (s, e) => ShowAbout());
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit Banglish", null, (s, e) => ExitApp());

            contextMenu.Items.Add(toggleItem);
            contextMenu.Items.Add(testItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(aboutItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            Icon appIcon = SystemIcons.Application;
            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Banglish-Logo.png");
                if (File.Exists(logoPath))
                {
                    byte[] bytes = File.ReadAllBytes(logoPath);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    using (Bitmap bmp = new Bitmap(ms))
                    {
                        IntPtr hIcon = bmp.GetHicon();
                        appIcon = (Icon)Icon.FromHandle(hIcon).Clone();
                        DestroyIcon(hIcon);
                    }
                }
            }
            catch {}

            trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                ContextMenuStrip = contextMenu,
                Text = "Banglish — Native Bangla Keyboard for Windows",
                Visible = true
            };

            trayIcon.ShowBalloonTip(2500, "Banglish Keyboard Running", "Banglish is active in System Tray.\nPress F12 to toggle Bangla (বাংলা) mode.", ToolTipIcon.Info);

            hook.Start();
        }

        private void OpenTestWindow()
        {
            Form testForm = new Form
            {
                Text = "Banglish Windows Transliteration Tester",
                Size = new Size(520, 360),
                StartPosition = FormStartPosition.CenterScreen
            };

            Label lblIn = new Label { Text = "Type Banglish Phonetic Text:", Location = new Point(15, 15), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            TextBox txtIn = new TextBox { Location = new Point(15, 38), Width = 470, Font = new Font("Consolas", 11f) };
            Label lblOut = new Label { Text = "Bangla Transliterated Output:", Location = new Point(15, 85), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            TextBox txtOut = new TextBox
            {
                Location = new Point(15, 110),
                Width = 470,
                Height = 180,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Nirmala UI", 15f, FontStyle.Bold),
                BackColor = Color.White
            };

            txtIn.TextChanged += (s, e) =>
            {
                txtOut.Text = BanglishEngine.Shared.Transliterate(txtIn.Text);
            };

            testForm.Controls.Add(lblIn);
            testForm.Controls.Add(txtIn);
            testForm.Controls.Add(lblOut);
            testForm.Controls.Add(txtOut);

            testForm.Show();
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "Banglish for Windows\n" +
                "100% Native, Fast & Free Phonetic Bangla Keyboard\n\n" +
                "Developed by A M Pabel & Team FramEmpire\n" +
                "Toggle Key: F12\n" +
                "GitHub: https://github.com/pabeledp/Banglish-Avro-Keyboard",
                "About Banglish", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private void ExitApp()
        {
            hook.Stop();
            hook.Dispose();
            trayIcon.Visible = false;
            Application.Exit();
        }

        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApplication());
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), ex.ToString());
                }
                catch {}
                MessageBox.Show(ex.Message, "Banglish Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
