using System;
using System.Drawing;
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

                string msg = enabled ? "Banglish (বাংলা) Mode Enabled" : "English Mode Enabled";
                trayIcon.ShowBalloonTip(1500, "Banglish Keyboard", msg + "\nPress F12 to switch anytime.", ToolTipIcon.Info);
            };

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem toggleItem = new ToolStripMenuItem("Toggle Banglish (F12)", null, (s, e) =>
            {
                hook.IsEnabled = !hook.IsEnabled;
                candidateWindow.SetMode(hook.IsEnabled);
            });
            ToolStripMenuItem testItem = new ToolStripMenuItem("Open Transliteration Tester", null, (s, e) => OpenTestWindow());
            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About Banglish Windows", null, (s, e) => ShowAbout());
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitApp());

            contextMenu.Items.Add(toggleItem);
            contextMenu.Items.Add(testItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(aboutItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = contextMenu,
                Text = "Banglish — Phonetic Bangla Keyboard for Windows",
                Visible = true
            };

            trayIcon.ShowBalloonTip(2500, "Banglish Keyboard Active", "Banglish is running in your System Tray.\nPress F12 to toggle between English and Bangla (বাংলা).", ToolTipIcon.Info);

            hook.Start();
        }

        private void OpenTestWindow()
        {
            Form testForm = new Form
            {
                Text = "Banglish Windows Transliteration Tester",
                Size = new Size(500, 350),
                StartPosition = FormStartPosition.CenterScreen
            };

            Label lblIn = new Label { Text = "Type Banglish Phonetic Text:", Location = new Point(15, 15), AutoSize = true };
            TextBox txtIn = new TextBox { Location = new Point(15, 35), Width = 450, Font = new Font("Consolas", 11f) };
            Label lblOut = new Label { Text = "Bangla Transliterated Output:", Location = new Point(15, 80), AutoSize = true };
            TextBox txtOut = new TextBox
            {
                Location = new Point(15, 100),
                Width = 450,
                Height = 160,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Nirmala UI", 14f, FontStyle.Bold)
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplication());
        }
    }
}
