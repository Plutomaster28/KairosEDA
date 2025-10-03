using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KairosEDA.Controls
{
    /// <summary>
    /// Custom control for displaying workflow stages with Classic Windows styling
    /// </summary>
    public class WorkflowStageControl : Panel
    {
        private string title;
        private string description;
        private Button runButton;
        private ProgressBar progressBar;
        private Label statusLabel;
        private Color accentColor;
        private bool isRunning = false;

        public WorkflowStageControl(string title, string description, EventHandler onRun, Color accentColor)
        {
            this.title = title;
            this.description = description;
            this.accentColor = accentColor;

            Height = 70;
            BorderStyle = BorderStyle.Fixed3D;
            BackColor = SystemColors.Control;
            Padding = new Padding(5);
            Margin = new Padding(0, 0, 0, 5);

            // Title label
            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Tahoma", 8.25f, FontStyle.Bold),
                ForeColor = SystemColors.WindowText,
                AutoSize = true,
                Location = new Point(8, 8)
            };

            // Description label
            var descLabel = new Label
            {
                Text = description,
                Font = new Font("Tahoma", 8.25f),
                ForeColor = SystemColors.ControlDarkDark,
                AutoSize = true,
                Location = new Point(8, 24)
            };

            // Run button
            runButton = new Button
            {
                Text = "Run",
                Width = 75,
                Height = 23,
                Location = new Point(8, 42),
                FlatStyle = FlatStyle.System,
                Font = new Font("Tahoma", 8.25f)
            };
            runButton.Click += (s, e) =>
            {
                onRun?.Invoke(this, e);
                ShowProgress();
            };

            // Progress bar (initially hidden)
            progressBar = new ProgressBar
            {
                Width = Width - 100,
                Height = 21,
                Location = new Point(90, 43),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Visible = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            
            // Handle resize to adjust progress bar
            this.Resize += (s, e) => progressBar.Width = Width - 100;

            // Status label
            statusLabel = new Label
            {
                Text = "Ready",
                AutoSize = true,
                Location = new Point(90, 46),
                Font = new Font("Tahoma", 8.25f),
                ForeColor = SystemColors.ControlDarkDark
            };

            Controls.Add(titleLabel);
            Controls.Add(descLabel);
            Controls.Add(runButton);
            Controls.Add(progressBar);
            Controls.Add(statusLabel);
        }

        public void ShowProgress()
        {
            isRunning = true;
            progressBar.Visible = true;
            statusLabel.Visible = false;
            runButton.Enabled = false;
        }

        public void HideProgress()
        {
            isRunning = false;
            progressBar.Visible = false;
            statusLabel.Visible = true;
            statusLabel.Text = "Completed";
            statusLabel.ForeColor = Color.Green;
            runButton.Enabled = true;
            runButton.Text = "Re-Run";
        }

        public void SetStatus(string status, bool success)
        {
            statusLabel.Text = status;
            statusLabel.ForeColor = success ? Color.Green : Color.Red;
            statusLabel.Visible = true;
            progressBar.Visible = false;
        }
    }
}
