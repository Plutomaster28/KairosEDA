using System;
using System.Threading;
using System.Threading.Tasks;

namespace KairosEDA.Models
{
    /// <summary>
    /// Simulates backend EDA tool execution for demonstration purposes.
    /// In production, this would communicate with actual Yosys, OpenROAD, etc.
    /// </summary>
    public class BackendSimulator
    {
        private CancellationTokenSource? cancellationTokenSource;
        private bool isRunning = false;

        public event EventHandler<LogEventArgs>? LogReceived;
        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        public event EventHandler<StageCompletedEventArgs>? StageCompleted;

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
                await SimulateStageExecution(stageName, project, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                OnLog($"{stageName} stage was cancelled.", LogLevel.Warning);
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

            var stages = new[] { "synthesis", "floorplan", "placement", "cts", "routing", "verification" };
            
            foreach (var stage in stages)
            {
                if (cancellationTokenSource?.IsCancellationRequested == true)
                    break;

                RunStage(stage, project);
                await Task.Delay(100); // Small delay between stages
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
                await Task.Delay(1000);
                OnLog($"{analysisType} analysis complete.", LogLevel.Success);
                
                // Simulate results
                switch (analysisType.ToLower())
                {
                    case "timing":
                        OnLog("WNS: -0.15 ns", LogLevel.Info);
                        OnLog("TNS: -2.3 ns", LogLevel.Warning);
                        OnLog("Critical Path: clk -> FF_reg[7]/D (15 levels)", LogLevel.Info);
                        break;
                    case "power":
                        OnLog("Total Power: 45.7 mW", LogLevel.Info);
                        OnLog("  Dynamic: 38.2 mW", LogLevel.Info);
                        OnLog("  Leakage: 7.5 mW", LogLevel.Info);
                        break;
                    case "drc":
                        OnLog("DRC Check Complete", LogLevel.Success);
                        OnLog("Found 3 violations:", LogLevel.Warning);
                        OnLog("  - Metal1 spacing (2 instances)", LogLevel.Warning);
                        OnLog("  - Via enclosure (1 instance)", LogLevel.Warning);
                        break;
                    case "lvs":
                        OnLog("LVS Check Complete", LogLevel.Success);
                        OnLog("Layout vs Schematic: MATCH", LogLevel.Success);
                        break;
                }
            });
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
        }

        private async Task SimulateStageExecution(string stageName, Project project, CancellationToken token)
        {
            OnLog($"Backend.Run(\"{stageName}\", project_config)", LogLevel.Info);
            OnLog($"PDK: {project.PDK}", LogLevel.Info);
            OnLog($"RTL Files: {project.RTLFiles.Count} file(s)", LogLevel.Info);
            
            await Task.Delay(500, token);

            switch (stageName.ToLower())
            {
                case "synthesis":
                    await SimulateSynthesis(project, token);
                    break;
                case "floorplan":
                    await SimulateFloorplan(project, token);
                    break;
                case "placement":
                    await SimulatePlacement(project, token);
                    break;
                case "cts":
                    await SimulateCTS(project, token);
                    break;
                case "routing":
                    await SimulateRouting(project, token);
                    break;
                case "verification":
                    await SimulateVerification(project, token);
                    break;
            }
        }

        private async Task SimulateSynthesis(Project project, CancellationToken token)
        {
            OnLog("Invoking Yosys synthesis engine...", LogLevel.Info);
            await Task.Delay(300, token);
            
            for (int i = 0; i <= 100; i += 10)
            {
                token.ThrowIfCancellationRequested();
                OnProgress("Synthesis", i);
                
                if (i == 20) OnLog("Reading RTL files...", LogLevel.Info);
                if (i == 40) OnLog("Elaborating design hierarchy...", LogLevel.Info);
                if (i == 60) OnLog("Technology mapping to Sky130...", LogLevel.Info);
                if (i == 80) OnLog("Optimizing logic...", LogLevel.Info);
                
                await Task.Delay(200, token);
            }

            OnLog("Synthesis complete!", LogLevel.Success);
            OnLog("  Gates: 1,247", LogLevel.Info);
            OnLog("  Flip-Flops: 128", LogLevel.Info);
            OnLog("  Area estimate: 0.15 mm²", LogLevel.Info);
            
            OnStageComplete("Synthesis", "Gate Count", "1,247", "✓ Pass");
            OnStageComplete("Synthesis", "FF Count", "128", "✓ Pass");
            OnStageComplete("Synthesis", "Area", "0.15 mm²", "✓ Pass");
        }

        private async Task SimulateFloorplan(Project project, CancellationToken token)
        {
            OnLog("Invoking OpenROAD floorplanner...", LogLevel.Info);
            await Task.Delay(300, token);
            
            for (int i = 0; i <= 100; i += 20)
            {
                token.ThrowIfCancellationRequested();
                OnProgress("Floorplan", i);
                
                if (i == 20) OnLog($"Setting die area: {project.Constraints.FloorplanWidthUm}x{project.Constraints.FloorplanHeightUm} µm", LogLevel.Info);
                if (i == 40) OnLog("Placing I/O pads...", LogLevel.Info);
                if (i == 60) OnLog("Defining power grid...", LogLevel.Info);
                if (i == 80) OnLog("Setting routing tracks...", LogLevel.Info);
                
                await Task.Delay(250, token);
            }

            OnLog("Floorplan complete!", LogLevel.Success);
            OnLog($"  Die area: {project.Constraints.FloorplanWidthUm * project.Constraints.FloorplanHeightUm / 1_000_000:F3} mm²", LogLevel.Info);
            OnLog($"  Target utilization: {project.Constraints.Utilization * 100:F0}%", LogLevel.Info);
            
            OnStageComplete("Floorplan", "Die Area", $"{project.Constraints.FloorplanWidthUm * project.Constraints.FloorplanHeightUm / 1_000_000:F3} mm²", "✓ Pass");
        }

        private async Task SimulatePlacement(Project project, CancellationToken token)
        {
            OnLog("Invoking OpenROAD placer...", LogLevel.Info);
            await Task.Delay(300, token);
            
            for (int i = 0; i <= 100; i += 15)
            {
                token.ThrowIfCancellationRequested();
                OnProgress("Placement", i);
                
                if (i == 15) OnLog("Global placement...", LogLevel.Info);
                if (i == 45) OnLog("Detailed placement...", LogLevel.Info);
                if (i == 75) OnLog("Legalizing cell positions...", LogLevel.Info);
                if (i == 90) OnLog("Optimizing placement...", LogLevel.Info);
                
                await Task.Delay(300, token);
            }

            OnLog("Placement complete!", LogLevel.Success);
            OnLog("  Cells placed: 1,247", LogLevel.Info);
            OnLog("  Utilization: 68.5%", LogLevel.Info);
            OnLog("  HPWL: 12,450 µm", LogLevel.Info);
            
            OnStageComplete("Placement", "Utilization", "68.5%", "✓ Pass");
            OnStageComplete("Placement", "HPWL", "12,450 µm", "✓ Pass");
        }

        private async Task SimulateCTS(Project project, CancellationToken token)
        {
            OnLog("Invoking Clock Tree Synthesis...", LogLevel.Info);
            await Task.Delay(300, token);
            
            for (int i = 0; i <= 100; i += 25)
            {
                token.ThrowIfCancellationRequested();
                OnProgress("CTS", i);
                
                if (i == 25) OnLog("Building clock tree topology...", LogLevel.Info);
                if (i == 50) OnLog("Inserting clock buffers...", LogLevel.Info);
                if (i == 75) OnLog("Balancing clock skew...", LogLevel.Info);
                
                await Task.Delay(280, token);
            }

            OnLog("CTS complete!", LogLevel.Success);
            OnLog("  Clock nets: 128", LogLevel.Info);
            OnLog("  Buffers inserted: 45", LogLevel.Info);
            OnLog("  Max skew: 0.08 ns", LogLevel.Info);
            
            OnStageComplete("CTS", "Clock Skew", "0.08 ns", "✓ Pass");
            OnStageComplete("CTS", "Buffers", "45", "✓ Pass");
        }

        private async Task SimulateRouting(Project project, CancellationToken token)
        {
            OnLog("Invoking OpenROAD router...", LogLevel.Info);
            await Task.Delay(300, token);
            
            for (int i = 0; i <= 100; i += 10)
            {
                token.ThrowIfCancellationRequested();
                OnProgress("Routing", i);
                
                if (i == 10) OnLog("Global routing...", LogLevel.Info);
                if (i == 30) OnLog("Track assignment...", LogLevel.Info);
                if (i == 50) OnLog("Detailed routing - Layer 1-3...", LogLevel.Info);
                if (i == 70) OnLog("Detailed routing - Layer 4-6...", LogLevel.Info);
                if (i == 85) OnLog("Fixing DRC violations...", LogLevel.Info);
                if (i == 95) OnLog("Via optimization...", LogLevel.Info);
                
                await Task.Delay(350, token);
            }

            OnLog("Routing complete!", LogLevel.Success);
            OnLog("  Total wire length: 45.7 mm", LogLevel.Info);
            OnLog("  Vias: 3,254", LogLevel.Info);
            OnLog("  Congestion: Low", LogLevel.Success);
            
            OnStageComplete("Routing", "Wire Length", "45.7 mm", "✓ Pass");
            OnStageComplete("Routing", "Vias", "3,254", "✓ Pass");
            OnStageComplete("Routing", "Congestion", "Low", "✓ Pass");
        }

        private async Task SimulateVerification(Project project, CancellationToken token)
        {
            OnLog("Running Design Rule Check (DRC)...", LogLevel.Info);
            await Task.Delay(500, token);
            
            OnProgress("Verification", 30);
            OnLog("DRC: Checking spacing rules...", LogLevel.Info);
            await Task.Delay(300, token);
            
            OnProgress("Verification", 50);
            OnLog("DRC: Checking width rules...", LogLevel.Info);
            await Task.Delay(300, token);
            
            OnProgress("Verification", 70);
            OnLog("DRC complete: 0 violations", LogLevel.Success);
            
            OnLog("Running Layout vs Schematic (LVS)...", LogLevel.Info);
            await Task.Delay(500, token);
            
            OnProgress("Verification", 90);
            OnLog("LVS: Comparing netlists...", LogLevel.Info);
            await Task.Delay(400, token);
            
            OnProgress("Verification", 100);
            OnLog("LVS complete: MATCH ✓", LogLevel.Success);
            
            OnLog("Verification complete!", LogLevel.Success);
            OnLog("Design is ready for tape-out!", LogLevel.Success);
            
            OnStageComplete("Verification", "DRC", "0 violations", "✓ Pass");
            OnStageComplete("Verification", "LVS", "MATCH", "✓ Pass");
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

    public class LogEventArgs : EventArgs
    {
        public string Message { get; set; } = "";
        public LogLevel Level { get; set; }
    }

    public class ProgressEventArgs : EventArgs
    {
        public string StageName { get; set; } = "";
        public int Progress { get; set; }
    }

    public class StageCompletedEventArgs : EventArgs
    {
        public string StageName { get; set; } = "";
        public string Metric { get; set; } = "";
        public string Value { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
