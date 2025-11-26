using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KairosEDA.Models
{
    /// <summary>
    /// Real EDA backend that orchestrates Yosys, OpenROAD, Magic, and Netgen
    /// for actual ASIC design flow execution.
    /// </summary>
    public class EdaBackend
    {
        private EdaToolchain toolchain;
        private CancellationTokenSource? cancellationTokenSource;
        private bool isRunning = false;

        public event EventHandler<LogEventArgs>? LogReceived;
        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        public event EventHandler<StageCompletedEventArgs>? StageCompleted;

        public EdaBackend()
        {
            toolchain = new EdaToolchain();
            
            // Subscribe to tool output
            toolchain.OutputReceived += (s, e) => OnLog(e.Message, LogLevel.Info);
            toolchain.ErrorReceived += (s, e) => OnLog(e.Message, LogLevel.Warning);
            
            // Create workspace directory for runs
            EnsureWorkspaceDirectory();
        }

        private void EnsureWorkspaceDirectory()
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string workspaceDir = Path.Combine(documentsPath, "KairosEDA_Directory");
                
                if (!Directory.Exists(workspaceDir))
                {
                    Directory.CreateDirectory(workspaceDir);
                    OnLog($"✓ Created workspace directory: {workspaceDir}", LogLevel.Success);
                }
            }
            catch (Exception ex)
            {
                OnLog($"Warning: Could not create workspace directory: {ex.Message}", LogLevel.Warning);
            }
        }

        public EdaToolchain Toolchain => toolchain;

        public async Task<ToolDetectionResult> DetectTools()
        {
            return await toolchain.DetectTools();
        }

        public async void RunStage(string stageName, Project? project)
        {
            if (isRunning)
            {
                OnLog("Another stage is already running. Please wait.", LogLevel.Warning);
                return;
            }

            if (project == null)
            {
                OnLog("No project loaded. Please create or open a project first.", LogLevel.Error);
                return;
            }

            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await ExecuteStage(stageName, project, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                OnLog($"{stageName} stage was cancelled.", LogLevel.Warning);
            }
            catch (Exception ex)
            {
                OnLog($"Error during {stageName}: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                isRunning = false;
            }
        }

        public async void RunCompleteFlow(Project? project)
        {
            if (project == null)
            {
                OnLog("No project loaded.", LogLevel.Error);
                return;
            }

            if (isRunning)
            {
                OnLog("Flow already running.", LogLevel.Warning);
                return;
            }

            var stages = new[] { "synthesis", "floorplan", "placement", "cts", "routing", "verification" };
            
            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                foreach (var stage in stages)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    OnLog($"\n=== Starting {stage} stage ===\n", LogLevel.Stage);
                    await ExecuteStage(stage, project, cancellationTokenSource.Token);
                    
                    await Task.Delay(500); // Brief pause between stages
                }

                OnLog("\n=== Complete flow finished ===\n", LogLevel.Success);
            }
            catch (OperationCanceledException)
            {
                OnLog("Flow was cancelled by user.", LogLevel.Warning);
            }
            catch (Exception ex)
            {
                OnLog($"Flow error: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                isRunning = false;
            }
        }

        public void RunAnalysis(string analysisType, Project? project)
        {
            if (project == null)
            {
                OnLog("No project loaded.", LogLevel.Error);
                return;
            }

            Task.Run(async () =>
            {
                OnLog($"Starting {analysisType} analysis...", LogLevel.Info);
                
                // TODO: Implement real analysis using OpenROAD timing/power analysis
                // For now, placeholder that will be replaced with real tool calls
                await Task.Delay(1000);
                
                OnLog($"{analysisType} analysis requires OpenROAD STA integration.", LogLevel.Warning);
                OnLog("Feature coming soon!", LogLevel.Info);
            });
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            OnLog("Stopping current operation...", LogLevel.Warning);
        }

        public string GetProjectRunDirectory(Project project)
        {
            // Create timestamped run directory in KairosEDA_Directory
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string workspaceDir = Path.Combine(documentsPath, "KairosEDA_Directory");
            string projectDir = Path.Combine(workspaceDir, project.Name);
            string runDir = Path.Combine(projectDir, $"run_{DateTime.Now:yyyyMMdd_HHmmss}");
            
            Directory.CreateDirectory(runDir);
            return runDir;
        }

        private async Task ExecuteStage(string stageName, Project project, CancellationToken token)
        {
            // Create project output directory in both project path and KairosEDA_Directory
            var outputDir = Path.Combine(project.Path, "kairos_output");
            Directory.CreateDirectory(outputDir);

            var stageDir = Path.Combine(outputDir, stageName.ToLower());
            Directory.CreateDirectory(stageDir);

            // Also create in centralized workspace
            string runDir = GetProjectRunDirectory(project);
            string stageRunDir = Path.Combine(runDir, stageName.ToLower());
            Directory.CreateDirectory(stageRunDir);

            OnLog($"Working directory: {stageDir}", LogLevel.Info);
            OnLog($"Run directory: {stageRunDir}", LogLevel.Info);
            OnLog($"PDK: {project.PDK}", LogLevel.Info);

            ToolResult result;

            switch (stageName.ToLower())
            {
                case "synthesis":
                    result = await ExecuteSynthesis(project, stageDir, token);
                    break;

                case "floorplan":
                    result = await ExecuteFloorplan(project, stageDir, token);
                    break;

                case "placement":
                    result = await ExecutePlacement(project, stageDir, token);
                    break;

                case "cts":
                    result = await ExecuteCTS(project, stageDir, token);
                    break;

                case "routing":
                    result = await ExecuteRouting(project, stageDir, token);
                    break;

                case "verification":
                    result = await ExecuteVerification(project, stageDir, token);
                    break;

                default:
                    OnLog($"Unknown stage: {stageName}", LogLevel.Error);
                    return;
            }

            if (result.Success)
            {
                OnLog($"{stageName} completed successfully!", LogLevel.Success);
                OnProgress(stageName, 100);
            }
            else
            {
                OnLog($"{stageName} failed: {result.ErrorMessage}", LogLevel.Error);
            }
        }

        private async Task<ToolResult> ExecuteSynthesis(Project project, string workingDir, CancellationToken token)
        {
            OnLog("Starting Yosys synthesis...", LogLevel.Info);
            OnProgress("Synthesis", 10);

            if (project.RTLFiles.Count == 0)
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = "No RTL files in project. Please add Verilog files first."
                };
            }

            OnLog($"RTL files: {project.RTLFiles.Count}", LogLevel.Info);
            foreach (var file in project.RTLFiles)
            {
                OnLog($"  - {Path.GetFileName(file)}", LogLevel.Info);
            }

            OnProgress("Synthesis", 30);
            var result = await toolchain.RunYosysSynthesis(project, workingDir, token);

            if (result.Success)
            {
                OnProgress("Synthesis", 100);
                
                // Parse output for statistics
                var netlistPath = Path.Combine(workingDir, "netlist.v");
                if (File.Exists(netlistPath))
                {
                    OnLog($"Generated netlist: {netlistPath}", LogLevel.Success);
                    OnStageComplete("Synthesis", "Netlist", "Generated", "✓ Pass");
                }
            }

            return result;
        }

        private async Task<ToolResult> ExecuteFloorplan(Project project, string workingDir, CancellationToken token)
        {
            OnLog("Starting OpenROAD floorplanning...", LogLevel.Info);
            OnProgress("Floorplan", 10);

            // Check for netlist from synthesis
            var netlistPath = Path.Combine(project.Path, "kairos_output", "synthesis", "netlist.v");
            if (!File.Exists(netlistPath))
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = "Netlist not found. Please run synthesis first."
                };
            }

            OnLog($"Die size: {project.Constraints.FloorplanWidthUm} x {project.Constraints.FloorplanHeightUm} µm", LogLevel.Info);
            OnLog($"Target utilization: {project.Constraints.Utilization * 100:F1}%", LogLevel.Info);
            
            OnProgress("Floorplan", 40);
            var result = await toolchain.RunOpenRoadStage("floorplan", project, workingDir, token);

            if (result.Success)
            {
                OnProgress("Floorplan", 100);
                var area = project.Constraints.FloorplanWidthUm * project.Constraints.FloorplanHeightUm / 1_000_000;
                OnStageComplete("Floorplan", "Die Area", $"{area:F3} mm²", "✓ Pass");
            }

            return result;
        }

        private async Task<ToolResult> ExecutePlacement(Project project, string workingDir, CancellationToken token)
        {
            OnLog("Starting OpenROAD placement...", LogLevel.Info);
            OnProgress("Placement", 10);

            OnProgress("Placement", 30);
            OnLog("Global placement...", LogLevel.Info);
            
            var result = await toolchain.RunOpenRoadStage("placement", project, workingDir, token);

            if (result.Success)
            {
                OnProgress("Placement", 100);
                OnStageComplete("Placement", "Status", "Complete", "✓ Pass");
            }

            return result;
        }

        private async Task<ToolResult> ExecuteCTS(Project project, string workingDir, CancellationToken token)
        {
            OnLog("Starting Clock Tree Synthesis...", LogLevel.Info);
            OnProgress("CTS", 10);

            OnLog($"Clock net: {project.Constraints.ClockPort}", LogLevel.Info);
            OnLog($"Target period: {project.Constraints.ClockPeriodNs} ns", LogLevel.Info);

            OnProgress("CTS", 40);
            var result = await toolchain.RunOpenRoadStage("cts", project, workingDir, token);

            if (result.Success)
            {
                OnProgress("CTS", 100);
                OnStageComplete("CTS", "Status", "Complete", "✓ Pass");
            }

            return result;
        }

        private async Task<ToolResult> ExecuteRouting(Project project, string workingDir, CancellationToken token)
        {
            OnLog("Starting OpenROAD routing...", LogLevel.Info);
            OnProgress("Routing", 10);

            OnLog($"Routing layers: {project.Constraints.RoutingLayers}", LogLevel.Info);
            
            OnProgress("Routing", 30);
            OnLog("Global routing...", LogLevel.Info);

            OnProgress("Routing", 60);
            OnLog("Detailed routing...", LogLevel.Info);

            var result = await toolchain.RunOpenRoadStage("routing", project, workingDir, token);

            if (result.Success)
            {
                OnProgress("Routing", 100);
                OnStageComplete("Routing", "Status", "Complete", "✓ Pass");
            }

            return result;
        }

        private async Task<ToolResult> ExecuteVerification(Project project, string workingDir, CancellationToken token)
        {
            OnLog("Starting Design Verification...", LogLevel.Info);
            OnProgress("Verification", 10);

            // DRC Check
            OnLog("Running DRC check with Magic...", LogLevel.Info);
            OnProgress("Verification", 30);

            var defFile = Path.Combine(project.Path, "kairos_output", "routing", "routed.def");
            var drcResult = await toolchain.RunMagicDRC(defFile, workingDir, token);

            if (!drcResult.Success)
            {
                OnLog("DRC check failed", LogLevel.Error);
                return drcResult;
            }

            OnLog("DRC check passed", LogLevel.Success);
            OnProgress("Verification", 60);

            // LVS Check
            OnLog("Running LVS check with Netgen...", LogLevel.Info);
            OnProgress("Verification", 80);

            // TODO: Implement actual LVS with proper file paths
            OnLog("LVS check: Feature requires layout extraction", LogLevel.Warning);
            
            OnProgress("Verification", 100);
            OnStageComplete("Verification", "DRC", "Pass", "✓ Pass");
            OnStageComplete("Verification", "LVS", "Pending", "⚠ N/A");

            return new ToolResult { Success = true };
        }

        private void OnLog(string message, LogLevel level)
        {
            LogReceived?.Invoke(this, new LogEventArgs { Message = message, Level = level });
        }

        private void OnProgress(string stageName, int progress)
        {
            ProgressChanged?.Invoke(this, new ProgressEventArgs { StageName = stageName, Progress = progress });
        }

        private void OnStageComplete(string stage, string metric, string value, string status)
        {
            StageCompleted?.Invoke(this, new StageCompletedEventArgs
            {
                StageName = stage,
                Metric = metric,
                Value = value,
                Status = status
            });
        }
    }
}
