using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Threading;

namespace KairosEDA
{
    /// <summary>
    /// Windows 7 Aero styled splash screen
    /// </summary>
    public class SplashScreen : Form
    {
        private ProgressBar progressBar = null!;
        private Label statusLabel = null!;
        private Label versionLabel = null!;
        private PictureBox logoPictureBox = null!;
        private System.Windows.Forms.Timer closeTimer = null!;
        private int tickCount = 0;

        public SplashScreen()
        {
            InitializeComponent();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Ensure the classic theme is applied once the handle exists
            if (this.Handle != IntPtr.Zero)
            {
                Win32Native.DisableAeroGlass(this.Handle);
                Win32Native.ApplyClassicThemeRecursive(this);
            }
        }

        private void InitializeComponent()
        {
            // Form settings - Classic Windows style
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(650, 380);
            this.BackColor = SystemColors.Control;
            this.ControlBox = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.Text = "Kairos EDA";
            
            // Set icon if available
            try
            {
                string iconPath = System.IO.Path.Combine(Application.StartupPath, "app_resources", "icon", "seadrive_icon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }

            // Header panel - Classic Windows blue
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = SystemColors.ActiveCaption
            };
            this.Controls.Add(headerPanel);

            // Logo/Title Label
            Label titleLabel = new Label
            {
                Text = "KAIROS EDA",
                Font = new Font("Tahoma", 24, FontStyle.Bold),
                ForeColor = SystemColors.ActiveCaptionText,
                AutoSize = false,
                Size = new Size(630, 50),
                Location = new Point(10, 5),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(titleLabel);

            // Icon/Logo area
            logoPictureBox = new PictureBox
            {
                Size = new Size(96, 96),
                Location = new Point(30, 100),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            
            // Try to load icon
            try
            {
                string iconPath = System.IO.Path.Combine(Application.StartupPath, "app_resources", "icon", "seadrive_icon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    using (Icon ico = new Icon(iconPath, 96, 96))
                    {
                        logoPictureBox.Image = ico.ToBitmap();
                    }
                }
                else
                {
                    // Draw placeholder
                    Bitmap bmp = new Bitmap(96, 96);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 102, 204)))
                        {
                            g.FillEllipse(brush, 8, 8, 80, 80);
                        }
                        using (Font f = new Font("Tahoma", 30, FontStyle.Bold))
                        using (SolidBrush textBrush = new SolidBrush(Color.White))
                        {
                            StringFormat sf = new StringFormat();
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            g.DrawString("K", f, textBrush, new RectangleF(0, 0, 96, 96), sf);
                        }
                    }
                    logoPictureBox.Image = bmp;
                }
            }
            catch { }
            this.Controls.Add(logoPictureBox);

            // Subtitle
            Label subtitleLabel = new Label
            {
                Text = "Electronic Design Automation Suite",
                Font = new Font("Tahoma", 10f, FontStyle.Bold),
                ForeColor = SystemColors.WindowText,
                AutoSize = false,
                Size = new Size(480, 25),
                Location = new Point(140, 110),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            this.Controls.Add(subtitleLabel);

            // Description
            Label descLabel = new Label
            {
                Text = "RTL to GDSII workflow automation with\nSynthesis • Floorplanning • Placement\nClock Tree Synthesis • Routing • Verification",
                Font = new Font("Tahoma", 8.25f),
                ForeColor = SystemColors.ControlText,
                AutoSize = false,
                Size = new Size(480, 70),
                Location = new Point(140, 140),
                BackColor = Color.Transparent
            };
            this.Controls.Add(descLabel);

            // Progress Bar (Classic Windows style)
            progressBar = new ProgressBar
            {
                Location = new Point(140, 220),
                Size = new Size(480, 23),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 20
            };
            this.Controls.Add(progressBar);

            // Status Label
            statusLabel = new Label
            {
                Text = "Loading application components...",
                Font = new Font("Tahoma", 8.25f),
                ForeColor = SystemColors.ControlText,
                AutoSize = false,
                Size = new Size(480, 25),
                Location = new Point(140, 248),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            this.Controls.Add(statusLabel);

            // Version Label
            versionLabel = new Label
            {
                Text = "Version 1.0.0 • Build 2025.10.03 • .NET 8.0",
                Font = new Font("Tahoma", 8.25f),
                ForeColor = SystemColors.ControlDarkDark,
                AutoSize = false,
                Size = new Size(620, 20),
                Location = new Point(15, 340),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            this.Controls.Add(versionLabel);

            // Border line at bottom
            Panel bottomBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 3,
                BackColor = SystemColors.ActiveCaption
            };
            this.Controls.Add(bottomBorder);

            // Close timer (non-blocking)
            closeTimer = new System.Windows.Forms.Timer
            {
                Interval = 400
            };
            closeTimer.Tick += CloseTimer_Tick;
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            tickCount++;
            
            // Update status messages
            switch (tickCount)
            {
                case 1:
                    statusLabel.Text = "Loading components...";
                    break;
                case 2:
                    statusLabel.Text = "Initializing backend...";
                    break;
                case 3:
                    statusLabel.Text = "Preparing workspace...";
                    break;
                case 4:
                    statusLabel.Text = "Ready!";
                    break;
                case 5:
                    closeTimer.Stop();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    break;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            
            // Start the close timer
            closeTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                closeTimer?.Stop();
                closeTimer?.Dispose();
                logoPictureBox?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
