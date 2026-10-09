using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Banglish.Setup
{
    public class SetupWizard : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        private int currentStep = 0; // 0: Welcome, 1: Location, 2: Installing, 3: Finish

        private Panel pnlContent;
        private Button btnBack;
        private Button btnNext;
        private Button btnCancel;

        // Step 1 controls
        private TextBox txtInstallPath;
        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkStartup;

        // Step 2 controls
        private ProgressBar progressBar;
        private Label lblInstallStatus;

        // Step 3 controls
        private CheckBox chkLaunchNow;

        private Image logoImg;
        private string targetFolder;

        public static Font GetBestFont(string[] fontNames, float size, FontStyle style)
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

        public SetupWizard()
        {
            targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\Banglish");
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Banglish Setup";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(680, 480);
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
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoPath))
                {
                    this.Icon = new Icon(icoPath);
                }
                else if (logoImg != null)
                {
                    using (Bitmap b = new Bitmap(logoImg, 32, 32))
                    {
                        this.Icon = Icon.FromHandle(b.GetHicon());
                    }
                }
            }
            catch {}

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(240, 253, 244)
            };
            pnlHeader.Paint += PnlHeader_Paint;

            // Close button on header
            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 30),
                Location = new Point(this.Width - 40, 10),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            pnlHeader.Controls.Add(btnClose);

            // Bottom Navigation Footer (Height 70)
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240)))
                {
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
                }
            };

            // 1. Far Left Credit: FramEmpire (subtle, small, muted)
            LinkLabel lnkCompany = new LinkLabel
            {
                Text = "FramEmpire",
                Font = GetBestFont(new[] { "Creato Display", "Segoe UI" }, 8.5f, FontStyle.Regular),
                LinkColor = Color.FromArgb(148, 163, 184), // Slate-400 subtle gray
                ActiveLinkColor = Color.FromArgb(100, 116, 139),
                VisitedLinkColor = Color.FromArgb(148, 163, 184),
                LinkBehavior = LinkBehavior.HoverUnderline,
                Location = new Point(24, 28),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lnkCompany.LinkClicked += (s, e) =>
            {
                try { System.Diagnostics.Process.Start("https://www.framempire.com"); } catch {}
            };
            pnlFooter.Controls.Add(lnkCompany);

            // 2. Far Right Credit: A M Pabel (subtle, small, muted)
            LinkLabel lnkAuthor = new LinkLabel
            {
                Text = "A M Pabel",
                Font = GetBestFont(new[] { "Creato Display", "Segoe UI" }, 8.5f, FontStyle.Regular),
                LinkColor = Color.FromArgb(148, 163, 184), // Slate-400 subtle gray
                ActiveLinkColor = Color.FromArgb(100, 116, 139),
                VisitedLinkColor = Color.FromArgb(148, 163, 184),
                LinkBehavior = LinkBehavior.HoverUnderline,
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lnkAuthor.LinkClicked += (s, e) =>
            {
                try { System.Diagnostics.Process.Start("https://www.ampabel.com"); } catch {}
            };
            lnkAuthor.Location = new Point(this.Width - 95, 28);
            pnlFooter.Controls.Add(lnkAuthor);

            // 3. Centered Action Buttons (Back, Next, Cancel)
            // Total width = 110 + 10 + 130 + 10 + 95 = 355
            int startBtnsX = (this.Width - 355) / 2;
            btnBack = CreateNavButton("← পেছনে", startBtnsX, 17, 110, (s, e) => GoBack());
            btnNext = CreateNavButton("পরবর্তী →", startBtnsX + 120, 17, 130, (s, e) => GoNext());
            btnNext.BackColor = Color.FromArgb(5, 150, 105);
            btnNext.ForeColor = Color.White;
            btnNext.Font = GetBestFont(new[] { "Hind Siliguri", "Segoe UI" }, 10.5f, FontStyle.Bold);

            btnCancel = CreateNavButton("বাতিল", startBtnsX + 260, 17, 95, (s, e) => this.Close());

            pnlFooter.Controls.Add(btnBack);
            pnlFooter.Controls.Add(btnNext);
            pnlFooter.Controls.Add(btnCancel);

            // Main Content Area
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(30, 15, 30, 15)
            };

            // Add controls in correct docking order so Fill is never hidden
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);

            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 18, 18));
            ShowStep(0);
        }

        private Button CreateNavButton(string text, int x, int y, int w, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Font = GetBestFont(new[] { "Hind Siliguri", "Segoe UI" }, 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(w, 36),
                Location = new Point(x, y),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn.Click += onClick;
            return btn;
        }

        private void PnlHeader_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (logoImg != null)
            {
                g.DrawImage(logoImg, new Rectangle(25, 20, 40, 40));
            }

            using (var titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var font = GetBestFont(new[] { "Creato Display", "Segoe UI" }, 13f, FontStyle.Bold))
            {
                g.DrawString("Banglish — Setup Wizard", font, titleBrush, 75, 18);
            }

            using (var subBrush = new SolidBrush(Color.FromArgb(5, 150, 105)))
            using (var font = GetBestFont(new[] { "Hind Siliguri", "Segoe UI" }, 10.5f, FontStyle.Regular))
            {
                g.DrawString("বাংলায় লিখি বিজয়ের সুর  •  Windows Edition", font, subBrush, 75, 42);
            }

            using (var linePen = new Pen(Color.FromArgb(209, 250, 229)))
            {
                g.DrawLine(linePen, 0, 79, this.Width, 79);
            }
        }

        private void ShowStep(int step)
        {
            pnlContent.Controls.Clear();
            currentStep = step;

            btnBack.Enabled = (step == 1);

            switch (step)
            {
                case 0:
                    RenderWelcomeStep();
                    btnBack.Visible = true;
                    btnNext.Visible = true;
                    btnCancel.Visible = true;
                    btnNext.Text = "পরবর্তী →";
                    break;
                case 1:
                    RenderLocationStep();
                    btnBack.Visible = true;
                    btnNext.Visible = true;
                    btnCancel.Visible = true;
                    btnNext.Text = "ইনস্টল করুন";
                    break;
                case 2:
                    RenderInstallingStep();
                    btnBack.Enabled = false;
                    btnNext.Enabled = false;
                    btnCancel.Enabled = false;
                    StartInstallationProcess();
                    break;
                case 3:
                    RenderFinishStep();
                    btnBack.Visible = false;
                    btnCancel.Visible = false;
                    btnNext.Enabled = true;
                    btnNext.Visible = true;
                    btnNext.Text = "সমাপ্ত (Finish)";
                    btnNext.Location = new Point((this.Width - 140) / 2, 17);
                    btnNext.Size = new Size(140, 36);
                    break;
            }
        }

        private void RenderWelcomeStep()
        {
            Label lblTitle = new Label
            {
                Text = "Banglish কীবোর্ড ইনস্টলেশনে স্বাগতম",
                Font = GetBestFont(new[] { "Hind Siliguri", "Segoe UI" }, 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(30, 20),
                AutoSize = true
            };

            Label lblDesc = new Label
            {
                Text = "উইন্ডোজের যেকোনো সফটওয়্যারে দ্রুত ও নিখুঁতভাবে বাংলা টাইপ করুন।\n\n" +
                       "• ১০০% অফলাইন ও নিরাপদ (প্রাইভেসি সুরক্ষিত)\n" +
                       "• F12 বাটন চেপে যেকোনো মুহূর্তে বাংলা ও ইংরেজির মধ্যে সুইচ করুন\n" +
                       "• রিয়েল-টাইম ফোনেটিক সাজেশন এবং আধুনিক অ্যাপল-স্টাইল গ্লাস ইন্টারফেস\n\n" +
                       "ইনস্টলেশন শুরু করতে নিচের 'পরবর্তী' বাটনে ক্লিক করুন।",
                Font = GetBestFont(new[] { "Hind Siliguri", "Segoe UI" }, 11f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(30, 65),
                Size = new Size(590, 190)
            };

            pnlContent.Controls.Add(lblTitle);
            pnlContent.Controls.Add(lblDesc);
        }

        private void RenderLocationStep()
        {
            Label lblTitle = new Label
            {
                Text = "ইনস্টলেশন লোকেশন নির্বাচন করুন",
                Font = new Font("Hind Siliguri", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(30, 15),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "নিচের ফোল্ডারে Banglish ফাইলগুলো ইনস্টল করা হবে:",
                Font = new Font("Hind Siliguri", 10.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(30, 48),
                AutoSize = true
            };

            txtInstallPath = new TextBox
            {
                Text = targetFolder,
                Location = new Point(30, 80),
                Width = 445,
                Font = new Font("Segoe UI", 9.5f)
            };

            Button btnBrowse = new Button
            {
                Text = "ব্রাউজ (Browse)...",
                Location = new Point(485, 78),
                Size = new Size(120, 28),
                Font = GetBestFont(new[] { "Hind Siliguri", "Segoe UI" }, 9f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnBrowse.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.SelectedPath = txtInstallPath.Text;
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtInstallPath.Text = fbd.SelectedPath;
                        targetFolder = fbd.SelectedPath;
                    }
                }
            };

            chkDesktop = new CheckBox
            {
                Text = "ডেস্কটপ শর্টকাট তৈরি করুন (Create Desktop Shortcut)",
                Font = new Font("Hind Siliguri", 10.5f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(30, 130),
                Size = new Size(500, 24),
                Checked = true
            };

            chkStartMenu = new CheckBox
            {
                Text = "স্টার্ট মেনু শর্টকাট তৈরি করুন (Create Start Menu Shortcut)",
                Font = new Font("Hind Siliguri", 10.5f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(30, 160),
                Size = new Size(500, 24),
                Checked = true
            };

            chkStartup = new CheckBox
            {
                Text = "উইন্ডোজ চালু হলে স্বয়ংক্রিয়ভাবে চালু করুন (Start on Windows Boot)",
                Font = new Font("Hind Siliguri", 10.5f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(30, 190),
                Size = new Size(500, 24),
                Checked = true
            };

            pnlContent.Controls.Add(lblTitle);
            pnlContent.Controls.Add(lblSub);
            pnlContent.Controls.Add(txtInstallPath);
            pnlContent.Controls.Add(btnBrowse);
            pnlContent.Controls.Add(chkDesktop);
            pnlContent.Controls.Add(chkStartMenu);
            pnlContent.Controls.Add(chkStartup);
        }

        private void RenderInstallingStep()
        {
            Label lblTitle = new Label
            {
                Text = "Banglish ইনস্টল হচ্ছে...",
                Font = new Font("Hind Siliguri", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(30, 40),
                AutoSize = true
            };

            lblInstallStatus = new Label
            {
                Text = "প্রয়োজনীয় ফাইলগুলো কপি করা হচ্ছে...",
                Font = new Font("Hind Siliguri", 10.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(30, 80),
                AutoSize = true
            };

            progressBar = new ProgressBar
            {
                Location = new Point(30, 115),
                Size = new Size(570, 24),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            pnlContent.Controls.Add(lblTitle);
            pnlContent.Controls.Add(lblInstallStatus);
            pnlContent.Controls.Add(progressBar);
        }

        private void RenderFinishStep()
        {
            Label lblTitle = new Label
            {
                Text = "ইনস্টলেশন সফলভাবে সম্পন্ন হয়েছে!",
                Font = new Font("Hind Siliguri", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(5, 150, 105),
                Location = new Point(30, 30),
                AutoSize = true
            };

            Label lblDesc = new Label
            {
                Text = "আপনার সিস্টেমে Banglish সফলভাবে কনফিগার করা হয়েছে।\n\n" +
                       "• সফটওয়্যারটি আপনার টাস্কবারের সিস্টেম ট্রেতে যুক্ত থাকবে।\n" +
                       "• যে কোনো অ্যাপ্লিকেশনে টাইপ করার সময় বাংলা চালু/বন্ধ করতে শুধু F12 চাপুন।\n" +
                       "• প্রথমবার ওপেন করার পর একটি স্বাগতম গাইড উইন্ডো প্রদর্শিত হবে।",
                Font = new Font("Hind Siliguri", 11f),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(30, 75),
                Size = new Size(570, 120)
            };

            chkLaunchNow = new CheckBox
            {
                Text = "এখনই Banglish চালু করুন (Launch Banglish now)",
                Font = new Font("Hind Siliguri", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(4, 120, 87),
                Location = new Point(30, 210),
                Size = new Size(450, 28),
                Checked = true
            };

            pnlContent.Controls.Add(lblTitle);
            pnlContent.Controls.Add(lblDesc);
            pnlContent.Controls.Add(chkLaunchNow);
        }

        private void GoNext()
        {
            if (currentStep == 0)
            {
                ShowStep(1);
            }
            else if (currentStep == 1)
            {
                targetFolder = txtInstallPath.Text;
                ShowStep(2);
            }
            else if (currentStep == 3)
            {
                if (chkLaunchNow != null && chkLaunchNow.Checked)
                {
                    string exePath = Path.Combine(targetFolder, "Banglish.exe");
                    if (File.Exists(exePath))
                    {
                        System.Diagnostics.Process.Start(exePath);
                    }
                }
                this.Close();
            }
        }

        private void GoBack()
        {
            if (currentStep == 1)
            {
                ShowStep(0);
            }
        }

        private void StartInstallationProcess()
        {
            var timer = new Timer { Interval = 1200 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                PerformInstallFiles();
                ShowStep(3);
            };
            timer.Start();
        }

        private void PerformInstallFiles()
        {
            try
            {
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                string sourceDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] filesToCopy = new string[] { "Banglish.exe", "words.txt", "Banglish-Logo.png", "app.ico" };

                foreach (var file in filesToCopy)
                {
                    string src = Path.Combine(sourceDir, file);
                    string dst = Path.Combine(targetFolder, file);
                    if (File.Exists(src))
                    {
                        File.Copy(src, dst, true);
                    }
                }

                string installedExe = Path.Combine(targetFolder, "Banglish.exe");

                // Mark first-run flag for the welcome window
                string flagFile = Path.Combine(targetFolder, ".first_run");
                File.WriteAllText(flagFile, "1");

                // Create Desktop Shortcut
                if (chkDesktop != null && chkDesktop.Checked)
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    CreateShortcut(Path.Combine(desktopPath, "Banglish.lnk"), installedExe);
                }

                // Create Start Menu Shortcut
                if (chkStartMenu != null && chkStartMenu.Checked)
                {
                    string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                    string programsDir = Path.Combine(startMenu, "Programs");
                    CreateShortcut(Path.Combine(programsDir, "Banglish.lnk"), installedExe);
                }

                // Setup Startup Registry Key
                if (chkStartup != null && chkStartup.Checked)
                {
                    try
                    {
                        using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                        {
                            if (key != null)
                            {
                                key.SetValue("Banglish", "\"" + installedExe + "\"");
                            }
                        }
                    }
                    catch {}
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ইনস্টলেশন ত্রুটি: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                    shortcut.IconLocation = targetPath + ",0";
                    shortcut.Description = "Banglish — Phonetic Bangla Keyboard for Windows";
                    shortcut.Save();
                }
            }
            catch {}
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupWizard());
        }
    }
}
