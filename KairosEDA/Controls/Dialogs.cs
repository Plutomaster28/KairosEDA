using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace KairosEDA.Controls
{
    // Import Win32Native from parent namespace
    using KairosEDA;
    /// <summary>
    /// Dialog for creating a new project
    /// </summary>
    public class NewProjectDialog : Form
    {
        private TextBox projectNameBox = null!;
        private TextBox projectPathBox = null!;
        private Button browseButton = null!;
        private Button okButton = null!;
        private Button cancelButton = null!;

        public string ProjectName => projectNameBox.Text;
        public string ProjectPath => projectPathBox.Text;

        public NewProjectDialog()
        {
            InitializeComponents();
            ApplyClassicTheme();
        }

        private void ApplyClassicTheme()
        {
            if (this.IsHandleCreated)
            {
                Win32Native.ApplyClassicThemeRecursive(this);
            }
            else
            {
                this.HandleCreated += (s, e) => Win32Native.ApplyClassicThemeRecursive(this);
            }
        }

        private void InitializeComponents()
        {
            Text = "New Kairos Project";
            Size = new Size(500, 220);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = SystemColors.Control;
            Font = new Font("Tahoma", 8.25f);

            // Project Name
            var nameLabel = new Label
            {
                Text = "Project Name:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            projectNameBox = new TextBox
            {
                Location = new Point(20, 45),
                Width = 440,
                Text = "MyKairosProject"
            };

            // Project Path
            var pathLabel = new Label
            {
                Text = "Project Location:",
                Location = new Point(20, 80),
                AutoSize = true
            };

            projectPathBox = new TextBox
            {
                Location = new Point(20, 105),
                Width = 360,
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KairosProjects")
            };

            browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(390, 103),
                Width = 70,
                Height = 25
            };
            browseButton.Click += OnBrowse;

            // Buttons
            okButton = new Button
            {
                Text = "Create",
                Location = new Point(280, 145),
                Width = 80,
                DialogResult = DialogResult.OK
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(370, 145),
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Controls.AddRange(new Control[] {
                nameLabel, projectNameBox,
                pathLabel, projectPathBox, browseButton,
                okButton, cancelButton
            });
        }

        private void OnBrowse(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select project location",
                SelectedPath = projectPathBox.Text
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                projectPathBox.Text = dialog.SelectedPath;
            }
        }
    }

    /// <summary>
    /// Dialog for selecting PDK
    /// </summary>
    public class PDKSelectionDialog : Form
    {
        private ComboBox pdkComboBox = null!;
        private Button okButton = null!;
        private Button cancelButton = null!;
        private Label infoLabel = null!;

        public string SelectedPDK => pdkComboBox.Text;

        public PDKSelectionDialog()
        {
            InitializeComponents();
            ApplyClassicTheme();
        }

        private void ApplyClassicTheme()
        {
            if (this.IsHandleCreated)
            {
                Win32Native.ApplyClassicThemeRecursive(this);
            }
            else
            {
                this.HandleCreated += (s, e) => Win32Native.ApplyClassicThemeRecursive(this);
            }
        }

        private void InitializeComponents()
        {
            Text = "Select Process Design Kit (PDK)";
            Size = new Size(450, 250);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = SystemColors.Control;
            Font = new Font("Tahoma", 8.25f);

            var label = new Label
            {
                Text = "Available PDKs:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            pdkComboBox = new ComboBox
            {
                Location = new Point(20, 45),
                Width = 390,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            pdkComboBox.Items.AddRange(new object[] {
                "Sky130 (SkyWater 130nm) - Open Source",
                "GF180 (GlobalFoundries 180nm) - Open Source",
                "ASAP7 (7nm Academic) - Predictive",
                "FreePDK45 (45nm Academic)",
                "Meisei PDK (Future)"
            });
            pdkComboBox.SelectedIndex = 0;
            pdkComboBox.SelectedIndexChanged += OnPDKSelected;

            infoLabel = new Label
            {
                Location = new Point(20, 80),
                Width = 390,
                Height = 100,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5),
                BackColor = Color.FromArgb(250, 250, 250),
                Text = "Sky130 is a mature 130nm open-source PDK from SkyWater.\n\n" +
                       "• Voltage: 1.8V / 3.3V / 5V\n" +
                       "• 5 metal layers\n" +
                       "• Supports digital, analog, and mixed-signal designs"
            };

            okButton = new Button
            {
                Text = "Select",
                Location = new Point(240, 190),
                Width = 80,
                DialogResult = DialogResult.OK
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(330, 190),
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Controls.AddRange(new Control[] {
                label, pdkComboBox, infoLabel,
                okButton, cancelButton
            });
        }

        private void OnPDKSelected(object? sender, EventArgs e)
        {
            switch (pdkComboBox.SelectedIndex)
            {
                case 0: // Sky130
                    infoLabel.Text = "Sky130 is a mature 130nm open-source PDK from SkyWater.\n\n" +
                                     "• Voltage: 1.8V / 3.3V / 5V\n" +
                                     "• 5 metal layers\n" +
                                     "• Supports digital, analog, and mixed-signal designs";
                    break;
                case 1: // GF180
                    infoLabel.Text = "GF180 is a 180nm open-source PDK from GlobalFoundries.\n\n" +
                                     "• Voltage: 1.8V / 3.3V / 5V / 6V\n" +
                                     "• 6 metal layers\n" +
                                     "• Excellent for mixed-signal and RF applications";
                    break;
                case 2: // ASAP7
                    infoLabel.Text = "ASAP7 is a predictive 7nm academic PDK.\n\n" +
                                     "• Voltage: 0.7V\n" +
                                     "• FinFET technology\n" +
                                     "• For research and teaching purposes";
                    break;
                case 3: // FreePDK45
                    infoLabel.Text = "FreePDK45 is an open 45nm academic PDK.\n\n" +
                                     "• Voltage: 1.1V\n" +
                                     "• Based on industry 45nm node\n" +
                                     "• Widely used in academic research";
                    break;
                case 4: // Meisei
                    infoLabel.Text = "Meisei PDK - Coming Soon!\n\n" +
                                     "• Custom process technology\n" +
                                     "• Optimized for Kairos EDA\n" +
                                     "• Contact support for early access";
                    break;
            }
        }
    }

    /// <summary>
    /// Dialog for setting design constraints
    /// </summary>
    public class ConstraintsDialog : Form
    {
        public ConstraintsDialog(Models.Project? project)
        {
            InitializeComponents(project);
            ApplyClassicTheme();
        }

        private void ApplyClassicTheme()
        {
            if (this.IsHandleCreated)
            {
                Win32Native.ApplyClassicThemeRecursive(this);
            }
            else
            {
                this.HandleCreated += (s, e) => Win32Native.ApplyClassicThemeRecursive(this);
            }
        }

        private void InitializeComponents(Models.Project? project)
        {
            Text = "Design Constraints";
            Size = new Size(500, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScroll = true;
            BackColor = SystemColors.Control;
            Font = new Font("Tahoma", 8.25f);

            int y = 20;

            // Clock Period
            AddConstraintField("Clock Period (ns):", project?.Constraints.ClockPeriodNs.ToString() ?? "10.0", ref y);
            
            // Voltage
            AddConstraintField("Supply Voltage (V):", project?.Constraints.VoltageV.ToString() ?? "1.8", ref y);
            
            // Power Budget
            AddConstraintField("Power Budget (mW):", project?.Constraints.PowerBudgetMw.ToString() ?? "100.0", ref y);
            
            // Floorplan Width
            AddConstraintField("Floorplan Width (µm):", project?.Constraints.FloorplanWidthUm.ToString() ?? "1000", ref y);
            
            // Floorplan Height
            AddConstraintField("Floorplan Height (µm):", project?.Constraints.FloorplanHeightUm.ToString() ?? "1000", ref y);
            
            // Utilization
            AddConstraintField("Utilization (0.0-1.0):", project?.Constraints.Utilization.ToString() ?? "0.7", ref y);
            
            // Routing Layers
            AddConstraintField("Routing Layers:", project?.Constraints.RoutingLayers.ToString() ?? "6", ref y);
            
            // Clock Port
            AddConstraintField("Clock Port Name:", project?.Constraints.ClockPort ?? "clk", ref y);

            // Buttons
            var okButton = new Button
            {
                Text = "Apply",
                Location = new Point(280, y + 20),
                Width = 80,
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(370, y + 20),
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Controls.Add(okButton);
            Controls.Add(cancelButton);
        }

        private void AddConstraintField(string labelText, string defaultValue, ref int y)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(20, y),
                Width = 180,
                AutoSize = false
            };

            var textBox = new TextBox
            {
                Location = new Point(210, y),
                Width = 250,
                Text = defaultValue
            };

            Controls.Add(label);
            Controls.Add(textBox);

            y += 35;
        }
    }
}
