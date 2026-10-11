using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Banglish.Core;
using Banglish.UI;
#if VOICE_BETA
using Banglish.Voice;
#endif

namespace Banglish
{
    public class HotkeyMessageWindow : NativeWindow, IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9112;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_F12 = 0x7B;

#if VOICE_BETA
        private const int HOTKEY_VOICE_ID = 9113;
        private const uint MOD_CONTROL = 0x0002;
        public event Action VoiceHotkeyPressed;
#endif

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public event Action HotkeyPressed;

        public HotkeyMessageWindow()
        {
            CreateHandle(new CreateParams());

            // 1. Language Toggle: F12
            bool ok = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_NOREPEAT, VK_F12);
            if (!ok)
            {
                RegisterHotKey(this.Handle, HOTKEY_ID, 0, VK_F12);
            }

#if VOICE_BETA
            // 2. Voice Typing Toggle: Ctrl + F12
            bool voiceOk = RegisterHotKey(this.Handle, HOTKEY_VOICE_ID, MOD_CONTROL | MOD_NOREPEAT, VK_F12);
            if (!voiceOk)
            {
                RegisterHotKey(this.Handle, HOTKEY_VOICE_ID, MOD_CONTROL, VK_F12);
            }
#endif
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = (int)m.WParam;
                if (id == HOTKEY_ID)
                {
                    if (HotkeyPressed != null) HotkeyPressed();
                    return;
                }
#if VOICE_BETA
                else if (id == HOTKEY_VOICE_ID)
                {
                    if (VoiceHotkeyPressed != null) VoiceHotkeyPressed();
                    return;
                }
#endif
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID);
#if VOICE_BETA
                UnregisterHotKey(this.Handle, HOTKEY_VOICE_ID);
#endif
                DestroyHandle();
            }
            catch {}
        }
    }

    public class TrayApplication : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private KeyboardHook hook;
        private CandidateForm candidateWindow;
        private ToggleBarForm toggleWidget;
        private HotkeyMessageWindow hotkeyWindow;

        public TrayApplication()
        {
            InitializeApp();
        }

        private void InitializeApp()
        {
            RegisterInstalledApp();

            candidateWindow = new CandidateForm();

            hook = new KeyboardHook();

            toggleWidget = new ToggleBarForm(hook);
            toggleWidget.Show();

            hotkeyWindow = new HotkeyMessageWindow();
            hotkeyWindow.HotkeyPressed += () =>
            {
                if (hook != null)
                {
                    hook.ToggleMode();
                }
            };

#if VOICE_BETA
            hotkeyWindow.VoiceHotkeyPressed += () =>
            {
                VoiceTypingManager.Shared.ToggleVoiceTyping();
            };
            hook.VoiceHotkeyPressed += () =>
            {
                VoiceTypingManager.Shared.ToggleVoiceTyping();
            };
#endif

            candidateWindow.OnSelectCandidate = (chosen) =>
            {
                hook.CommitSelectedCandidate(chosen);
            };

            // Non-blocking asynchronous UI dispatch prevents low-level hook timeouts
            hook.CandidatesChanged += (raw, candList, selIdx) =>
            {
                if (candidateWindow != null && !candidateWindow.IsDisposed)
                {
                    if (candidateWindow.InvokeRequired)
                    {
                        candidateWindow.BeginInvoke(new Action(() => candidateWindow.UpdateCandidates(raw, candList, selIdx)));
                    }
                    else
                    {
                        candidateWindow.UpdateCandidates(raw, candList, selIdx);
                    }
                }
            };

            hook.ModeToggled += (enabled) =>
            {
                if (candidateWindow != null && !candidateWindow.IsDisposed)
                {
                    if (!enabled)
                    {
                        if (candidateWindow.InvokeRequired)
                            candidateWindow.BeginInvoke(new Action(() => candidateWindow.Hide()));
                        else
                            candidateWindow.Hide();
                    }
                }

                if (toggleWidget != null && !toggleWidget.IsDisposed)
                {
                    toggleWidget.SetMode(enabled);
                }

                string modeName = enabled ? "Bangla (বাংলা)" : "English (ENG)";
                if (trayIcon != null)
                {
                    trayIcon.Text = "Banglish — " + modeName + " (F12)";
                }
            };

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Renderer = new DarkMenuRenderer();
            contextMenu.ShowImageMargin = false;
            contextMenu.AutoClose = true;
            contextMenu.Font = ToggleBarForm.GetBestFont(new[] { "Hind Siliguri", "Nirmala UI", "Segoe UI" }, 9.5f, FontStyle.Regular);

            ToolStripMenuItem toggleItem = new ToolStripMenuItem("🔄  ভাষা পরিবর্তন (F12)", null, (s, e) =>
            {
                hook.ToggleMode();
            });
            toggleItem.Font = new Font(toggleItem.Font, FontStyle.Bold);

            contextMenu.Items.Add(toggleItem);

#if VOICE_BETA
            ToolStripMenuItem voiceItem = new ToolStripMenuItem("🎙️  ভয়েস টাইপিং (Ctrl+F12)", null, (s, e) =>
            {
                VoiceTypingManager.Shared.ToggleVoiceTyping();
            });
            contextMenu.Items.Add(voiceItem);
#endif

            ToolStripMenuItem toggleBarItem = new ToolStripMenuItem("👁️  টগল বার দেখান / লুকান", null, (s, e) =>
            {
                if (toggleWidget.Visible) toggleWidget.Hide();
                else toggleWidget.Show();
            });
            contextMenu.Items.Add(toggleBarItem);
            contextMenu.Items.Add(new ToolStripSeparator());

#if VOICE_BETA
            ToolStripMenuItem voiceSettingsItem = new ToolStripMenuItem("⚙️  ভয়েস সেটিংস (Google API Key)", null, (s, e) => ShowVoiceSettings());
            contextMenu.Items.Add(voiceSettingsItem);
#endif

            ToolStripMenuItem welcomeItem = new ToolStripMenuItem("📖  Welcome & Quick Guide", null, (s, e) => new WelcomeForm().Show());
            ToolStripMenuItem testItem = new ToolStripMenuItem("🧪  Open Transliteration Tester", null, (s, e) => OpenTestWindow());
            ToolStripMenuItem aboutItem = new ToolStripMenuItem("ℹ️  About Banglish Windows", null, (s, e) => ShowAbout());
            ToolStripMenuItem exitItem = new ToolStripMenuItem("❌  Exit Banglish", null, (s, e) => ExitApp());
            exitItem.ForeColor = Color.FromArgb(248, 113, 113);

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
                Text = "Banglish — Bangla (বাংলা) (F12)",
                Visible = true
            };

            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    hook.ToggleMode();
                }
            };

            hook.Start();
        }

#if VOICE_BETA
        private void ShowVoiceSettings()
        {
            Form form = new Form
            {
                Text = "Banglish — ভয়েস টাইপিং সেটিংস (Beta)",
                Size = new Size(480, 260),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblKey = new Label
            {
                Text = "Google Cloud Speech-to-Text API Key:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            TextBox txtKey = new TextBox
            {
                Location = new Point(20, 45),
                Width = 425,
                Font = new Font("Consolas", 10f),
                Text = BanglishConfig.Shared.ApiKey ?? ""
            };

            Label lblInfo = new Label
            {
                Text = "শর্টকাট: Ctrl + F12 চাপলে ভয়েস টাইপিং চালু বা বন্ধ হবে।\nঅথবা টাস্কবারের কর্নার টগলে থাকা মাইক্রোফোন আইকনে ক্লিক করুন।",
                Location = new Point(20, 80),
                Size = new Size(425, 45),
                ForeColor = Color.DimGray,
                Font = new Font("Hind Siliguri", 9.5f, FontStyle.Regular)
            };

            CheckBox chkPunct = new CheckBox
            {
                Text = "স্বয়ংক্রিয় যতিচিহ্ন যুক্ত করুন (Automatic Punctuation)",
                Location = new Point(20, 135),
                AutoSize = true,
                Checked = BanglishConfig.Shared.AutoPunctuation,
                Font = new Font("Hind Siliguri", 9.5f, FontStyle.Regular)
            };

            Button btnSave = new Button
            {
                Text = "সংরক্ষণ করুন (Save)",
                Location = new Point(20, 175),
                Width = 160,
                Height = 32,
                BackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Hind Siliguri", 9.5f, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;

            btnSave.Click += (s, e) =>
            {
                BanglishConfig.Shared.ApiKey = txtKey.Text.Trim();
                BanglishConfig.Shared.AutoPunctuation = chkPunct.Checked;
                BanglishConfig.Shared.Save();
                MessageBox.Show("সেটিংস সফলভাবে সংরক্ষিত হয়েছে!", "Banglish", MessageBoxButtons.OK, MessageBoxIcon.Information);
                form.Close();
            };

            form.Controls.Add(lblKey);
            form.Controls.Add(txtKey);
            form.Controls.Add(lblInfo);
            form.Controls.Add(chkPunct);
            form.Controls.Add(btnSave);

            form.ShowDialog();
        }
#endif

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
#if VOICE_BETA
            string title = "Banglish for Windows v1.0.1 (Beta with Voice Typing)\n";
            string desc = "Toggle Key: F12\nVoice Typing: Ctrl + F12\n";
#else
            string title = "Banglish for Windows v1.0.0 (Stable)\n";
            string desc = "Toggle Key: F12\n";
#endif
            MessageBox.Show(
                title +
                "100% Native, Fast & Free Phonetic Bangla Keyboard\n\n" +
                "Developed by A M Pabel & Team FramEmpire\n" +
                desc +
                "GitHub: https://github.com/pabeledp/Banglish-Avro-Keyboard",
                "About Banglish", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private void ExitApp()
        {
            try
            {
                if (hotkeyWindow != null) hotkeyWindow.Dispose();
#if VOICE_BETA
                VoiceTypingManager.Shared.Dispose();
#endif
                if (toggleWidget != null) toggleWidget.Close();
                if (candidateWindow != null) candidateWindow.Close();
                if (hook != null)
                {
                    hook.Stop();
                    hook.Dispose();
                }
                if (trayIcon != null) trayIcon.Visible = false;
            }
            catch {}
            Application.Exit();
        }

        private void RegisterInstalledApp()
        {
            try
            {
                bool isBeta = false;
#if VOICE_BETA
                isBeta = true;
#endif
                string appName = isBeta ? "Banglish (Beta)" : "Banglish";
                string appVersion = isBeta ? "1.0.1-beta" : "1.0.0";
                string regKeyName = isBeta ? "Banglish-Beta" : "Banglish";

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string icoPath = Path.Combine(baseDir, "app.ico");
                string displayIcon = File.Exists(icoPath) ? icoPath : (exePath + ",0");

                string uninstallPath = Path.Combine(baseDir, "Banglish-Uninstall.exe");
                string uninstallCmd = "\"" + (File.Exists(uninstallPath) ? uninstallPath : exePath) + "\" --uninstall";
                string quietUninstallCmd = uninstallCmd + " --quiet";

                Action<Microsoft.Win32.RegistryKey, string> writeEntry = (root, subKey) =>
                {
                    try
                    {
                        using (var key = root.CreateSubKey(subKey))
                        {
                            if (key != null)
                            {
                                key.SetValue("DisplayName", appName);
                                key.SetValue("DisplayVersion", appVersion);
                                key.SetValue("DisplayIcon", displayIcon);
                                key.SetValue("Publisher", "Team FramEmpire & A M Pabel");
                                key.SetValue("InstallLocation", baseDir);
                                key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
                                key.SetValue("EstimatedSize", 52400, Microsoft.Win32.RegistryValueKind.DWord);
                                key.SetValue("UninstallString", uninstallCmd);
                                key.SetValue("QuietUninstallString", quietUninstallCmd);
                                key.SetValue("URLInfoAbout", "https://github.com/pabeledp/Banglish-Avro-Keyboard");
                                key.SetValue("HelpLink", "https://github.com/pabeledp/Banglish-Avro-Keyboard");
                                key.SetValue("NoModify", 1, Microsoft.Win32.RegistryValueKind.DWord);
                                key.SetValue("NoRepair", 1, Microsoft.Win32.RegistryValueKind.DWord);
                                key.SetValue("VersionMajor", 1, Microsoft.Win32.RegistryValueKind.DWord);
                                key.SetValue("VersionMinor", isBeta ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                                key.SetValue("MajorVersion", 1, Microsoft.Win32.RegistryValueKind.DWord);
                                key.SetValue("MinorVersion", isBeta ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                            }
                        }
                    }
                    catch {}
                };

                writeEntry(Microsoft.Win32.Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + regKeyName);
                writeEntry(Microsoft.Win32.Registry.CurrentUser, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + regKeyName);

                try
                {
                    writeEntry(Microsoft.Win32.Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + regKeyName);
                    writeEntry(Microsoft.Win32.Registry.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + regKeyName);
                }
                catch {}
            }
            catch {}
        }

        private static void PerformUninstall(bool quiet)
        {
            if (!quiet)
            {
                DialogResult res = MessageBox.Show(
                    "আপনি কি নিশ্চিতভাবে Banglish আনইনস্টল করতে চান?",
                    "Banglish Uninstall",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res != DialogResult.Yes) return;
            }

            try
            {
                // 1. Terminate running instances
                foreach (var p in Process.GetProcessesByName("Banglish"))
                {
                    if (p.Id != Process.GetCurrentProcess().Id)
                    {
                        try { p.Kill(); } catch {}
                    }
                }
                foreach (var p in Process.GetProcessesByName("Banglish-v1.0.1-beta"))
                {
                    if (p.Id != Process.GetCurrentProcess().Id)
                    {
                        try { p.Kill(); } catch {}
                    }
                }

                // 2. Remove Shortcuts
                try
                {
                    string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Banglish.lnk");
                    if (File.Exists(desktopLnk)) File.Delete(desktopLnk);

                    string startMenuLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs\\Banglish.lnk");
                    if (File.Exists(startMenuLnk)) File.Delete(startMenuLnk);
                }
                catch {}

                // 3. Remove Startup Run key
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue("Banglish", false);
                            key.DeleteValue("Banglish-Beta", false);
                        }
                    }
                }
                catch {}

                // 4. Remove Uninstall keys from Registry
                try
                {
                    Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Banglish", false);
                }
                catch {}
                try
                {
                    Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Banglish-Beta", false);
                }
                catch {}

                // 5. Delete installation folder via background self-delete
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                if (appDir.EndsWith("\\") || appDir.EndsWith("/")) appDir = appDir.Substring(0, appDir.Length - 1);

                if (appDir.IndexOf("Banglish", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c timeout /t 1 /nobreak > NUL & rmdir /s /q \"" + appDir + "\"",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    };
                    Process.Start(psi);
                }

                if (!quiet)
                {
                    MessageBox.Show(
                        "Banglish সফলভাবে আপনার কম্পিউটার থেকে আনইনস্টল করা হয়েছে।",
                        "Banglish Uninstall",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (!quiet)
                {
                    MessageBox.Show("আনইনস্টল ত্রুটি: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                if (args != null && args.Length > 0 && Array.Exists(args, a => a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase) || a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase)))
                {
                    bool quiet = Array.Exists(args, a => a.Equals("--quiet", StringComparison.OrdinalIgnoreCase) || a.Equals("/quiet", StringComparison.OrdinalIgnoreCase));
                    PerformUninstall(quiet);
                    return;
                }

                Process current = Process.GetCurrentProcess();
                foreach (Process p in Process.GetProcessesByName(current.ProcessName))
                {
                    if (p.Id != current.Id)
                    {
                        // Another instance of Banglish is already running
                        return;
                    }
                }

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
