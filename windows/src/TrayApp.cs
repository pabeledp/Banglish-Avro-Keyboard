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
        private ToggleBarForm toggleWidget;

        public TrayApplication()
        {
            InitializeApp();
        }

        private void InitializeApp()
        {
            candidateWindow = new CandidateForm();

            hook = new KeyboardHook();

            toggleWidget = new ToggleBarForm(hook);
            toggleWidget.Show();

            candidateWindow.OnSelectCandidate = (chosen) =>
            {
                hook.CommitSelectedCandidate(chosen);
            };

            hook.CandidatesChanged += (raw, candList, selIdx) =>
            {
                if (candidateWindow.InvokeRequired)
                {
                    candidateWindow.Invoke(new Action(() => candidateWindow.UpdateCandidates(raw, candList, selIdx)));
                }
                else
                {
                    candidateWindow.UpdateCandidates(raw, candList, selIdx);
                }
            };

            hook.ModeToggled += (enabled) =>
            {
                if (!enabled)
                {
                    if (candidateWindow.InvokeRequired)
                        candidateWindow.Invoke(new Action(() => candidateWindow.Hide()));
                    else
                        candidateWindow.Hide();
                }

                if (toggleWidget != null)
                {
                    toggleWidget.SetMode(enabled);
                }

                string msg = enabled ? "Banglish (বাংলা) Mode Active" : "English Mode Active";
                trayIcon.ShowBalloonTip(1500, "Banglish Keyboard", msg + "\nPress F12 anytime to switch.", ToolTipIcon.Info);
            };

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem toggleItem = new ToolStripMenuItem("Toggle Banglish (F12)", null, (s, e) =>
            {
                hook.ToggleMode();
            });
            ToolStripMenuItem toggleBarItem = new ToolStripMenuItem("টগল বার দেখান / লুকান", null, (s, e) =>
            {
                if (toggleWidget.Visible) toggleWidget.Hide();
                else toggleWidget.Show();
            });
            ToolStripMenuItem welcomeItem = new ToolStripMenuItem("Welcome & Quick Guide", null, (s, e) => new WelcomeForm().Show());
            ToolStripMenuItem testItem = new ToolStripMenuItem("Open Transliteration Tester", null, (s, e) => OpenTestWindow());
            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About Banglish Windows", null, (s, e) => ShowAbout());
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit Banglish", null, (s, e) => ExitApp());

            contextMenu.Items.Add(toggleItem);
            contextMenu.Items.Add(toggleBarItem);
            contextMenu.Items.Add(welcomeItem);
            contextMenu.Items.Add(testItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(aboutItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            CheckFirstRunWelcome();

            Icon appIcon = SystemIcons.Application;
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoPath))
                {
                    appIcon = new Icon(icoPath, 32, 32);
                }
                else
                {
                    using (Stream s = asm.GetManifestResourceStream("app.ico"))
                    {
                        if (s != null) appIcon = new Icon(s, 32, 32);
                    }
                }

                if (appIcon == SystemIcons.Application)
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
                    else
                    {
                        using (Stream s = asm.GetManifestResourceStream("Banglish-Logo.png"))
                        {
                            if (s != null)
                            {
                                using (Bitmap bmp = new Bitmap(s))
                                {
                                    IntPtr hIcon = bmp.GetHicon();
                                    appIcon = (Icon)Icon.FromHandle(hIcon).Clone();
                                    DestroyIcon(hIcon);
                                }
                            }
                        }
                    }
                }
            }
            catch {}

            trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                ContextMenuStrip = contextMenu,
                Text = "Banglish — Native Bangla Keyboard for Windows (F12)",
                Visible = true
            };

            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    hook.ToggleMode();
                }
            };

            trayIcon.ShowBalloonTip(2500, "Banglish Keyboard Running", "Banglish is active in System Tray & Corner Toggle.\nPress F12 or click the corner toggle to switch.", ToolTipIcon.Info);

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
                Font = new Font("Hind Siliguri", 15f, FontStyle.Bold),
                BackColor = Color.White
            };

            txtIn.TextChanged += (s, e) =>
            {
                var cands = BanglishDictionary.Shared.GetCandidates(txtIn.Text);
                txtOut.Text = string.Join("\r\n", cands);
            };

            testForm.Controls.Add(lblIn);
            testForm.Controls.Add(txtIn);
            testForm.Controls.Add(lblOut);
            testForm.Controls.Add(txtOut);

            testForm.Show();
        }

        private void CheckFirstRunWelcome()
        {
            try
            {
                string flagPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".first_run");
                string localDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Banglish");
                string userFlagPath = Path.Combine(localDataDir, ".welcome_shown");

                bool shouldShow = File.Exists(flagPath) || !File.Exists(userFlagPath);

                if (shouldShow)
                {
                    if (!Directory.Exists(localDataDir)) Directory.CreateDirectory(localDataDir);
                    File.WriteAllText(userFlagPath, DateTime.Now.ToString());
                    if (File.Exists(flagPath)) File.Delete(flagPath);

                    var welcome = new WelcomeForm();
                    welcome.Show();
                }
            }
            catch {}
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
            try
            {
                if (toggleWidget != null) toggleWidget.Close();
                if (candidateWindow != null) candidateWindow.Close();
                hook.Stop();
                hook.Dispose();
                trayIcon.Visible = false;
            }
            catch {}
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
