using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KairosEDA.Models
{
    /// <summary>
    /// Manages the EDA toolchain (Yosys, OpenROAD, Magic, Netgen) for real ASIC design flow.
    /// Handles tool detection, configuration, and execution on Windows.
    /// </summary>
    public class EdaToolchain
    {
        public ToolchainConfig Config { get; private set; }
        
        public event EventHandler<ToolOutputEventArgs>? OutputReceived;
        public event EventHandler<ToolErrorEventArgs>? ErrorReceived;

        public EdaToolchain()
        {
            Config = LoadOrCreateConfig();
        }

        private ToolchainConfig LoadOrCreateConfig()
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KairosEDA",
                "toolchain.json"
            );

            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<ToolchainConfig>(json) ?? new ToolchainConfig();
                }
                catch
                {
                    return new ToolchainConfig();
                }
            }

            return new ToolchainConfig();
        }

        public void SaveConfig()
        {
            var configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KairosEDA"
            );

            Directory.CreateDirectory(configDir);

            var configPath = Path.Combine(configDir, "toolchain.json");
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(Config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(configPath, json);
        }

        /// <summary>
        /// Auto-detect installed EDA tools on the system
        /// </summary>
        public async Task<ToolDetectionResult> DetectTools()
        {
            var result = new ToolDetectionResult();

            // Check for Yosys
            result.YosysFound = await CheckToolExists("yosys", "--version");
            if (result.YosysFound && string.IsNullOrEmpty(Config.YosysPath))
            {
                Config.YosysPath = "yosys"; // Use PATH
            }

            // Check for OpenROAD
            result.OpenRoadFound = await CheckToolExists("openroad", "-version");
            if (result.OpenRoadFound && string.IsNullOrEmpty(Config.OpenRoadPath))
            {
                Config.OpenRoadPath = "openroad";
            }

            // Check for OpenSTA (usually built from source in WSL)
            result.OpenStaFound = await CheckToolExistsInWSL("sta", "-version") ||
                                  await CheckWSLFileExists("~/OpenSTA/build/sta") ||
                                  await CheckWSLFileExists("/usr/local/bin/sta") ||
                                  await CheckWSLFileExists("/usr/bin/sta");
            if (result.OpenStaFound && string.IsNullOrEmpty(Config.OpenStaPath))
            {
                // Try to find the actual path
                if (await CheckWSLFileExists("~/OpenSTA/build/sta"))
                    Config.OpenStaPath = "~/OpenSTA/build/sta";
                else if (await CheckWSLFileExists("/usr/local/bin/sta"))
                    Config.OpenStaPath = "/usr/local/bin/sta";
                else if (await CheckWSLFileExists("/usr/bin/sta"))
                    Config.OpenStaPath = "/usr/bin/sta";
                else
                    Config.OpenStaPath = "sta";
            }

            // Check for Magic
            result.MagicFound = await CheckToolExists("magic", "--version");
            if (result.MagicFound && string.IsNullOrEmpty(Config.MagicPath))
            {
                Config.MagicPath = "magic";
            }

            // Check for Netgen
            result.NetgenFound = await CheckToolExists("netgen", "-noconsole");
            if (result.NetgenFound && string.IsNullOrEmpty(Config.NetgenPath))
            {
                Config.NetgenPath = "netgen";
            }

            return result;
        }

        private async Task<bool> CheckToolExists(string command, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0 || process.ExitCode == 1; // Some tools return 1 for version
                }
            }
            catch
            {
                // Tool not found in PATH
            }

            return false;
        }

        private async Task<bool> CheckToolExistsWithPaths(string command, string args, string[] possiblePaths)
        {
            // Try each path
            foreach (var path in possiblePaths)
            {
                var expandedPath = path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                
                if (await CheckToolExists(expandedPath, args))
                {
                    return true;
                }
            }
            
            return false;
        }

        private async Task<bool> CheckToolExistsInWSL(string command, string args)
        {
            try
            {
                // Check if tool exists in WSL by running: wsl which <command>
                var whichPsi = new ProcessStartInfo
                {
                    FileName = "wsl",
                    Arguments = $"which {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var whichProcess = Process.Start(whichPsi);
                if (whichProcess != null)
                {
                    await whichProcess.WaitForExitAsync();
                    
                    // If 'which' found the command (exit code 0), verify it works
                    if (whichProcess.ExitCode == 0)
                    {
                        // Now try to actually run the command with args to verify
                        var testPsi = new ProcessStartInfo
                        {
                            FileName = "wsl",
                            Arguments = $"{command} {args}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using var testProcess = Process.Start(testPsi);
                        if (testProcess != null)
                        {
                            await testProcess.WaitForExitAsync();
                            // Some tools return non-zero for --version, so we just check if it ran
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // WSL not available or tool not found
            }

            return false;
        }

        private async Task<bool> CheckWSLFileExists(string path)
        {
            try
            {
                // Expand ~ to home directory and check if file exists in WSL
                var psi = new ProcessStartInfo
                {
                    FileName = "wsl",
                    Arguments = $"test -f {path} && echo exists",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    return output.Trim() == "exists";
                }
            }
            catch
            {
                // WSL not available or file check failed
            }

            return false;
        }

        /// <summary>
        /// Run Yosys synthesis
        /// </summary>
        public async Task<ToolResult> RunYosysSynthesis(
            Project project,
            string outputDir,
            CancellationToken token)
        {
            if (string.IsNullOrEmpty(Config.YosysPath))
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = "Yosys not configured. Please set the path in Settings."
                };
            }

            // Create Yosys script
            var scriptPath = Path.Combine(outputDir, "synthesis.ys");
            var netlistPath = Path.Combine(outputDir, "netlist.v");
            var jsonPath = Path.Combine(outputDir, "netlist.json");

            var script = new StringBuilder();
            
            // Read RTL files
            foreach (var rtlFile in project.RTLFiles)
            {
                script.AppendLine($"read_verilog {rtlFile}");
            }

            // Hierarchy and synthesis
            script.AppendLine("hierarchy -check -top top");
            script.AppendLine("proc; opt; fsm; opt; memory; opt");
            
            // Technology mapping based on PDK
            if (project.PDK.ToLower().Contains("sky130"))
            {
                script.AppendLine("# Sky130 technology mapping");
                script.AppendLine("techmap -map +/techmap.v");
                script.AppendLine("abc -liberty sky130_fd_sc_hd__tt_025C_1v80.lib");
            }
            else
            {
                script.AppendLine("# Generic technology mapping");
                script.AppendLine("techmap");
                script.AppendLine("abc");
            }

            script.AppendLine("clean");
            script.AppendLine($"write_verilog -noattr {netlistPath}");
            script.AppendLine($"write_json {jsonPath}");
            script.AppendLine("stat");

            File.WriteAllText(scriptPath, script.ToString());

            // Run Yosys
            return await ExecuteTool(
                Config.YosysPath,
                $"-s {scriptPath}",
                outputDir,
                token
            );
        }

        /// <summary>
        /// Run OpenROAD for floorplan, placement, CTS, and routing
        /// </summary>
        public async Task<ToolResult> RunOpenRoadStage(
            string stage,
            Project project,
            string workingDir,
            CancellationToken token)
        {
            if (string.IsNullOrEmpty(Config.OpenRoadPath))
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = "OpenROAD not configured. Please set the path in Settings."
                };
            }

            // Create OpenROAD TCL script based on stage
            var scriptPath = Path.Combine(workingDir, $"{stage}.tcl");
            var script = GenerateOpenRoadScript(stage, project, workingDir);
            File.WriteAllText(scriptPath, script);

            return await ExecuteTool(
                Config.OpenRoadPath,
                $"-exit {scriptPath}",
                workingDir,
                token
            );
        }

        private string GenerateOpenRoadScript(string stage, Project project, string workingDir)
        {
            var script = new StringBuilder();
            
            script.AppendLine("# OpenROAD script generated by KairosEDA");
            script.AppendLine($"# Stage: {stage}");
            script.AppendLine();

            switch (stage.ToLower())
            {
                case "floorplan":
                    script.AppendLine($"# Floorplan: {project.Constraints.FloorplanWidthUm}x{project.Constraints.FloorplanHeightUm} µm");
                    script.AppendLine("read_lef sky130_fd_sc_hd.tlef");
                    script.AppendLine("read_lef sky130_fd_sc_hd_merged.lef");
                    script.AppendLine($"read_verilog {Path.Combine(workingDir, "netlist.v")}");
                    script.AppendLine("link_design top");
                    script.AppendLine($"initialize_floorplan -utilization {project.Constraints.Utilization * 100} \\");
                    script.AppendLine($"  -aspect_ratio 1 \\");
                    script.AppendLine($"  -core_space 2");
                    script.AppendLine("write_def floorplan.def");
                    break;

                case "placement":
                    script.AppendLine("read_lef sky130_fd_sc_hd.tlef");
                    script.AppendLine("read_lef sky130_fd_sc_hd_merged.lef");
                    script.AppendLine("read_def floorplan.def");
                    script.AppendLine("global_placement");
                    script.AppendLine("detailed_placement");
                    script.AppendLine("write_def placement.def");
                    break;

                case "cts":
                    script.AppendLine("read_lef sky130_fd_sc_hd.tlef");
                    script.AppendLine("read_lef sky130_fd_sc_hd_merged.lef");
                    script.AppendLine("read_def placement.def");
                    script.AppendLine($"clock_tree_synthesis -root_buf {project.Constraints.ClockPort}");
                    script.AppendLine("write_def cts.def");
                    break;

                case "routing":
                    script.AppendLine("read_lef sky130_fd_sc_hd.tlef");
                    script.AppendLine("read_lef sky130_fd_sc_hd_merged.lef");
                    script.AppendLine("read_def cts.def");
                    script.AppendLine("global_route");
                    script.AppendLine("detailed_route");
                    script.AppendLine("write_def routed.def");
                    break;
            }

            return script.ToString();
        }

        /// <summary>
        /// Run Magic for DRC checks and GDS generation
        /// </summary>
        public async Task<ToolResult> RunMagicDRC(
            string defFile,
            string outputDir,
            CancellationToken token)
        {
            if (string.IsNullOrEmpty(Config.MagicPath))
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = "Magic not configured. Please set the path in Settings."
                };
            }

            var scriptPath = Path.Combine(outputDir, "drc.tcl");
            var script = new StringBuilder();
            
            script.AppendLine($"# Magic DRC script");
            script.AppendLine($"drc on");
            script.AppendLine($"drc check");
            script.AppendLine($"drc catchup");
            script.AppendLine($"set drcresult [drc listall why]");
            script.AppendLine($"puts \"DRC violations: $drcresult\"");
            script.AppendLine($"quit -noprompt");

            File.WriteAllText(scriptPath, script.ToString());

            return await ExecuteTool(
                Config.MagicPath,
                $"-noconsole -dnull {scriptPath}",
                outputDir,
                token
            );
        }

        /// <summary>
        /// Run Netgen for LVS checks
        /// </summary>
        public async Task<ToolResult> RunNetgenLVS(
            string layout,
            string schematic,
            string outputDir,
            CancellationToken token)
        {
            if (string.IsNullOrEmpty(Config.NetgenPath))
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = "Netgen not configured. Please set the path in Settings."
                };
            }

            return await ExecuteTool(
                Config.NetgenPath,
                $"-batch lvs {layout} {schematic}",
                outputDir,
                token
            );
        }

        private async Task<ToolResult> ExecuteTool(
            string toolPath,
            string arguments,
            string workingDir,
            CancellationToken token)
        {
            var result = new ToolResult { Success = false };
            var output = new StringBuilder();
            var errors = new StringBuilder();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                
                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                        OutputReceived?.Invoke(this, new ToolOutputEventArgs { Message = e.Data });
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errors.AppendLine(e.Data);
                        ErrorReceived?.Invoke(this, new ToolErrorEventArgs { Message = e.Data });
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(token);

                result.Success = process.ExitCode == 0;
                result.ExitCode = process.ExitCode;
                result.Output = output.ToString();
                result.ErrorMessage = errors.ToString();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Tool execution failed: {ex.Message}";
            }

            return result;
        }
    }

    public class ToolchainConfig
    {
        public string YosysPath { get; set; } = "";
        public string OpenRoadPath { get; set; } = "";
        public string OpenStaPath { get; set; } = "";
        public string MagicPath { get; set; } = "";
        public string NetgenPath { get; set; } = "";
        public string PdkPath { get; set; } = "";
        public bool UseDocker { get; set; } = false;
        public string DockerImage { get; set; } = "efabless/openlane:latest";
    }

    public class ToolDetectionResult
    {
        public bool YosysFound { get; set; }
        public bool OpenRoadFound { get; set; }
        public bool OpenStaFound { get; set; }
        public bool MagicFound { get; set; }
        public bool NetgenFound { get; set; }

        public bool AllToolsFound => YosysFound && OpenRoadFound && OpenStaFound && MagicFound && NetgenFound;
        public int FoundCount => (YosysFound ? 1 : 0) + (OpenRoadFound ? 1 : 0) + 
                                 (OpenStaFound ? 1 : 0) + (MagicFound ? 1 : 0) + (NetgenFound ? 1 : 0);
    }

    public class ToolResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    public class ToolOutputEventArgs : EventArgs
    {
        public string Message { get; set; } = "";
    }

    public class ToolErrorEventArgs : EventArgs
    {
        public string Message { get; set; } = "";
    }
}
