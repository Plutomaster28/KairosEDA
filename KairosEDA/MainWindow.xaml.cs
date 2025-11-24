using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using KairosEDA.Models;

namespace KairosEDA
{
    public partial class MainWindow : Window
    {
        private ProjectManager? projectManager;
        private BackendSimulator? backend;
        private Dictionary<string, TextBox> openEditors = new Dictionary<string, TextBox>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeManagers();
            SetupProjectTreeEvents();
            SetupKeyboardShortcuts();
            Loaded += MainWindow_Loaded;
        }

        private void SetupKeyboardShortcuts()
        {
            // Ctrl+N - New File
            var newFileCmd = new System.Windows.Input.RoutedCommand();
            newFileCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(
                System.Windows.Input.Key.N, System.Windows.Input.ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newFileCmd, (s, e) => OnNewFile(s, e)));

            // Ctrl+O - Open File
            var openFileCmd = new System.Windows.Input.RoutedCommand();
            openFileCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(
                System.Windows.Input.Key.O, System.Windows.Input.ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(openFileCmd, (s, e) => OnOpenFile(s, e)));

            // Ctrl+S - Save File
            var saveCmd = new System.Windows.Input.RoutedCommand();
            saveCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(
                System.Windows.Input.Key.S, System.Windows.Input.ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(saveCmd, (s, e) => OnSaveCurrentFile(s, e)));

            // Ctrl+W - Close File
            var closeCmd = new System.Windows.Input.RoutedCommand();
            closeCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(
                System.Windows.Input.Key.W, System.Windows.Input.ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(closeCmd, (s, e) => OnCloseCurrentFile(s, e)));
        }

        private void InitializeManagers()
        {
            try
            {
                projectManager = new ProjectManager();
                backend = new BackendSimulator();
                backend.LogReceived += OnBackendLog;
                backend.ProgressChanged += OnBackendProgress;
                backend.StageCompleted += OnStageCompleted;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing managers:\n\n{ex.Message}", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            statusLabel.Text = "Ready";
            LogToConsole("Kairos EDA initialized successfully.");
            LogToConsole("Create or open a project to get started.");
        }

        private void SetupProjectTreeEvents()
        {
            projectTree.MouseDoubleClick += ProjectTree_MouseDoubleClick;
        }

        private void ProjectTree_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (projectTree.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is string filePath)
            {
                OpenFileInEditor(filePath);
            }
        }

        private void OpenFileInEditor(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"File not found:\n{filePath}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check if file is already open
            string fileName = Path.GetFileName(filePath);
            foreach (TabItem tab in editorTabs.Items)
            {
                if (tab.Tag as string == filePath)
                {
                    editorTabs.SelectedItem = tab;
                    LogToConsole($"Switched to {fileName}");
                    return;
                }
            }

            try
            {
                // Read file content
                string content = File.ReadAllText(filePath);

                // Create new tab
                var newTab = new TabItem
                {
                    Header = CreateTabHeader(fileName, filePath),
                    Tag = filePath
                };

                // Create editor
                var editor = new TextBox
                {
                    Text = content,
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    Padding = new Thickness(5),
                    BorderThickness = new Thickness(0)
                };

                // Wrap in sunken border
                var border = new Border
                {
                    Margin = new Thickness(4),
                    BorderThickness = new Thickness(2),
                    BorderBrush = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(128, 128, 128), 0),
                            new GradientStop(Colors.White, 1)
                        }
                    },
                    Background = new SolidColorBrush(Colors.White),
                    Child = editor
                };

                newTab.Content = border;
                openEditors[filePath] = editor;

                editorTabs.Items.Add(newTab);
                editorTabs.SelectedItem = newTab;

                LogToConsole($"Opened {fileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open file:\n\n{ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                LogToConsole($"Error opening {fileName}: {ex.Message}");
            }
        }

        private StackPanel CreateTabHeader(string fileName, string filePath)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var fileIcon = new TextBlock
            {
                Text = "📄 ",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };

            var label = new TextBlock
            {
                Text = fileName,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var closeButton = new Button
            {
                Content = "×",
                Width = 16,
                Height = 16,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(0),
                Margin = new Thickness(4, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(192, 192, 192)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            closeButton.Click += (s, e) =>
            {
                CloseEditorTab(filePath);
                e.Handled = true; // Prevent tab selection
            };

            panel.Children.Add(fileIcon);
            panel.Children.Add(label);
            panel.Children.Add(closeButton);

            return panel;
        }

        private void CloseEditorTab(string filePath)
        {
            // Find and remove the tab
            TabItem? tabToRemove = null;
            foreach (TabItem tab in editorTabs.Items)
            {
                if (tab.Tag as string == filePath)
                {
                    tabToRemove = tab;
                    break;
                }
            }

            if (tabToRemove != null)
            {
                // Check if modified (simple check - could be enhanced)
                if (openEditors.ContainsKey(filePath))
                {
                    var editor = openEditors[filePath];
                    var originalContent = File.ReadAllText(filePath);
                    if (editor.Text != originalContent)
                    {
                        var result = MessageBox.Show(
                            $"Save changes to {Path.GetFileName(filePath)}?",
                            "Unsaved Changes",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            SaveFile(filePath);
                        }
                        else if (result == MessageBoxResult.Cancel)
                        {
                            return; // Don't close
                        }
                    }
                    openEditors.Remove(filePath);
                }

                editorTabs.Items.Remove(tabToRemove);
                LogToConsole($"Closed {Path.GetFileName(filePath)}");
            }
        }

        private void SaveFile(string filePath)
        {
            if (openEditors.ContainsKey(filePath))
            {
                try
                {
                    File.WriteAllText(filePath, openEditors[filePath].Text);
                    LogToConsole($"Saved {Path.GetFileName(filePath)}");
                    MessageBox.Show($"File saved successfully!", "Save File",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save file:\n\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RunWorkflowStage(string stage)
        {
            if (backend == null || projectManager?.CurrentProject == null) return;

            statusLabel.Text = $"Running {stage}...";
            buildStatus.Text = $"Running {stage}...";
            progressBar.Value = 0;
            buildOutput.Text = $"Starting {stage} workflow stage...\n";

            // Execute workflow stage
            backend.RunStage(stage, projectManager.CurrentProject);
        }

        // File Menu Event Handlers
        private void OnNewFile(object sender, RoutedEventArgs e)
        {
            // Create a new untitled file
            string fileName = $"Untitled-{editorTabs.Items.Count}.v";
            string tempPath = Path.Combine(Path.GetTempPath(), fileName);

            // Create empty file content
            string template = $"// {fileName}\n// Created: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n" +
                            "module new_module (\n" +
                            "    input wire clk,\n" +
                            "    input wire rst,\n" +
                            "    // Add your ports here\n" +
                            ");\n\n" +
                            "// Add your logic here\n\n" +
                            "endmodule\n";

            // Create new tab
            var newTab = new TabItem
            {
                Header = CreateTabHeader(fileName, tempPath),
                Tag = tempPath
            };

            // Create editor
            var editor = new TextBox
            {
                Text = template,
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                Padding = new Thickness(5),
                BorderThickness = new Thickness(0)
            };

            var border = new Border
            {
                Margin = new Thickness(4),
                BorderThickness = new Thickness(2),
                BorderBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(128, 128, 128), 0),
                        new GradientStop(Colors.White, 1)
                    }
                },
                Background = new SolidColorBrush(Colors.White),
                Child = editor
            };

            newTab.Content = border;
            openEditors[tempPath] = editor;

            editorTabs.Items.Add(newTab);
            editorTabs.SelectedItem = newTab;

            LogToConsole($"Created new file: {fileName}");
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open File",
                Filter = "Verilog Files (*.v;*.sv;*.vh)|*.v;*.sv;*.vh|" +
                        "HDL Files (*.vhd;*.vhdl)|*.vhd;*.vhdl|" +
                        "All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    OpenFileInEditor(file);
                }
            }
        }

        private void OnSaveCurrentFile(object sender, RoutedEventArgs e)
        {
            if (editorTabs.SelectedItem is TabItem selectedTab)
            {
                string? filePath = selectedTab.Tag as string;
                if (filePath != null)
                {
                    // Check if it's a temp file (needs Save As)
                    if (filePath.StartsWith(Path.GetTempPath()))
                    {
                        OnSaveFileAs(sender, e);
                    }
                    else
                    {
                        SaveFile(filePath);
                    }
                }
            }
            else
            {
                LogToConsole("No file open to save");
            }
        }

        private void OnSaveFileAs(object sender, RoutedEventArgs e)
        {
            if (editorTabs.SelectedItem is TabItem selectedTab)
            {
                string? oldPath = selectedTab.Tag as string;
                if (oldPath != null && openEditors.ContainsKey(oldPath))
                {
                    var dialog = new SaveFileDialog
                    {
                        Title = "Save File As",
                        Filter = "Verilog Files (*.v)|*.v|" +
                                "SystemVerilog Files (*.sv)|*.sv|" +
                                "Verilog Header (*.vh)|*.vh|" +
                                "All Files (*.*)|*.*",
                        FileName = Path.GetFileName(oldPath)
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        string newPath = dialog.FileName;
                        try
                        {
                            // Save content to new location
                            File.WriteAllText(newPath, openEditors[oldPath].Text);

                            // Update tracking
                            var editor = openEditors[oldPath];
                            openEditors.Remove(oldPath);
                            openEditors[newPath] = editor;

                            // Update tab
                            selectedTab.Tag = newPath;
                            selectedTab.Header = CreateTabHeader(Path.GetFileName(newPath), newPath);

                            LogToConsole($"Saved as {Path.GetFileName(newPath)}");
                            MessageBox.Show($"File saved successfully!", "Save File As",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to save file:\n\n{ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void OnCloseCurrentFile(object sender, RoutedEventArgs e)
        {
            if (editorTabs.SelectedItem is TabItem selectedTab)
            {
                string? filePath = selectedTab.Tag as string;
                if (filePath != null)
                {
                    CloseEditorTab(filePath);
                }
            }
        }

        // Project Menu Event Handlers
        private void OnNewProject(object sender, RoutedEventArgs e)
        {
            LogToConsole("Creating new project...");
            
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select or create a folder for your new project",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string projectPath = dialog.SelectedPath;
                string projectName = System.IO.Path.GetFileName(projectPath);

                projectManager?.CreateNewProject(projectName, projectPath);
                
                LogToConsole($"Created new project: {projectName}");
                LogToConsole($"Project location: {projectPath}");
                statusLabel.Text = $"Project: {projectName}";
                
                RefreshProjectExplorer();
                MessageBox.Show($"Project '{projectName}' created successfully!", "New Project", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnOpenProject(object sender, RoutedEventArgs e)
        {
            LogToConsole("Opening project...");
            
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select project directory to open",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string projectPath = dialog.SelectedPath;
                string projectName = System.IO.Path.GetFileName(projectPath);

                try
                {
                    // Load existing .kproj file if it exists, otherwise create new project from directory
                    string kprojFile = System.IO.Path.Combine(projectPath, projectName + ".kproj");
                    
                    if (File.Exists(kprojFile))
                    {
                        projectManager?.LoadProject(kprojFile);
                        LogToConsole($"Loaded project: {projectName}");
                    }
                    else
                    {
                        // Create project from directory
                        projectManager?.CreateNewProject(projectName, projectPath);
                        LogToConsole($"Created project from directory: {projectName}");
                    }
                    
                    statusLabel.Text = $"Project: {projectName}";
                    RefreshProjectExplorer();
                    
                    MessageBox.Show($"Project '{projectName}' opened successfully!", "Open Project", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open project:\n\n{ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    LogToConsole($"Error opening project: {ex.Message}");
                }
            }
        }

        private void OnSaveProject(object sender, RoutedEventArgs e)
        {
            if (projectManager?.CurrentProject == null)
            {
                MessageBox.Show("No project to save. Please create or open a project first.", "Save Project",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                projectManager.SaveProject();
                string projectFile = System.IO.Path.Combine(projectManager.CurrentProject.Path, 
                    projectManager.CurrentProject.Name + ".kproj");
                
                LogToConsole($"Project saved: {projectFile}");
                statusLabel.Text = $"Project: {projectManager.CurrentProject.Name} (Saved)";
                
                MessageBox.Show("Project saved successfully!", "Save Project", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save project:\n\n{ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                LogToConsole($"Error saving project: {ex.Message}");
            }
        }

        private void OnImportVerilog(object sender, RoutedEventArgs e)
        {
            if (projectManager?.CurrentProject == null)
            {
                MessageBox.Show("Please create or open a project first.", "No Project",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Import Verilog/SystemVerilog Files",
                Filter = "Verilog Files (*.v;*.sv;*.vh)|*.v;*.sv;*.vh|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    projectManager.AddRTLFile(file);
                    LogToConsole($"Added RTL file: {System.IO.Path.GetFileName(file)}");
                }
                
                RefreshProjectExplorer();
                MessageBox.Show($"Imported {dialog.FileNames.Length} file(s) successfully!", "Import Verilog",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnSelectPDK(object sender, RoutedEventArgs e)
        {
            if (projectManager?.CurrentProject == null)
            {
                MessageBox.Show("Please create or open a project first.", "No Project",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select PDK (Process Design Kit) directory\n\nCommon PDKs: Sky130, GF180, ASAP7",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string pdkPath = dialog.SelectedPath;
                string pdkName = System.IO.Path.GetFileName(pdkPath);
                
                projectManager.SetPDK(pdkPath);
                LogToConsole($"PDK selected: {pdkName}");
                LogToConsole($"PDK path: {pdkPath}");
                
                MessageBox.Show($"PDK '{pdkName}' selected successfully!", "Select PDK",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnSetConstraints(object sender, RoutedEventArgs e)
        {
            if (projectManager?.CurrentProject == null)
            {
                MessageBox.Show("Please create or open a project first.", "No Project",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var constraints = projectManager.CurrentProject.Constraints;
            string constraintInfo = $"Current Design Constraints:\n\n" +
                $"Clock Period: {constraints.ClockPeriodNs} ns ({1000.0 / constraints.ClockPeriodNs:F1} MHz)\n" +
                $"Supply Voltage: {constraints.VoltageV} V\n" +
                $"Power Budget: {constraints.PowerBudgetMw} mW\n" +
                $"Die Size: {constraints.FloorplanWidthUm} × {constraints.FloorplanHeightUm} µm\n" +
                $"Utilization: {constraints.Utilization * 100:F1}%\n" +
                $"Routing Layers: {constraints.RoutingLayers}\n" +
                $"Clock Port: {constraints.ClockPort}\n\n" +
                "[Constraint editor UI to be implemented]";

            MessageBox.Show(constraintInfo, "Design Constraints", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            LogToConsole("Displayed design constraints");
        }

        private void OnProjectSettings(object sender, RoutedEventArgs e)
        {
            if (projectManager?.CurrentProject == null)
            {
                MessageBox.Show("Please create or open a project first.", "No Project",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var proj = projectManager.CurrentProject;
            string settingsInfo = $"Project Settings\n\n" +
                $"Name: {proj.Name}\n" +
                $"Location: {proj.Path}\n" +
                $"PDK: {proj.PDK}\n" +
                $"RTL Files: {proj.RTLFiles.Count}\n" +
                $"Created: {proj.Created:yyyy-MM-dd HH:mm}\n" +
                $"Last Modified: {proj.LastModified:yyyy-MM-dd HH:mm}\n" +
                $"Build History: {proj.BuildHistory.Count} runs\n\n" +
                "[Settings editor UI to be implemented]";

            MessageBox.Show(settingsInfo, "Project Settings", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            LogToConsole("Displayed project settings");
        }

        private void OnRunSynthesis(object sender, RoutedEventArgs e) => RunWorkflowStage("Synthesis");
        private void OnRunFloorplan(object sender, RoutedEventArgs e) => RunWorkflowStage("Floorplan");
        private void OnRunPlacement(object sender, RoutedEventArgs e) => RunWorkflowStage("Placement");
        private void OnRunRouting(object sender, RoutedEventArgs e) => RunWorkflowStage("Routing");

        private void OnRunCompleteFlow(object sender, RoutedEventArgs e)
        {
            if (projectManager?.CurrentProject == null)
            {
                MessageBox.Show("Please load or create a project first.", "No Project", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LogToConsole("Starting complete EDA flow...");
            backend?.RunCompleteFlow(projectManager.CurrentProject);
        }

        private void OnStopFlow(object sender, RoutedEventArgs e)
        {
            backend?.Stop();
            LogToConsole("Flow stopped by user.");
            statusLabel.Text = "Stopped";
        }

        private void OnToolchainSetup(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Toolchain Setup functionality - to be implemented", "Toolchain Setup");
        }

        private void OnTimingAnalysis(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Timing Analysis functionality - to be implemented", "Timing Analysis");
        }

        private void OnPowerAnalysis(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Power Analysis functionality - to be implemented", "Power Analysis");
        }

        private void OnDRCCheck(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("DRC Check functionality - to be implemented", "DRC Check");
        }

        private void OnLVSCheck(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("LVS Check functionality - to be implemented", "LVS Check");
        }

        private void OnViewGDS(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open GDS/GDSII File",
                Filter = "GDSII Files (*.gds;*.gdsii)|*.gds;*.gdsii|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Try to open with associated program or show info
                    if (File.Exists(dialog.FileName))
                    {
                        var fileInfo = new FileInfo(dialog.FileName);
                        string message = $"GDS File: {fileInfo.Name}\n" +
                            $"Size: {fileInfo.Length / 1024.0:F2} KB\n" +
                            $"Modified: {fileInfo.LastWriteTime}\n\n" +
                            "Opening with system default viewer...";
                        
                        LogToConsole($"Opening GDS file: {fileInfo.Name}");
                        MessageBox.Show(message, "GDS File", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Try to open with default application
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = dialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open GDS file:\n\n{ex.Message}\n\n" +
                        "Install a GDS viewer like KLayout or gds3d to view layout files.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OnShowConsole(object sender, RoutedEventArgs e)
        {
            // Console is in the right panel GroupBox with TabControl
            // Find and select console tab
            LogToConsole("Console view activated");
        }

        private void OnShowReports(object sender, RoutedEventArgs e)
        {
            // Reports is in the right panel GroupBox with TabControl
            // Find and select reports tab
            LogToConsole("Reports view activated");
        }

        private void OnShowProjectExplorer(object sender, RoutedEventArgs e)
        {
            // Toggle project explorer column visibility
            LogToConsole("Project explorer toggle (to be implemented)");
        }

        private void OnBeginnerMode(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Beginner Mode activated - simplified interface", "Beginner Mode");
        }

        private void OnExpertMode(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Expert Mode activated - full control", "Expert Mode");
        }

        private void OnViewDocs(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Documentation viewer - to be implemented", "Documentation");
        }

        private void OnTutorials(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tutorials - to be implemented", "Tutorials");
        }

        private void OnAbout(object sender, RoutedEventArgs e)
        {
            string aboutText = "KAIROS EDA v1.0.0\n" +
                "Electronic Design Automation Suite\n\n" +
                "Complete RTL to GDSII Design Flow\n" +
                "• Synthesis • Floorplanning • Placement\n" +
                "• Clock Tree Synthesis • Routing\n" +
                "• Timing/Power Analysis • DRC/LVS\n\n" +
                "Framework: .NET 8.0 Windows\n" +
                "Build: 2025.11.24\n\n" +
                "© 2025 Kairos EDA Project";
            
            MessageBox.Show(aboutText, "About Kairos EDA", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnExit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Custom title bar window controls
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Double-click to maximize/restore
                MaximizeButton_Click(sender, e);
            }
            else
            {
                // Single-click to drag
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Backend event handlers
        private void OnBackendLog(object? sender, LogEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LogToConsole(e.Message);
                buildOutput.AppendText($"[{e.Level}] {e.Message}\n");
                buildOutput.ScrollToEnd();
            });
        }

        private void OnBackendProgress(object? sender, ProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = e.Progress;
                progressPercent.Text = $"{e.Progress}%";
                backendStatus.Text = $"Backend: {e.StageName} ({e.Progress}%)";
                buildStatus.Text = $"{e.StageName} - {e.Progress}%";
            });
        }

        private void OnStageCompleted(object? sender, StageCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                statusLabel.Text = e.Status;
                buildStatus.Text = e.Status;
                LogToConsole($"Stage {e.StageName} completed: {e.Status}");
                if (!string.IsNullOrEmpty(e.Metric) && !string.IsNullOrEmpty(e.Value))
                {
                    LogToConsole($"  {e.Metric}: {e.Value}");
                }
            });
        }

        private void LogToConsole(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            consoleOutput.AppendText($"[{timestamp}] {message}\n");
            consoleOutput.ScrollToEnd();
        }

        private void RefreshProjectExplorer()
        {
            projectTree.Items.Clear();

            if (projectManager?.CurrentProject == null)
            {
                var noProjectItem = new TreeViewItem { Header = "No Project Loaded" };
                projectTree.Items.Add(noProjectItem);
                projectStatus.Text = "No Project Loaded";
                return;
            }

            var project = projectManager.CurrentProject;
            projectStatus.Text = $"Project: {project.Name}";

            // Root project node
            var projectNode = new TreeViewItem
            {
                Header = $"📁 {project.Name}",
                IsExpanded = true
            };

            // RTL Files folder
            var rtlFolder = new TreeViewItem
            {
                Header = $"📄 RTL Files ({project.RTLFiles.Count})",
                IsExpanded = true
            };
            foreach (var rtlFile in project.RTLFiles)
            {
                var fileNode = new TreeViewItem
                {
                    Header = $"  {System.IO.Path.GetFileName(rtlFile)}",
                    Tag = rtlFile
                };
                rtlFolder.Items.Add(fileNode);
            }
            projectNode.Items.Add(rtlFolder);

            // PDK info
            var pdkNode = new TreeViewItem
            {
                Header = $"🔧 PDK: {project.PDK}"
            };
            projectNode.Items.Add(pdkNode);

            // Constraints
            var constraintsNode = new TreeViewItem
            {
                Header = "⚡ Constraints",
                IsExpanded = false
            };
            constraintsNode.Items.Add(new TreeViewItem
            {
                Header = $"Clock: {1000.0 / project.Constraints.ClockPeriodNs:F1} MHz"
            });
            constraintsNode.Items.Add(new TreeViewItem
            {
                Header = $"Voltage: {project.Constraints.VoltageV} V"
            });
            constraintsNode.Items.Add(new TreeViewItem
            {
                Header = $"Die: {project.Constraints.FloorplanWidthUm} × {project.Constraints.FloorplanHeightUm} µm"
            });
            projectNode.Items.Add(constraintsNode);

            // Build History
            if (project.BuildHistory.Count > 0)
            {
                var historyNode = new TreeViewItem
                {
                    Header = $"📊 Build History ({project.BuildHistory.Count})",
                    IsExpanded = false
                };
                foreach (var build in project.BuildHistory.TakeLast(5))
                {
                    string status = build.Success ? "✓" : "✗";
                    historyNode.Items.Add(new TreeViewItem
                    {
                        Header = $"{status} {build.Stage} - {build.Timestamp:HH:mm:ss}"
                    });
                }
                projectNode.Items.Add(historyNode);
            }

            projectTree.Items.Add(projectNode);
        }
    }
}
