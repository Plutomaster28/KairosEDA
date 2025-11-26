using System;
using System.Drawing;
using System.Windows.Forms;
using KairosEDA.Controls;
using KairosEDA.Models;
using LogLevel = KairosEDA.Models.LogLevel;

namespace KairosEDA
{
    public partial class MainForm : Form
    {
        private ProjectManager projectManager = null!;
        private EdaBackend edaBackend = null!;

        // Main UI Components
        private MenuStrip mainMenu = null!;
        private ToolStrip mainToolbar = null!;
        private StatusStrip statusBar = null!;
        private SplitContainer mainSplitter = null!;
        private SplitContainer rightSplitter = null!;

        // Left Panel - Project Explorer
        private TreeView projectExplorer = null!;
    private Panel projectExplorerContainer = null!;
    private bool isProjectExplorerPanning;
    private bool projectExplorerPanActivated;
    private Point projectExplorerPanLastPoint;
    private int projectExplorerHorizontalRemainder;
    private int projectExplorerVerticalRemainder;
    private bool suppressProjectExplorerSelection;

        // Center Panel - Workflow Buttons and Progress
        private FlowLayoutPanel workflowPanel = null!;
        private SplitContainer workflowSplitter = null!;
        private Panel progressPanel = null!;

        // Right Panel - Console and Reports
        private TabControl rightTabs = null!;
        private RichTextBox consoleLog = null!;
        private DataGridView reportGrid = null!;

        public MainForm()
        {
            try
            {
                InitializeManagers();
                InitializeComponent();
                ApplyClassicWindowsStyle();

                this.Load += (s, e) => 
                {
                    AdjustWorkflowStageWidths();
                    SetSplitterDistancesSafely();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing MainForm:\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}", 
                    "Initialization Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                throw;
            }
        }
        
        private void SetSplitterDistancesSafely()
        {
            try
            {
                // Wait for layout to complete before setting splitter distances
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // Set main splitter (left panel = project explorer)
                        if (mainSplitter != null && mainSplitter.Width > 0)
                        {
                            int desiredDistance = 250;
                            int maxDistance = mainSplitter.Width - mainSplitter.Panel2MinSize - mainSplitter.SplitterWidth;
                            int minDistance = mainSplitter.Panel1MinSize;
                            
                            if (maxDistance > minDistance)
                            {
                                mainSplitter.SplitterDistance = Math.Max(minDistance, Math.Min(desiredDistance, maxDistance));
                            }
                        }
                        
                        // Set right splitter (center = workflow, right = console)
                        if (rightSplitter != null && rightSplitter.Width > 0)
                        {
                            int desiredDistance = 650;
                            int maxDistance = rightSplitter.Width - rightSplitter.Panel2MinSize - rightSplitter.SplitterWidth;
                            int minDistance = rightSplitter.Panel1MinSize;
                            
                            if (maxDistance > minDistance)
                            {
                                rightSplitter.SplitterDistance = Math.Max(minDistance, Math.Min(desiredDistance, maxDistance));
                            }
                        }
                        
                        // Set workflow splitter (top = workflow buttons, bottom = progress panel)
                        if (workflowSplitter != null && workflowSplitter.Height > 0)
                        {
                            int desiredDistance = 480;
                            int maxDistance = workflowSplitter.Height - workflowSplitter.Panel2MinSize - workflowSplitter.SplitterWidth;
                            int minDistance = workflowSplitter.Panel1MinSize;
                            
                            if (maxDistance > minDistance)
                            {
                                workflowSplitter.SplitterDistance = Math.Max(minDistance, Math.Min(desiredDistance, maxDistance));
                            }
                        }
                    }
                    catch
                    {
                        // If setting splitter distances fails, just use defaults
                    }
                }));
            }
            catch
            {
                // Ignore errors - splitters will use default 50/50 split
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "Kairos EDA - Electronic Design Automation Suite";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            // Use DPI auto-scaling so controls and client area scale correctly on high-DPI displays
            this.AutoScaleMode = AutoScaleMode.Dpi;
            
            // Load custom icon from embedded resource
            this.Icon = Helpers.ResourceHelper.GetApplicationIcon();
            
            this.BackColor = SystemColors.Control; // Classic Windows gray
            this.MinimumSize = new Size(1200, 700);
            this.Font = new Font("Tahoma", 8.25f); // Classic Windows font
            this.Padding = new Padding(0); // Let controls dock naturally to edges
            this.ClientSize = new Size(1400, 900); // Use ClientSize for interior dimensions

            // CRITICAL: Add controls in correct docking order!
            // 1. Menu (Dock.Top) - goes first
            CreateMenu();

            // 2. Toolbar (Dock.Top) - goes under menu
            CreateToolbar();

            // 3. Status bar (Dock.Bottom) - goes to bottom
            CreateStatusBar();

            // 4. Main content (Dock.Fill) - fills remaining space
            CreateMainLayout();

            // 5. Populate panels with content
            CreateProjectExplorer();
            CreateWorkflowPanel();
            CreateRightTabs();
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void InitializeManagers()
        {
            projectManager = new ProjectManager();
            edaBackend = new EdaBackend();

            // Wire up events
            edaBackend.LogReceived += OnLogReceived;
            edaBackend.ProgressChanged += OnProgressChanged;
            edaBackend.StageCompleted += OnStageCompleted;
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
                Dock = DockStyle.Top,
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
            toolsMenu.DropDownItems.Add("&Toolchain Setup...", null, OnToolchainSetup);
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
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
            helpMenu.DropDownItems.Add("&Documentation", null, OnDocumentation);
            helpMenu.DropDownItems.Add("&Tutorials (YouTube)", null, OnTutorials);
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
                Dock = DockStyle.Top,
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
            var button = new ToolStripButton
            {
                Text = text,
                ToolTipText = tooltip,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                AutoSize = true
            };
            button.Click += onClick; // Wire up the click handler!
            return button;
        }

        private void CreateStatusBar()
        {
            statusBar = new StatusStrip
            {
                Dock = DockStyle.Bottom,
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
                BorderStyle = BorderStyle.None, // No border - prevents content from being cut off
                SplitterWidth = 5,
                BackColor = SystemColors.ControlDark,
                IsSplitterFixed = false, // Allow user to resize
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };

            // Right splitter: Center (Workflow) | Right (Console/Reports)
            rightSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None, // No border - prevents content from being cut off
                SplitterWidth = 5,
                BackColor = SystemColors.ControlDark,
                IsSplitterFixed = false, // Allow user to resize
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };

            // Add nested structure while layout is suspended
            mainSplitter.Panel2.Controls.Add(rightSplitter);
            this.Controls.Add(mainSplitter);
        }

        private void CreateProjectExplorer()
        {
            projectExplorerContainer = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = SystemColors.Control,
                Padding = new Padding(0) // No inner padding
            };
            
            var label = new Label
            {
                Text = "Project Explorer",
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = SystemColors.ActiveCaption,
                ForeColor = SystemColors.ActiveCaptionText,
                Font = new Font("Tahoma", 8.25f, FontStyle.Bold),
                Padding = new Padding(8, 6, 0, 6)
            };

            projectExplorer = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ShowLines = true,
                ShowRootLines = true,
                ImageList = new ImageList()
            };

            projectExplorer.MouseDown += ProjectExplorer_MouseDown;
            projectExplorer.MouseMove += ProjectExplorer_MouseMove;
            projectExplorer.MouseUp += ProjectExplorer_MouseUp;
            projectExplorer.MouseLeave += ProjectExplorer_MouseLeave;
            projectExplorer.BeforeSelect += ProjectExplorer_BeforeSelect;

            Win32Native.ApplyTreeViewTheme(projectExplorer);

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

            var treeHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Window,
                Padding = new Padding(5, 5, 5, 5) // Add padding to keep tree away from borders
            };
            treeHost.Controls.Add(projectExplorer);

            projectExplorerContainer.Controls.Add(treeHost);
            projectExplorerContainer.Controls.Add(label);
            mainSplitter.Panel1.Controls.Add(projectExplorerContainer);
        }

        private void CreateWorkflowPanel()
        {
            // Create vertical splitter for workflow and progress
            workflowSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BorderStyle = BorderStyle.None,
                SplitterWidth = 5,
                BackColor = SystemColors.Control,
                IsSplitterFixed = false,
                Panel1MinSize = 200, // Minimum for workflow
                Panel2MinSize = 120  // Minimum for progress
            };

            // Workflow panel (top part)
            var workflowContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Control,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            workflowContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            workflowContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            workflowContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var label = new Label
            {
                Text = "EDA Workflow",
                Dock = DockStyle.Fill,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = SystemColors.ActiveCaption,
                ForeColor = SystemColors.ActiveCaptionText,
                Font = new Font("Tahoma", 8.25f, FontStyle.Bold),
                Padding = new Padding(8, 6, 0, 6),
                Margin = new Padding(0)
            };

            workflowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(12, 12, 12, 12), // More padding to keep content away from borders
                BackColor = SystemColors.Control,
                AutoSize = false,
                Margin = new Padding(0),
                AutoScrollMargin = new Size(0, 10),
                TabStop = true
            };

            workflowPanel.MouseEnter += FocusWorkflowPanel;
            workflowPanel.MouseWheel += WorkflowPanel_MouseWheel;
            workflowPanel.PreviewKeyDown += WorkflowPanel_PreviewKeyDown;
            workflowPanel.KeyDown += WorkflowPanel_KeyDown;
            workflowPanel.Resize += (s, e) => AdjustWorkflowStageWidths();
            workflowPanel.HandleCreated += (s, e) => AdjustWorkflowStageWidths();

            workflowContainer.Controls.Add(label, 0, 0);
            workflowContainer.Controls.Add(workflowPanel, 0, 1);

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
                Margin = new Padding(0, 0, 0, 8)
            };

            stagePanel.MinimumSize = new Size(300, stagePanel.Height);
            stagePanel.MaximumSize = new Size(int.MaxValue, stagePanel.Height);
            stagePanel.Width = Math.Max(300, workflowPanel.ClientSize.Width - 24); // Account for padding

            workflowPanel.Controls.Add(stagePanel);
            AttachWorkflowScrollHandlers(stagePanel);
        }

        private void AdjustWorkflowStageWidths()
        {
            if (workflowPanel == null)
            {
                return;
            }

            var targetWidth = Math.Max(300, workflowPanel.ClientSize.Width - 24); // Account for padding

            foreach (Control ctrl in workflowPanel.Controls)
            {
                if (ctrl is WorkflowStageControl stage)
                {
                    stage.Width = targetWidth;
                }
            }
        }

        private void FocusWorkflowPanel(object? sender, EventArgs e)
        {
            if (workflowPanel != null && workflowPanel.CanFocus)
            {
                workflowPanel.Focus();
            }
        }

        private void WorkflowPanel_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Up ||
                e.KeyCode == Keys.Down ||
                e.KeyCode == Keys.PageUp ||
                e.KeyCode == Keys.PageDown ||
                e.KeyCode == Keys.Home ||
                e.KeyCode == Keys.End)
            {
                e.IsInputKey = true;
            }
        }

        private void WorkflowPanel_KeyDown(object? sender, KeyEventArgs e)
        {
            if (workflowPanel == null)
            {
                return;
            }

            const int arrowStep = 60;
            var viewportStep = Math.Max(arrowStep, workflowPanel.ClientSize.Height - 40);

            switch (e.KeyCode)
            {
                case Keys.Down:
                    ScrollWorkflowBy(arrowStep);
                    e.Handled = true;
                    break;
                case Keys.Up:
                    ScrollWorkflowBy(-arrowStep);
                    e.Handled = true;
                    break;
                case Keys.PageDown:
                    ScrollWorkflowBy(viewportStep);
                    e.Handled = true;
                    break;
                case Keys.PageUp:
                    ScrollWorkflowBy(-viewportStep);
                    e.Handled = true;
                    break;
                case Keys.Home:
                    ScrollWorkflowToTop();
                    e.Handled = true;
                    break;
                case Keys.End:
                    ScrollWorkflowToBottom();
                    e.Handled = true;
                    break;
            }
        }

        private void WorkflowPanel_MouseWheel(object? sender, MouseEventArgs e)
        {
            HandleWorkflowMouseWheel(e);
        }

        private void WorkflowChild_MouseWheel(object? sender, MouseEventArgs e)
        {
            HandleWorkflowMouseWheel(e);
        }

        private void HandleWorkflowMouseWheel(MouseEventArgs e)
        {
            if (workflowPanel == null)
            {
                return;
            }

            var linesPerNotch = SystemInformation.MouseWheelScrollLines;
            var stepPerNotch = linesPerNotch > 0 ? linesPerNotch * 20 : 80;
            var delta = e.Delta > 0 ? -stepPerNotch : stepPerNotch;

            ScrollWorkflowBy(delta);

            if (e is HandledMouseEventArgs handled)
            {
                handled.Handled = true;
            }
        }

        private void AttachWorkflowScrollHandlers(Control control)
        {
            control.MouseEnter += FocusWorkflowPanel;
            control.MouseWheel += WorkflowChild_MouseWheel;

            foreach (Control child in control.Controls)
            {
                AttachWorkflowScrollHandlers(child);
            }
        }

        private void ScrollWorkflowBy(int delta)
        {
            if (workflowPanel == null)
            {
                return;
            }

            var scroll = workflowPanel.VerticalScroll;

            if (scroll.Maximum <= scroll.Minimum && !scroll.Visible)
            {
                return;
            }

            var maxValue = Math.Max(scroll.Minimum, scroll.Maximum - scroll.LargeChange + 1);
            var target = Math.Max(scroll.Minimum, Math.Min(scroll.Value + delta, maxValue));

            if (target != scroll.Value)
            {
                scroll.Value = target;
                workflowPanel.PerformLayout();
            }
        }

        private void ScrollWorkflowToTop()
        {
            if (workflowPanel == null)
            {
                return;
            }

            var scroll = workflowPanel.VerticalScroll;

            if (scroll.Maximum <= scroll.Minimum && !scroll.Visible)
            {
                return;
            }

            if (scroll.Value != scroll.Minimum)
            {
                scroll.Value = scroll.Minimum;
                workflowPanel.PerformLayout();
            }
        }

        private void ScrollWorkflowToBottom()
        {
            if (workflowPanel == null)
            {
                return;
            }

            var scroll = workflowPanel.VerticalScroll;

            if (scroll.Maximum <= scroll.Minimum && !scroll.Visible)
            {
                return;
            }

            var maxValue = Math.Max(scroll.Minimum, scroll.Maximum - scroll.LargeChange + 1);

            if (scroll.Value != maxValue)
            {
                scroll.Value = maxValue;
                workflowPanel.PerformLayout();
            }
        }

        private void ProjectExplorer_MouseDown(object? sender, MouseEventArgs e)
        {
            if (projectExplorer == null || e.Button != MouseButtons.Left)
            {
                return;
            }

            projectExplorer.Focus();
            projectExplorer.Capture = true;
            isProjectExplorerPanning = true;
            projectExplorerPanActivated = false;
            suppressProjectExplorerSelection = false;
            projectExplorerPanLastPoint = e.Location;
            projectExplorerHorizontalRemainder = 0;
            projectExplorerVerticalRemainder = 0;
        }

        private void ProjectExplorer_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!isProjectExplorerPanning)
            {
                return;
            }

            var deltaX = e.Location.X - projectExplorerPanLastPoint.X;
            var deltaY = e.Location.Y - projectExplorerPanLastPoint.Y;

            if (!projectExplorerPanActivated)
            {
                if (Math.Abs(deltaX) < 3 && Math.Abs(deltaY) < 3)
                {
                    return;
                }

                projectExplorerPanActivated = true;
                projectExplorer.Cursor = Cursors.SizeAll;
            }

            projectExplorerPanLastPoint = e.Location;
            ScrollProjectExplorer(deltaX, deltaY);
        }

        private void ProjectExplorer_MouseUp(object? sender, MouseEventArgs e)
        {
            StopProjectExplorerPan(projectExplorerPanActivated);
        }

        private void ProjectExplorer_MouseLeave(object? sender, EventArgs e)
        {
            if (isProjectExplorerPanning && (Control.MouseButtons & MouseButtons.Left) == 0)
            {
                StopProjectExplorerPan(projectExplorerPanActivated);
            }
        }

        private void ProjectExplorer_BeforeSelect(object? sender, TreeViewCancelEventArgs e)
        {
            if (suppressProjectExplorerSelection)
            {
                e.Cancel = true;
                suppressProjectExplorerSelection = false;
            }
        }

        private void StopProjectExplorerPan(bool cancelSelection)
        {
            if (!isProjectExplorerPanning && !projectExplorerPanActivated)
            {
                return;
            }

            isProjectExplorerPanning = false;
            projectExplorerPanActivated = false;
            projectExplorerHorizontalRemainder = 0;
            projectExplorerVerticalRemainder = 0;

            if (projectExplorer != null)
            {
                projectExplorer.Capture = false;
                projectExplorer.Cursor = Cursors.Default;
            }

            if (cancelSelection)
            {
                suppressProjectExplorerSelection = true;
            }
        }

        private void ScrollProjectExplorer(int deltaX, int deltaY)
        {
            if (projectExplorer == null)
            {
                return;
            }

            projectExplorerHorizontalRemainder += deltaX;
            projectExplorerVerticalRemainder += deltaY;

            const int horizontalStepSize = 32;
            var verticalStepSize = Math.Max(projectExplorer.ItemHeight, 18);

            while (projectExplorerHorizontalRemainder >= horizontalStepSize)
            {
                Win32Native.ScrollTreeView(projectExplorer, 1, 0);
                projectExplorerHorizontalRemainder -= horizontalStepSize;
            }

            while (projectExplorerHorizontalRemainder <= -horizontalStepSize)
            {
                Win32Native.ScrollTreeView(projectExplorer, -1, 0);
                projectExplorerHorizontalRemainder += horizontalStepSize;
            }

            while (projectExplorerVerticalRemainder >= verticalStepSize)
            {
                MoveProjectExplorerByVisibleOffset(1);
                projectExplorerVerticalRemainder -= verticalStepSize;
            }

            while (projectExplorerVerticalRemainder <= -verticalStepSize)
            {
                MoveProjectExplorerByVisibleOffset(-1);
                projectExplorerVerticalRemainder += verticalStepSize;
            }
        }

        private void MoveProjectExplorerByVisibleOffset(int offset)
        {
            if (projectExplorer?.TopNode == null || offset == 0)
            {
                return;
            }

            var currentTop = projectExplorer.TopNode;
            var lastValid = currentTop;
            var steps = Math.Abs(offset);

            while (steps > 0 && lastValid != null)
            {
                var next = offset > 0 ? lastValid.NextVisibleNode : lastValid.PrevVisibleNode;
                if (next == null)
                {
                    break;
                }

                lastValid = next;
                steps--;
            }

            if (lastValid != null && lastValid != currentTop)
            {
                projectExplorer.BeginUpdate();
                projectExplorer.TopNode = lastValid;
                projectExplorer.EndUpdate();
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
            edaBackend.RunStage("synthesis", projectManager.CurrentProject);
        }

        private void OnRunFloorplan(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Floorplan ===", LogLevel.Stage);
            edaBackend.RunStage("floorplan", projectManager.CurrentProject);
        }

        private void OnRunPlacement(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Placement ===", LogLevel.Stage);
            edaBackend.RunStage("placement", projectManager.CurrentProject);
        }

        private void OnRunCTS(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Clock Tree Synthesis ===", LogLevel.Stage);
            edaBackend.RunStage("cts", projectManager.CurrentProject);
        }

        private void OnRunRouting(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Routing ===", LogLevel.Stage);
            edaBackend.RunStage("routing", projectManager.CurrentProject);
        }

        private void OnRunVerification(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Verification (DRC/LVS) ===", LogLevel.Stage);
            edaBackend.RunStage("verification", projectManager.CurrentProject);
        }

        private void OnRunCompleteFlow(object? sender, EventArgs e)
        {
            LogMessage("=== Starting Complete EDA Flow ===", LogLevel.Stage);
            edaBackend.RunCompleteFlow(projectManager.CurrentProject);
        }

        private void OnStopFlow(object? sender, EventArgs e)
        {
            edaBackend.Stop();
            LogMessage("Flow stopped by user", LogLevel.Warning);
        }

        private void OnTimingAnalysis(object? sender, EventArgs e)
        {
            LogMessage("Running timing analysis...", LogLevel.Info);
            edaBackend.RunAnalysis("timing", projectManager.CurrentProject);
        }

        private void OnPowerAnalysis(object? sender, EventArgs e)
        {
            LogMessage("Running power analysis...", LogLevel.Info);
            edaBackend.RunAnalysis("power", projectManager.CurrentProject);
        }

        private void OnDRCCheck(object? sender, EventArgs e)
        {
            LogMessage("Running DRC check...", LogLevel.Info);
            edaBackend.RunAnalysis("drc", projectManager.CurrentProject);
        }

        private void OnLVSCheck(object? sender, EventArgs e)
        {
            LogMessage("Running LVS check...", LogLevel.Info);
            edaBackend.RunAnalysis("lvs", projectManager.CurrentProject);
        }

        private void OnToolchainSetup(object? sender, EventArgs e)
        {
            using var dialog = new ToolchainSetupDialog(edaBackend.Toolchain);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                LogMessage("Toolchain configuration saved", LogLevel.Success);
            }
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

        private void OnDocumentation(object? sender, EventArgs e)
        {
            var docViewer = new Controls.DocumentationViewer();
            docViewer.Show();
        }

        private void OnTutorials(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.youtube.com/@miyamii_lmao",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open browser:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

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

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            LogMessage("===================================", LogLevel.Info);
            LogMessage("  Kairos EDA v1.0", LogLevel.Stage);
            LogMessage("  Electronic Design Automation Suite", LogLevel.Info);
            LogMessage("===================================", LogLevel.Info);
            LogMessage("", LogLevel.Info);
            LogMessage("Integrated EDA toolchain: Yosys + OpenROAD + Magic + Netgen", LogLevel.Info);
            LogMessage("", LogLevel.Info);

            // Auto-detect tools on startup
            LogMessage("Detecting EDA tools...", LogLevel.Info);
            var detection = await edaBackend.DetectTools();
            
            if (detection.AllToolsFound)
            {
                LogMessage($"✓ All tools detected! Found: Yosys, OpenROAD, Magic, Netgen", LogLevel.Success);
            }
            else
            {
                LogMessage($"⚠ {detection.FoundCount}/4 tools found", LogLevel.Warning);
                if (!detection.YosysFound) LogMessage("  - Yosys: Not found", LogLevel.Warning);
                if (!detection.OpenRoadFound) LogMessage("  - OpenROAD: Not found", LogLevel.Warning);
                if (!detection.MagicFound) LogMessage("  - Magic: Not found", LogLevel.Warning);
                if (!detection.NetgenFound) LogMessage("  - Netgen: Not found", LogLevel.Warning);
                LogMessage("", LogLevel.Info);
                LogMessage("→ Go to Tools → Toolchain Setup to configure paths", LogLevel.Info);
                LogMessage("→ Or use Docker for pre-configured toolchain", LogLevel.Info);
            }

            LogMessage("", LogLevel.Info);
            LogMessage("Ready! Create a new project or open an existing one.", LogLevel.Success);
        }

        #endregion
    }

}
