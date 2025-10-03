using System;
using System.Drawing;
using System.Windows.Forms;
using KairosEDA.Controls;
using KairosEDA.Models;

namespace KairosEDA
{
    public partial class MainForm : Form
    {
        private ProjectManager projectManager = null!;
        private BackendSimulator backendSimulator = null!;

        // Main UI Components
        private MenuStrip mainMenu = null!;
        private ToolStrip mainToolbar = null!;
        private StatusStrip statusBar = null!;
        private SplitContainer mainSplitter = null!;
        private SplitContainer rightSplitter = null!;

        // Left Panel - Project Explorer
        private TreeView projectExplorer = null!;

        // Center Panel - Workflow Buttons and Progress
    private FlowLayoutPanel workflowPanel = null!;
        private Panel progressPanel = null!;

        // Right Panel - Console and Reports
        private TabControl rightTabs = null!;
        private RichTextBox consoleLog = null!;
        private DataGridView reportGrid = null!;

        public MainForm()
        {
            InitializeComponent();
            InitializeManagers();
            ApplyClassicWindowsStyle();

            this.Load += (s, e) => AdjustWorkflowStageWidths();
        }

        private void InitializeComponent()
        {
            this.Text = "Kairos EDA - Electronic Design Automation Suite";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Load custom icon
            try
            {
                string iconPath = System.IO.Path.Combine(Application.StartupPath, "app_resources", "icon", "seadrive_icon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
                else
                {
                    this.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                this.Icon = SystemIcons.Application;
            }
            
            this.BackColor = SystemColors.Control; // Classic Windows gray
            this.MinimumSize = new Size(1200, 700);
            this.Font = new Font("Tahoma", 8.25f); // Classic Windows font

            // Create menu
            CreateMenu();

            // Create toolbar
            CreateToolbar();

            // Create status bar
            CreateStatusBar();

            // Create main splitter (3-panel layout)
            CreateMainLayout();

            // Create controls
            CreateProjectExplorer();
            CreateWorkflowPanel();
            CreateProgressPanel();
            CreateRightTabs();
        }

        private void InitializeManagers()
        {
            projectManager = new ProjectManager();
            backendSimulator = new BackendSimulator();

            // Wire up events
            backendSimulator.LogReceived += OnLogReceived;
            backendSimulator.ProgressChanged += OnProgressChanged;
            backendSimulator.StageCompleted += OnStageCompleted;
        }

        private void ApplyClassicWindowsStyle()
        {
            // Must be called after window handle is created
            if (!this.IsHandleCreated)
            {
                this.HandleCreated += (s, e) => ApplyClassicWindowsStyle();
                return;
            }

            // Disable Aero glass for classic look
            Win32Native.DisableAeroGlass(this.Handle);

            // Recursively apply Classic Windows theme to all controls
            Win32Native.ApplyClassicThemeRecursive(this);

            // Style the menu and toolbar for classic Windows
            if (mainMenu != null)
            {
                mainMenu.BackColor = SystemColors.Control;
                mainMenu.RenderMode = ToolStripRenderMode.System; // Use system renderer
            }

            if (mainToolbar != null)
            {
                mainToolbar.BackColor = SystemColors.Control;
                mainToolbar.RenderMode = ToolStripRenderMode.System; // Use system renderer
            }

            if (statusBar != null)
            {
                statusBar.BackColor = SystemColors.Control;
                statusBar.RenderMode = ToolStripRenderMode.System;
            }
        }

        private void CreateMenu()
        {
            mainMenu = new MenuStrip
            {
                BackColor = SystemColors.MenuBar,
                RenderMode = ToolStripRenderMode.System
            };

            // File Menu
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("&New Project...", null, OnNewProject);
            fileMenu.DropDownItems.Add("&Open Project...", null, OnOpenProject);
            fileMenu.DropDownItems.Add("&Save Project", null, OnSaveProject);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Recent Projects", null);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("E&xit", null, (s, e) => Application.Exit());

            // Project Menu
            var projectMenu = new ToolStripMenuItem("&Project");
            projectMenu.DropDownItems.Add("Import &Verilog...", null, OnImportVerilog);
            projectMenu.DropDownItems.Add("Select &PDK...", null, OnSelectPDK);
            projectMenu.DropDownItems.Add("Set &Constraints...", null, OnSetConstraints);
            projectMenu.DropDownItems.Add(new ToolStripSeparator());
            projectMenu.DropDownItems.Add("Project &Settings...", null, OnProjectSettings);

            // Flow Menu
            var flowMenu = new ToolStripMenuItem("F&low");
            flowMenu.DropDownItems.Add("Run &Synthesis", null, OnRunSynthesis);
            flowMenu.DropDownItems.Add("Run &Floorplan", null, OnRunFloorplan);
            flowMenu.DropDownItems.Add("Run &Placement", null, OnRunPlacement);
            flowMenu.DropDownItems.Add("Run &Routing", null, OnRunRouting);
            flowMenu.DropDownItems.Add(new ToolStripSeparator());
            flowMenu.DropDownItems.Add("Run &Complete Flow", null, OnRunCompleteFlow);
            flowMenu.DropDownItems.Add(new ToolStripSeparator());
            flowMenu.DropDownItems.Add("&Stop Current Stage", null, OnStopFlow);

            // Tools Menu
            var toolsMenu = new ToolStripMenuItem("&Tools");
            toolsMenu.DropDownItems.Add("&Timing Analysis", null, OnTimingAnalysis);
            toolsMenu.DropDownItems.Add("&Power Analysis", null, OnPowerAnalysis);
            toolsMenu.DropDownItems.Add("&DRC Check", null, OnDRCCheck);
            toolsMenu.DropDownItems.Add("&LVS Check", null, OnLVSCheck);
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add("View &GDS Layout", null, OnViewGDS);

            // View Menu
            var viewMenu = new ToolStripMenuItem("&View");
            var consoleItem = new ToolStripMenuItem("&Console", null, OnShowConsole) { Checked = true };
            var reportsItem = new ToolStripMenuItem("&Reports", null, OnShowReports) { Checked = true };
            var explorerItem = new ToolStripMenuItem("&Project Explorer", null, OnShowProjectExplorer) { Checked = true };
            viewMenu.DropDownItems.Add(consoleItem);
            viewMenu.DropDownItems.Add(reportsItem);
            viewMenu.DropDownItems.Add(explorerItem);
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add("&Beginner Mode", null, OnBeginnerMode);
            viewMenu.DropDownItems.Add("&Expert Mode", null, OnExpertMode);

            // Help Menu
            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add("View &Documentation", null, OnViewDocs);
            helpMenu.DropDownItems.Add("&Tutorials", null, OnTutorials);
            helpMenu.DropDownItems.Add(new ToolStripSeparator());
            helpMenu.DropDownItems.Add("&About Kairos EDA...", null, OnAbout);

            mainMenu.Items.AddRange(new ToolStripItem[] { fileMenu, projectMenu, flowMenu, toolsMenu, viewMenu, helpMenu });
            this.MainMenuStrip = mainMenu;
            this.Controls.Add(mainMenu);
        }

        private void CreateToolbar()
        {
            mainToolbar = new ToolStrip
            {
                ImageScalingSize = new Size(24, 24),
                RenderMode = ToolStripRenderMode.System,
                GripStyle = ToolStripGripStyle.Hidden
            };

            mainToolbar.Items.Add(CreateToolButton("New", "New Project", OnNewProject));
            mainToolbar.Items.Add(CreateToolButton("Open", "Open Project", OnOpenProject));
            mainToolbar.Items.Add(CreateToolButton("Save", "Save Project", OnSaveProject));
            mainToolbar.Items.Add(new ToolStripSeparator());
            mainToolbar.Items.Add(CreateToolButton("Import", "Import Verilog", OnImportVerilog));
            mainToolbar.Items.Add(CreateToolButton("PDK", "Select PDK", OnSelectPDK));
            mainToolbar.Items.Add(new ToolStripSeparator());
            mainToolbar.Items.Add(CreateToolButton("▶ Run", "Run Complete Flow", OnRunCompleteFlow));
            mainToolbar.Items.Add(CreateToolButton("■ Stop", "Stop Flow", OnStopFlow));

            this.Controls.Add(mainToolbar);
        }

        private ToolStripButton CreateToolButton(string text, string tooltip, EventHandler onClick)
        {
            return new ToolStripButton
            {
                Text = text,
                ToolTipText = tooltip,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                AutoSize = true
            };
        }

        private void CreateStatusBar()
        {
            statusBar = new StatusStrip
            {
                RenderMode = ToolStripRenderMode.System
            };

            statusBar.Items.Add(new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft });
            statusBar.Items.Add(new ToolStripStatusLabel("No Project Loaded"));
            statusBar.Items.Add(new ToolStripStatusLabel("Backend: Idle"));

            this.Controls.Add(statusBar);
        }

        private void CreateMainLayout()
        {
            // Main splitter: Left (Project Explorer) | Center+Right
            mainSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 220,
                BorderStyle = BorderStyle.Fixed3D,
                SplitterWidth = 5,
                BackColor = SystemColors.Control,
                IsSplitterFixed = false // Allow user to resize
            };

            // Right splitter: Center (Workflow) | Right (Console/Reports)
            rightSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 380,
                BorderStyle = BorderStyle.Fixed3D,
                SplitterWidth = 5,
                BackColor = SystemColors.Control,
                IsSplitterFixed = false // Allow user to resize
            };

            mainSplitter.Panel2.Controls.Add(rightSplitter);
            this.Controls.Add(mainSplitter);
        }

        private void CreateProjectExplorer()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = SystemColors.Window };
            var label = new Label
            {
                Text = "Project Explorer",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = SystemColors.ActiveCaption,
                ForeColor = SystemColors.ActiveCaptionText,
                Font = new Font("Tahoma", 8.25f, FontStyle.Bold),
                Padding = new Padding(3, 2, 0, 0)
            };

            projectExplorer = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ShowLines = true,
                ShowRootLines = true,
                ImageList = new ImageList()
            };

            // Add sample project structure
            var rootNode = new TreeNode("Kairos Project");
            rootNode.Nodes.Add("RTL Files").Nodes.AddRange(new[] {
                new TreeNode("adder.v"),
                new TreeNode("cpu_core.v"),
                new TreeNode("memory.v")
            });
            rootNode.Nodes.Add("Constraints").Nodes.AddRange(new[] {
                new TreeNode("timing.sdc"),
                new TreeNode("floorplan.tcl")
            });
            rootNode.Nodes.Add("PDK: Sky130");
            rootNode.Nodes.Add("Results").Nodes.AddRange(new[] {
                new TreeNode("Synthesis"),
                new TreeNode("Placement"),
                new TreeNode("Routing"),
                new TreeNode("Reports")
            });
            projectExplorer.Nodes.Add(rootNode);
            rootNode.Expand();

            panel.Controls.Add(projectExplorer);
            panel.Controls.Add(label);
            mainSplitter.Panel1.Controls.Add(panel);
        }

        private void CreateWorkflowPanel()
        {
            // Create vertical splitter for workflow and progress
            var workflowSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 480, // Workflow takes more space
                BorderStyle = BorderStyle.None,
                SplitterWidth = 5,
                BackColor = SystemColors.Control,
                IsSplitterFixed = false,
                Panel1MinSize = 200, // Minimum for workflow
                Panel2MinSize = 120  // Minimum for progress
            };

            // Workflow panel (top part)
            var workflowContainer = new Panel { Dock = DockStyle.Fill, BackColor = SystemColors.Control };
            
            var label = new Label
            {
                Text = "EDA Workflow",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = SystemColors.ActiveCaption,
                ForeColor = SystemColors.ActiveCaptionText,
                Font = new Font("Tahoma", 8.25f, FontStyle.Bold),
                Padding = new Padding(3, 2, 0, 0)
            };

            workflowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(8),
                BackColor = SystemColors.Control,
                AutoSize = false
            };
            
            // Add controls to container FIRST
            workflowContainer.Controls.Add(workflowPanel);
            workflowContainer.Controls.Add(label);
            
            // Resize handler to adjust workflow button widths
            workflowPanel.Resize += (s, e) => AdjustWorkflowStageWidths();
            workflowPanel.HandleCreated += (s, e) => AdjustWorkflowStageWidths();

            // Create workflow stage buttons AFTER panel is added
            CreateWorkflowStage("1. Synthesis", "Converts RTL → Gate-level netlist", OnRunSynthesis, Color.FromArgb(70, 130, 180));
            CreateWorkflowStage("2. Floorplan", "Defines chip area & macro placement", OnRunFloorplan, Color.FromArgb(60, 179, 113));
            CreateWorkflowStage("3. Placement", "Places standard cells", OnRunPlacement, Color.FromArgb(255, 140, 0));
            CreateWorkflowStage("4. Clock Tree Synthesis", "Builds clock distribution network", OnRunCTS, Color.FromArgb(186, 85, 211));
            CreateWorkflowStage("5. Routing", "Connects all cells with metal layers", OnRunRouting, Color.FromArgb(220, 20, 60));
            CreateWorkflowStage("6. Verification (DRC/LVS)", "Checks design rules & layout vs schematic", OnRunVerification, Color.FromArgb(255, 215, 0));
            AdjustWorkflowStageWidths();
            workflowSplitter.Panel1.Controls.Add(workflowContainer);

            // Add progress panel at the bottom in splitter Panel2
            CreateProgressPanel();
            workflowSplitter.Panel2.Controls.Add(progressPanel);
            
            rightSplitter.Panel1.Controls.Add(workflowSplitter);
        }

        private void CreateWorkflowStage(string title, string description, EventHandler onClick, Color accentColor)
        {
            var stagePanel = new WorkflowStageControl(title, description, onClick, accentColor)
            {
                Margin = new Padding(0, 0, 0, 6)
            };

            stagePanel.MinimumSize = new Size(320, stagePanel.Height);
            stagePanel.MaximumSize = new Size(int.MaxValue, stagePanel.Height);
            stagePanel.Width = Math.Max(320, workflowPanel.ClientSize.Width - 20);

            workflowPanel.Controls.Add(stagePanel);
        }

        private void AdjustWorkflowStageWidths()
        {
            if (workflowPanel == null)
            {
                return;
            }

            var targetWidth = Math.Max(320, workflowPanel.ClientSize.Width - 20);

            foreach (Control ctrl in workflowPanel.Controls)
            {
                if (ctrl is WorkflowStageControl stage)
                {
                    stage.Width = targetWidth;
                }
            }
        }

        private void CreateProgressPanel()
        {
            progressPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                BackColor = SystemColors.Control
            };

            var label = new Label
            {
                Text = "Progress && Statistics",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = SystemColors.ActiveCaption,
                ForeColor = SystemColors.ActiveCaptionText,
                Font = new Font("Tahoma", 8.25f, FontStyle.Bold),
                Padding = new Padding(3, 2, 0, 0)
            };

            var statsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                BackColor = SystemColors.Control
            };

            // Add progress bars and stats labels (will be populated dynamically)
            var overallProgress = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 21,
                Style = ProgressBarStyle.Continuous,
                Value = 0
            };

            var statsLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Design Statistics:\n\n" +
                       "Gates: 0\n" +
                       "Flip-Flops: 0\n" +
                       "Area: 0.00 mm²\n" +
                       "Utilization: 0%\n\n" +
                       "Timing:\n" +
                       "WNS: N/A\n" +
                       "TNS: N/A\n\n" +
                       "Power: 0.00 mW",
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = SystemColors.Window,
                Padding = new Padding(8),
                Font = new Font("Tahoma", 8.25f)
            };

            statsPanel.Controls.Add(statsLabel);
            statsPanel.Controls.Add(overallProgress);

            progressPanel.Controls.Add(statsPanel);
            progressPanel.Controls.Add(label);
        }

        private void CreateRightTabs()
        {
            rightTabs = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Console Tab
            var consoleTab = new TabPage("Console");
            consoleLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
                Font = new Font("Courier New", 8.25f),
                ReadOnly = true,
                BorderStyle = BorderStyle.Fixed3D
            };
            consoleTab.Controls.Add(consoleLog);
            rightTabs.TabPages.Add(consoleTab);

            // Reports Tab
            var reportsTab = new TabPage("Reports");
            reportGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.None,
                BackgroundColor = SystemColors.Window
            };
            
            // Setup report columns
            reportGrid.Columns.Add("Stage", "Stage");
            reportGrid.Columns.Add("Metric", "Metric");
            reportGrid.Columns.Add("Value", "Value");
            reportGrid.Columns.Add("Status", "Status");
            
            reportsTab.Controls.Add(reportGrid);
            rightTabs.TabPages.Add(reportsTab);

            // Timing Tab
            var timingTab = new TabPage("Timing");
            var timingView = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };
            timingTab.Controls.Add(timingView);
            rightTabs.TabPages.Add(timingTab);

            // Violations Tab
            var violationsTab = new TabPage("Violations");
            var violationsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.None
            };
            violationsGrid.Columns.Add("Type", "Type");
            violationsGrid.Columns.Add("Severity", "Severity");
            violationsGrid.Columns.Add("Location", "Location");
            violationsGrid.Columns.Add("Description", "Description");
            violationsTab.Controls.Add(violationsGrid);
            rightTabs.TabPages.Add(violationsTab);

            rightSplitter.Panel2.Controls.Add(rightTabs);
        }

        #region Event Handlers

        private void OnNewProject(object? sender, EventArgs e)
        {
            var dialog = new NewProjectDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                projectManager.CreateNewProject(dialog.ProjectName, dialog.ProjectPath);
                LogMessage("Created new project: " + dialog.ProjectName, LogLevel.Info);
                UpdateStatusBar("Project: " + dialog.ProjectName);
            }
        }

        private void OnOpenProject(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Kairos Project (*.kproj)|*.kproj|All Files (*.*)|*.*",
                Title = "Open Kairos Project"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                projectManager.LoadProject(dialog.FileName);
                LogMessage("Loaded project: " + dialog.FileName, LogLevel.Info);
            }
        }

        private void OnSaveProject(object? sender, EventArgs e)
        {
            projectManager.SaveProject();
            LogMessage("Project saved successfully", LogLevel.Success);
        }

        private void OnImportVerilog(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Verilog Files (*.v;*.sv)|*.v;*.sv|All Files (*.*)|*.*",
                Title = "Import Verilog RTL",
                Multiselect = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in dialog.FileNames)
                {
                    projectManager.AddRTLFile(file);
                    LogMessage($"Imported RTL: {System.IO.Path.GetFileName(file)}", LogLevel.Info);
                }
            }
        }

        private void OnSelectPDK(object? sender, EventArgs e)
        {
            var dialog = new PDKSelectionDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                projectManager.SetPDK(dialog.SelectedPDK);
                LogMessage($"PDK selected: {dialog.SelectedPDK}", LogLevel.Info);
            }
        }

        private void OnSetConstraints(object? sender, EventArgs e)
        {
            var dialog = new ConstraintsDialog(projectManager.CurrentProject);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                LogMessage("Constraints updated", LogLevel.Info);
            }
        }

        private void OnProjectSettings(object? sender, EventArgs e)
        {
            MessageBox.Show("Project Settings dialog would open here", "Settings", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnRunSynthesis(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Synthesis ===", LogLevel.Stage);
            backendSimulator.RunStage("synthesis", projectManager.CurrentProject);
        }

        private void OnRunFloorplan(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Floorplan ===", LogLevel.Stage);
            backendSimulator.RunStage("floorplan", projectManager.CurrentProject);
        }

        private void OnRunPlacement(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Placement ===", LogLevel.Stage);
            backendSimulator.RunStage("placement", projectManager.CurrentProject);
        }

        private void OnRunCTS(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Clock Tree Synthesis ===", LogLevel.Stage);
            backendSimulator.RunStage("cts", projectManager.CurrentProject);
        }

        private void OnRunRouting(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Routing ===", LogLevel.Stage);
            backendSimulator.RunStage("routing", projectManager.CurrentProject);
        }

        private void OnRunVerification(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Verification (DRC/LVS) ===", LogLevel.Stage);
            backendSimulator.RunStage("verification", projectManager.CurrentProject);
        }

        private void OnRunCompleteFlow(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Complete EDA Flow ===", LogLevel.Stage);
            backendSimulator.RunCompleteFlow(projectManager.CurrentProject);
        }

        private void OnStopFlow(object? sender, EventArgs e)
        {
            backendSimulator.Stop();
            LogMessage("Flow stopped by user", LogLevel.Warning);
        }

        private void OnTimingAnalysis(object? sender, EventArgs e)
        {
            LogMessage("Running timing analysis...", LogLevel.Info);
            backendSimulator.RunAnalysis("timing", projectManager.CurrentProject);
        }

        private void OnPowerAnalysis(object? sender, EventArgs e)
        {
            LogMessage("Running power analysis...", LogLevel.Info);
            backendSimulator.RunAnalysis("power", projectManager.CurrentProject);
        }

        private void OnDRCCheck(object? sender, EventArgs e)
        {
            LogMessage("Running DRC check...", LogLevel.Info);
            backendSimulator.RunAnalysis("drc", projectManager.CurrentProject);
        }

        private void OnLVSCheck(object? sender, EventArgs e)
        {
            LogMessage("Running LVS check...", LogLevel.Info);
            backendSimulator.RunAnalysis("lvs", projectManager.CurrentProject);
        }

        private void OnViewGDS(object? sender, EventArgs e)
        {
            MessageBox.Show("Would launch KLayout or internal GDS viewer here", 
                "GDS Viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnShowConsole(object? sender, EventArgs e) { }
        private void OnShowReports(object? sender, EventArgs e) { }
        private void OnShowProjectExplorer(object? sender, EventArgs e) { }

        private void OnBeginnerMode(object? sender, EventArgs e)
        {
            MessageBox.Show("Switched to Beginner Mode\n\nPre-configured settings will be used.", 
                "Mode Change", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnExpertMode(object? sender, EventArgs e)
        {
            MessageBox.Show("Switched to Expert Mode\n\nFull control over all parameters and TCL injection enabled.", 
                "Mode Change", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnViewDocs(object? sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/kairoseda/docs",
                UseShellExecute = true
            });
        }

        private void OnTutorials(object? sender, EventArgs e) { }

        private void OnAbout(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Kairos EDA v1.0\n\n" +
                "Electronic Design Automation Suite\n\n" +
                "A modern, user-friendly interface for RTL-to-GDSII flows.\n" +
                "Powered by Yosys, OpenROAD, Magic, and Netgen.\n\n" +
                "© 2025 Kairos EDA Project",
                "About Kairos EDA",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnLogReceived(object? sender, LogEventArgs e)
        {
            if (consoleLog.InvokeRequired)
            {
                consoleLog.Invoke(new Action(() => LogMessage(e.Message, e.Level)));
            }
            else
            {
                LogMessage(e.Message, e.Level);
            }
        }

        private void OnProgressChanged(object? sender, ProgressEventArgs e)
        {
            if (statusBar.InvokeRequired)
            {
                statusBar.Invoke(new Action(() => UpdateProgress(e)));
            }
            else
            {
                UpdateProgress(e);
            }
        }

        private void OnStageCompleted(object? sender, StageCompletedEventArgs e)
        {
            if (reportGrid.InvokeRequired)
            {
                reportGrid.Invoke(new Action(() => AddReportRow(e)));
            }
            else
            {
                AddReportRow(e);
            }
        }

        #endregion

        #region Helper Methods

        private void LogMessage(string message, LogLevel level)
        {
            Color color = level switch
            {
                LogLevel.Error => Color.FromArgb(255, 100, 100),
                LogLevel.Warning => Color.FromArgb(255, 200, 100),
                LogLevel.Success => Color.FromArgb(100, 255, 150),
                LogLevel.Stage => Color.FromArgb(100, 200, 255),
                _ => Color.FromArgb(220, 220, 220)
            };

            consoleLog.SelectionStart = consoleLog.TextLength;
            consoleLog.SelectionLength = 0;
            consoleLog.SelectionColor = color;
            consoleLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            consoleLog.SelectionColor = consoleLog.ForeColor;
            consoleLog.ScrollToCaret();
        }

        private void UpdateProgress(ProgressEventArgs e)
        {
            statusBar.Items[2].Text = $"Backend: {e.StageName} - {e.Progress}%";
        }

        private void UpdateStatusBar(string message)
        {
            statusBar.Items[1].Text = message;
        }

        private void AddReportRow(StageCompletedEventArgs e)
        {
            reportGrid.Rows.Add(e.StageName, e.Metric, e.Value, e.Status);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            LogMessage("===================================", LogLevel.Info);
            LogMessage("  Kairos EDA v1.0", LogLevel.Stage);
            LogMessage("  Electronic Design Automation Suite", LogLevel.Info);
            LogMessage("===================================", LogLevel.Info);
            LogMessage("", LogLevel.Info);
            LogMessage("Ready. Create a new project or open an existing one.", LogLevel.Success);
        }

        #endregion
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Success,
        Stage
    }
}
