using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using KairosEDA.Models;

namespace KairosEDA.Controls
{
    /// <summary>
    /// Dialog for configuring EDA toolchain paths and settings
    /// </summary>
    public class ToolchainSetupDialog : Form
    {
        private EdaToolchain toolchain;
        private TextBox txtYosysPath;
        private TextBox txtOpenRoadPath;
        private TextBox txtOpenStaPath;
        private TextBox txtMagicPath;
        private TextBox txtNetgenPath;
        private TextBox txtPdkPath;
        private CheckBox chkUseDocker;
        private TextBox txtDockerImage;
        private Label lblStatus;
        private Button btnDetect;
        private Button btnSave;
        private Button btnCancel;

        public ToolchainSetupDialog(EdaToolchain toolchain)
        {
            this.toolchain = toolchain;
            InitializeComponents();
            LoadCurrentConfig();
        }

        private void InitializeComponents()
        {
            this.Text = "EDA Toolchain Setup";
            this.Size = new Size(600, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Tahoma", 8.25f);

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            int y = 10;
            const int labelWidth = 120;
            const int textBoxWidth = 350;
            const int lineHeight = 30;
            const int spacing = 8;

            // Title
            var lblTitle = new Label
            {
                Text = "Configure EDA Tools",
                Location = new Point(15, y),
                Size = new Size(550, 20),
                Font = new Font("Tahoma", 10f, FontStyle.Bold)
            };
            panel.Controls.Add(lblTitle);
            y += lineHeight;

            var lblSubtitle = new Label
            {
                Text = "Set paths to Yosys, OpenROAD, OpenSTA, Magic, and Netgen. Leave blank to use PATH.",
                Location = new Point(15, y),
                Size = new Size(550, 30),
                ForeColor = SystemColors.GrayText
            };
            panel.Controls.Add(lblSubtitle);
            y += lineHeight + spacing;

            // Yosys Path
            panel.Controls.Add(CreateLabel("Yosys:", 15, y, labelWidth));
            txtYosysPath = CreateTextBox(15 + labelWidth, y, textBoxWidth);
            panel.Controls.Add(txtYosysPath);
            panel.Controls.Add(CreateBrowseButton(15 + labelWidth + textBoxWidth + 5, y, txtYosysPath));
            y += lineHeight;

            // OpenROAD Path
            panel.Controls.Add(CreateLabel("OpenROAD:", 15, y, labelWidth));
            txtOpenRoadPath = CreateTextBox(15 + labelWidth, y, textBoxWidth);
            panel.Controls.Add(txtOpenRoadPath);
            panel.Controls.Add(CreateBrowseButton(15 + labelWidth + textBoxWidth + 5, y, txtOpenRoadPath));
            y += lineHeight;

            // OpenSTA Path
            panel.Controls.Add(CreateLabel("OpenSTA:", 15, y, labelWidth));
            txtOpenStaPath = CreateTextBox(15 + labelWidth, y, textBoxWidth);
            panel.Controls.Add(txtOpenStaPath);
            panel.Controls.Add(CreateBrowseButton(15 + labelWidth + textBoxWidth + 5, y, txtOpenStaPath));
            y += lineHeight;

            // Magic Path
            panel.Controls.Add(CreateLabel("Magic:", 15, y, labelWidth));
            txtMagicPath = CreateTextBox(15 + labelWidth, y, textBoxWidth);
            panel.Controls.Add(txtMagicPath);
            panel.Controls.Add(CreateBrowseButton(15 + labelWidth + textBoxWidth + 5, y, txtMagicPath));
            y += lineHeight;

            // Netgen Path
            panel.Controls.Add(CreateLabel("Netgen:", 15, y, labelWidth));
            txtNetgenPath = CreateTextBox(15 + labelWidth, y, textBoxWidth);
            panel.Controls.Add(txtNetgenPath);
            panel.Controls.Add(CreateBrowseButton(15 + labelWidth + textBoxWidth + 5, y, txtNetgenPath));
            y += lineHeight;

            // PDK Path
            panel.Controls.Add(CreateLabel("PDK Directory:", 15, y, labelWidth));
            txtPdkPath = CreateTextBox(15 + labelWidth, y, textBoxWidth);
            panel.Controls.Add(txtPdkPath);
            var btnBrowsePdk = CreateBrowseButton(15 + labelWidth + textBoxWidth + 5, y, txtPdkPath);
            btnBrowsePdk.Click -= (s, e) => BrowseForFile(txtPdkPath);
            btnBrowsePdk.Click += (s, e) => BrowseForFolder(txtPdkPath);
            panel.Controls.Add(btnBrowsePdk);
            y += lineHeight + spacing;

            // Docker option
            chkUseDocker = new CheckBox
            {
                Text = "Use Docker (Recommended for Windows)",
                Location = new Point(15, y),
                Size = new Size(300, 20),
                FlatStyle = FlatStyle.System
            };
            chkUseDocker.CheckedChanged += (s, e) =>
            {
                txtDockerImage.Enabled = chkUseDocker.Checked;
                txtYosysPath.Enabled = !chkUseDocker.Checked;
                txtOpenRoadPath.Enabled = !chkUseDocker.Checked;
                txtOpenStaPath.Enabled = !chkUseDocker.Checked;
                txtMagicPath.Enabled = !chkUseDocker.Checked;
                txtNetgenPath.Enabled = !chkUseDocker.Checked;
            };
            panel.Controls.Add(chkUseDocker);
            y += lineHeight;

            panel.Controls.Add(CreateLabel("Docker Image:", 15, y, labelWidth));
            txtDockerImage = CreateTextBox(15 + labelWidth, y, textBoxWidth);
            panel.Controls.Add(txtDockerImage);
            y += lineHeight + spacing;

            // Status label
            lblStatus = new Label
            {
                Location = new Point(15, y),
                Size = new Size(550, 40),
                Text = "Click 'Auto-Detect' to find tools in your PATH.",
                ForeColor = SystemColors.GrayText,
                BorderStyle = BorderStyle.Fixed3D,
                Padding = new Padding(5),
                BackColor = SystemColors.Info
            };
            panel.Controls.Add(lblStatus);
            y += 50;

            // Buttons
            btnDetect = new Button
            {
                Text = "Auto-Detect",
                Location = new Point(15, y),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.System
            };
            btnDetect.Click += async (s, e) => await AutoDetectTools();
            panel.Controls.Add(btnDetect);

            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(350, y),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.System,
                DialogResult = DialogResult.OK
            };
            btnSave.Click += (s, e) => SaveConfig();
            panel.Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(460, y),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.System,
                DialogResult = DialogResult.Cancel
            };
            panel.Controls.Add(btnCancel);

            this.Controls.Add(panel);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private Label CreateLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y + 3),
                Size = new Size(width, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private TextBox CreateTextBox(int x, int y, int width)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 22),
                BorderStyle = BorderStyle.Fixed3D
            };
        }

        private Button CreateBrowseButton(int x, int y, TextBox targetTextBox)
        {
            var btn = new Button
            {
                Text = "...",
                Location = new Point(x, y - 1),
                Size = new Size(28, 24),
                FlatStyle = FlatStyle.System
            };
            btn.Click += (s, e) => BrowseForFile(targetTextBox);
            return btn;
        }

        private void BrowseForFile(TextBox textBox)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Tool Executable"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox.Text = dialog.FileName;
            }
        }

        private void BrowseForFolder(TextBox textBox)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select PDK Directory"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox.Text = dialog.SelectedPath;
            }
        }

        private void LoadCurrentConfig()
        {
            txtYosysPath.Text = toolchain.Config.YosysPath;
            txtOpenRoadPath.Text = toolchain.Config.OpenRoadPath;
            txtOpenStaPath.Text = toolchain.Config.OpenStaPath;
            txtMagicPath.Text = toolchain.Config.MagicPath;
            txtNetgenPath.Text = toolchain.Config.NetgenPath;
            txtPdkPath.Text = toolchain.Config.PdkPath;
            chkUseDocker.Checked = toolchain.Config.UseDocker;
            txtDockerImage.Text = toolchain.Config.DockerImage;
            txtDockerImage.Enabled = chkUseDocker.Checked;
        }

        private async Task AutoDetectTools()
        {
            btnDetect.Enabled = false;
            lblStatus.Text = "Detecting tools...";
            lblStatus.ForeColor = SystemColors.ControlText;

            var result = await toolchain.DetectTools();

            lblStatus.Text = $"Found {result.FoundCount}/5 tools: " +
                $"Yosys={result.YosysFound}, " +
                $"OpenROAD={result.OpenRoadFound}, " +
                $"OpenSTA={result.OpenStaFound}, " +
                $"Magic={result.MagicFound}, " +
                $"Netgen={result.NetgenFound}";

            if (result.AllToolsFound)
            {
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                lblStatus.ForeColor = Color.DarkOrange;
            }

            // Update text boxes
            txtYosysPath.Text = toolchain.Config.YosysPath;
            txtOpenRoadPath.Text = toolchain.Config.OpenRoadPath;
            txtOpenStaPath.Text = toolchain.Config.OpenStaPath;
            txtMagicPath.Text = toolchain.Config.MagicPath;
            txtNetgenPath.Text = toolchain.Config.NetgenPath;

            btnDetect.Enabled = true;
        }

        private void SaveConfig()
        {
            toolchain.Config.YosysPath = txtYosysPath.Text;
            toolchain.Config.OpenRoadPath = txtOpenRoadPath.Text;
            toolchain.Config.OpenStaPath = txtOpenStaPath.Text;
            toolchain.Config.MagicPath = txtMagicPath.Text;
            toolchain.Config.NetgenPath = txtNetgenPath.Text;
            toolchain.Config.PdkPath = txtPdkPath.Text;
            toolchain.Config.UseDocker = chkUseDocker.Checked;
            toolchain.Config.DockerImage = txtDockerImage.Text;

            toolchain.SaveConfig();
        }
    }
}
