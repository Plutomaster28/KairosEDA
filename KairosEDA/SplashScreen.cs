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
            // Form settings - Classic Windows 95 style
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(650, 400);
            this.BackColor = Color.FromArgb(192, 192, 192); // #C0C0C0
            this.ControlBox = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.Text = "Kairos EDA";
            
            // Set icon from embedded resource
            this.Icon = Helpers.ResourceHelper.GetApplicationIcon();

            // Header panel - Windows 95 blue gradient style
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(0, 0, 128) // Navy blue
            };
            headerPanel.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    headerPanel.ClientRectangle,
                    Color.FromArgb(0, 0, 128),
                    Color.FromArgb(16, 132, 208),
                    LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, headerPanel.ClientRectangle);
                }
            };
            this.Controls.Add(headerPanel);

            // Logo/Title Label
            Label titleLabel = new Label
            {
                Text = "KAIROS EDA",
                Font = new Font("Arial", 32, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(630, 45),
                Location = new Point(10, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(titleLabel);
            
            // Subtitle in header
            Label subtitleHeader = new Label
            {
                Text = "Electronic Design Automation Suite",
                Font = new Font("MS Sans Serif", 10f),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(630, 20),
                Location = new Point(10, 55),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(subtitleHeader);

            // Icon/Logo area with sunken border
            Panel iconPanel = new Panel
            {
                Location = new Point(30, 100),
                Size = new Size(120, 120),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            iconPanel.Paint += (s, e) =>
            {
                // Draw sunken 3D border
                ControlPaint.DrawBorder3D(e.Graphics, iconPanel.ClientRectangle, Border3DStyle.Sunken);
            };
            this.Controls.Add(iconPanel);
            
            logoPictureBox = new PictureBox
            {
                Size = new Size(100, 100),
                Location = new Point(10, 10),
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            
            // Try to load icon from embedded resource
            Bitmap? iconBitmap = Helpers.ResourceHelper.GetApplicationIconAsBitmap(96);
            if (iconBitmap != null)
            {
                logoPictureBox.Image = iconBitmap;
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
            iconPanel.Controls.Add(logoPictureBox);

            // Info panel with sunken border
            Panel infoPanel = new Panel
            {
                Location = new Point(170, 100),
                Size = new Size(450, 120),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            infoPanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder3D(e.Graphics, infoPanel.ClientRectangle, Border3DStyle.Sunken);
            };
            this.Controls.Add(infoPanel);

            // Description inside info panel
            Label descLabel = new Label
            {
                Text = "RTL to GDSII Design Flow\n\n" +
                       "• Full RTL to GDSII automated flow\n" +
                       "• Synthesis, Floorplan, Place & Route\n" +
                       "• Timing and Power Analysis\n" +
                       "• DRC/LVS Verification\n" +
                       "• Open Source PDK Support",
                Font = new Font("MS Sans Serif", 9f),
                ForeColor = Color.Black,
                AutoSize = false,
                Size = new Size(430, 100),
                Location = new Point(10, 10),
                BackColor = Color.White
            };
            infoPanel.Controls.Add(descLabel);

            // Status Label
            statusLabel = new Label
            {
                Text = "Starting up...",
                Font = new Font("MS Sans Serif", 9f),
                ForeColor = Color.Black,
                AutoSize = false,
                Size = new Size(590, 20),
                Location = new Point(30, 240),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            this.Controls.Add(statusLabel);

            // Progress Bar with sunken border panel
            Panel progressPanel = new Panel
            {
                Location = new Point(30, 265),
                Size = new Size(590, 28),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            progressPanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder3D(e.Graphics, progressPanel.ClientRectangle, Border3DStyle.Sunken);
            };
            this.Controls.Add(progressPanel);
            
            progressBar = new ProgressBar
            {
                Location = new Point(3, 3),
                Size = new Size(584, 22),
                Style = ProgressBarStyle.Continuous,
                Value = 0,
                Maximum = 100
            };
            progressPanel.Controls.Add(progressBar);

            // Version Label
            versionLabel = new Label
            {
                Text = "Version 1.0.0",
                Font = new Font("MS Sans Serif", 8.25f),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(30, 310),
                BackColor = Color.Transparent
            };
            this.Controls.Add(versionLabel);
            
            // Copyright
            Label copyrightLabel = new Label
            {
                Text = "© 2025 Kairos EDA Project",
                Font = new Font("MS Sans Serif", 8.25f),
                ForeColor = Color.Black,
                AutoSize = false,
                Size = new Size(200, 20),
                Location = new Point(420, 310),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };
            this.Controls.Add(copyrightLabel);

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
            int progress = tickCount * 10;
            
            if (progress <= 100)
            {
                progressBar.Value = Math.Min(progress, 100);
            }
            
            // Update status messages with realistic loading steps
            switch (tickCount)
            {
                case 1:
                    statusLabel.Text = "Initializing application...";
                    break;
                case 2:
                    statusLabel.Text = "Loading core modules...";
                    break;
                case 3:
                    statusLabel.Text = "Starting EDA backend...";
                    break;
                case 4:
                    statusLabel.Text = "Loading project manager...";
                    break;
                case 5:
                    statusLabel.Text = "Initializing workflow engine...";
                    break;
                case 6:
                    statusLabel.Text = "Loading toolchain interfaces...";
                    break;
                case 7:
                    statusLabel.Text = "Checking PDK configurations...";
                    break;
                case 8:
                    statusLabel.Text = "Preparing user interface...";
                    break;
                case 9:
                    statusLabel.Text = "Finalizing startup...";
                    break;
                case 10:
                    statusLabel.Text = "Ready!";
                    break;
                case 11:
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
