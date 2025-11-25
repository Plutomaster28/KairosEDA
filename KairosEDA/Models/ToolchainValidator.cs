using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KairosEDA.Models
{
    /// <summary>
    /// Operation mode based on detected tools
    /// </summary>
    public enum OperationMode
    {
        Unavailable,    // No WSL or tools detected
        Basic,          // OpenLane Docker image only
        Standard        // Full toolchain (OpenROAD/Yosys/etc.)
    }

    /// <summary>
    /// Information about a detected tool
    /// </summary>
    public class ToolInfo
    {
        public string Name { get; set; } = "";
        public bool IsAvailable { get; set; }
        public string Version { get; set; } = "";
        public string Type { get; set; } = ""; // "command" or "docker"
    }

    /// <summary>
    /// Validates EDA toolchain availability in WSL
    /// </summary>
    public class ToolchainValidator
    {
        private readonly WSLManager wslManager;

        // Tool definitions
        private readonly Dictionary<string, (string command, string versionFlag)> standardTools = new()
        {
            { "OpenROAD", ("openroad", "-version") },
            { "Yosys", ("yosys", "-version") },
            { "Magic", ("magic", "-version") },
            { "Netgen", ("netgen", "-batch") },
            { "KLayout", ("klayout", "-v") },
            { "OpenSTA", ("sta", "-version") }
        };

        private readonly List<string> dockerImages = new()
        {
            "efabless/openlane",
            "efabless/openlane2"
        };

        // Common installation directories to check
        private readonly List<string> openLaneSearchPaths = new()
        {
            "/opt/openlane",
            "/usr/local/openlane",
            "$HOME/openlane",
            "$HOME/.local/openlane",
            "/tools/openlane"
        };

        private readonly List<string> openRoadSearchPaths = new()
        {
            "$HOME/OpenROAD/build/bin",  // Common build location - check first
            "/opt/openroad",
            "/usr/local/openroad",
            "$HOME/openroad",
            "$HOME/.local/openroad",
            "/tools/openroad",
            "/usr/local/bin",
            "/usr/bin"
        };

        public OperationMode CurrentMode { get; private set; } = OperationMode.Unavailable;
        public Dictionary<string, ToolInfo> DetectedTools { get; private set; } = new();
        public List<string> MissingTools { get; private set; } = new();
        public bool HasOpenLane { get; private set; }
        public bool HasDocker { get; private set; }
        public string OpenLaneLocation { get; private set; } = "";
        public string OpenROADLocation { get; private set; } = "";

        public ToolchainValidator(WSLManager wslManager)
        {
            this.wslManager = wslManager;
        }

        /// <summary>
        /// Performs full toolchain validation
        /// </summary>
        public async Task<OperationMode> ValidateToolchainAsync(Action<string>? onProgress = null)
        {
            DetectedTools.Clear();
            MissingTools.Clear();
            HasOpenLane = false;
            HasDocker = false;

            if (!wslManager.IsWSLAvailable)
            {
                onProgress?.Invoke("❌ WSL is not available");
                CurrentMode = OperationMode.Unavailable;
                return CurrentMode;
            }

            onProgress?.Invoke("✓ WSL detected: " + wslManager.WSLVersion);
            onProgress?.Invoke($"  Distribution: {wslManager.DefaultDistro}");
            onProgress?.Invoke("");
            onProgress?.Invoke("Scanning for EDA tools...");

            // Check for Docker first
            HasDocker = await wslManager.CheckCommandExistsAsync("docker");
            if (HasDocker)
            {
                onProgress?.Invoke("✓ Docker is available in WSL");
                
                // Check for OpenLane Docker images
                foreach (var image in dockerImages)
                {
                    bool imageExists = await wslManager.CheckDockerImageExistsAsync(image);
                    if (imageExists)
                    {
                        HasOpenLane = true;
                        DetectedTools[image] = new ToolInfo
                        {
                            Name = image,
                            IsAvailable = true,
                            Type = "docker",
                            Version = "detected"
                        };
                        onProgress?.Invoke($"✓ Docker image found: {image}");
                    }
                }

                // Deep scan for OpenLane directory
                await CheckOpenLaneDirectoryAsync(onProgress);

                if (!HasOpenLane)
                {
                    onProgress?.Invoke("⚠ No OpenLane Docker images found in registry");
                    onProgress?.Invoke("  Searched for: efabless/openlane, efabless/openlane2");
                }
            }
            else
            {
                onProgress?.Invoke("⚠ Docker not found in WSL");
                onProgress?.Invoke("  Install Docker: sudo apt install docker.io");
            }

            onProgress?.Invoke("");
            onProgress?.Invoke("Checking for native tools...");

            // Check for OpenROAD specifically with deep scanning
            await CheckOpenROADAsync(onProgress);

            // Check for other standard tools
            int foundCount = DetectedTools.Values.Count(t => t.IsAvailable && t.Type == "command");
            
            foreach (var tool in standardTools)
            {
                if (tool.Key == "OpenROAD")
                    continue; // Already checked with deep scan

                bool exists = await wslManager.CheckCommandExistsAsync(tool.Value.command);
                
                if (exists)
                {
                    string version = await wslManager.GetCommandVersionAsync(tool.Value.command, tool.Value.versionFlag);
                    DetectedTools[tool.Key] = new ToolInfo
                    {
                        Name = tool.Key,
                        IsAvailable = true,
                        Version = version,
                        Type = "command"
                    };
                    onProgress?.Invoke($"✓ {tool.Key}: {version}");
                    foundCount++;
                }
                else
                {
                    DetectedTools[tool.Key] = new ToolInfo
                    {
                        Name = tool.Key,
                        IsAvailable = false,
                        Type = "command"
                    };
                    MissingTools.Add(tool.Key);
                }
            }

            if (foundCount == 0)
            {
                onProgress?.Invoke("⚠ No native EDA tools found");
            }

            // Determine operation mode
            onProgress?.Invoke("");
            CurrentMode = DetermineOperationMode();
            
            string modeDescription = CurrentMode switch
            {
                OperationMode.Standard => "STANDARD MODE - Full toolchain available",
                OperationMode.Basic => "BASIC MODE - OpenLane Docker only",
                OperationMode.Unavailable => "UNAVAILABLE - No tools detected",
                _ => "UNKNOWN"
            };

            onProgress?.Invoke("═══════════════════════════════════════");
            onProgress?.Invoke($"🔧 {modeDescription}");
            onProgress?.Invoke("═══════════════════════════════════════");

            if (CurrentMode == OperationMode.Standard && MissingTools.Count > 0)
            {
                onProgress?.Invoke("");
                onProgress?.Invoke("Missing optional tools:");
                foreach (var tool in MissingTools)
                {
                    onProgress?.Invoke($"  • {tool}");
                }
                onProgress?.Invoke("(Some features may be limited)");
            }

            return CurrentMode;
        }

        /// <summary>
        /// Determines operation mode based on detected tools
        /// </summary>
        private OperationMode DetermineOperationMode()
        {
            // Standard mode: Has OpenROAD OR Yosys (core tools)
            bool hasOpenROAD = DetectedTools.ContainsKey("OpenROAD") && DetectedTools["OpenROAD"].IsAvailable;
            bool hasYosys = DetectedTools.ContainsKey("Yosys") && DetectedTools["Yosys"].IsAvailable;

            if (hasOpenROAD || hasYosys)
            {
                return OperationMode.Standard;
            }

            // Basic mode: Has OpenLane Docker
            if (HasOpenLane)
            {
                return OperationMode.Basic;
            }

            // No tools available
            return OperationMode.Unavailable;
        }

        /// <summary>
        /// Gets a summary of the toolchain status
        /// </summary>
        public string GetSummary()
        {
            if (CurrentMode == OperationMode.Unavailable)
            {
                return "No EDA toolchain detected. Please install WSL and EDA tools.";
            }

            var available = DetectedTools.Values.Where(t => t.IsAvailable).ToList();
            int toolCount = available.Count;

            string summary = $"Mode: {CurrentMode}\n";
            summary += $"Tools available: {toolCount}\n";

            if (HasOpenLane)
            {
                summary += "OpenLane: Yes (Docker)\n";
            }

            foreach (var tool in available.Where(t => t.Type == "command"))
            {
                summary += $"{tool.Name}: {tool.Version}\n";
            }

            if (MissingTools.Count > 0)
            {
                summary += $"\nMissing: {string.Join(", ", MissingTools)}";
            }

            return summary;
        }

        /// <summary>
        /// Checks if a specific tool is available
        /// </summary>
        public bool IsToolAvailable(string toolName)
        {
            return DetectedTools.ContainsKey(toolName) && DetectedTools[toolName].IsAvailable;
        }

        /// <summary>
        /// Gets list of available tools for a specific stage
        /// </summary>
        public List<string> GetToolsForStage(string stage)
        {
            var tools = new List<string>();

            switch (stage.ToLower())
            {
                case "synthesis":
                    if (IsToolAvailable("Yosys")) tools.Add("Yosys");
                    if (HasOpenLane) tools.Add("OpenLane");
                    break;

                case "floorplan":
                case "placement":
                case "routing":
                case "cts":
                    if (IsToolAvailable("OpenROAD")) tools.Add("OpenROAD");
                    if (HasOpenLane) tools.Add("OpenLane");
                    break;

                case "drc":
                case "lvs":
                    if (IsToolAvailable("Magic")) tools.Add("Magic");
                    if (IsToolAvailable("Netgen")) tools.Add("Netgen");
                    if (HasOpenLane) tools.Add("OpenLane");
                    break;

                case "timing":
                    if (IsToolAvailable("OpenSTA")) tools.Add("OpenSTA");
                    if (IsToolAvailable("OpenROAD")) tools.Add("OpenROAD");
                    break;
            }

            return tools;
        }

        /// <summary>
        /// Deep scan for OpenROAD installation
        /// Checks for: native binary, Docker image, or compiled build
        /// </summary>
        private async Task CheckOpenROADAsync(Action<string>? onProgress = null)
        {
            bool found = false;
            string location = "";
            string version = "";
            string installType = "";

            // First check if openroad command exists in PATH
            bool inPath = await wslManager.CheckCommandExistsAsync("openroad");
            if (inPath)
            {
                version = await wslManager.GetCommandVersionAsync("openroad", "-version");
                var whichResult = await wslManager.ExecuteWSLCommandAsync("which openroad");
                location = whichResult.output.Trim();
                installType = "Native (in PATH)";
                found = true;
                onProgress?.Invoke($"✓ OpenROAD: {version}");
                onProgress?.Invoke($"  Location: {location}");
            }
            else
            {
                // Check common installation directories
                onProgress?.Invoke("  Scanning for OpenROAD installation...");
                
                foreach (var searchPath in openRoadSearchPaths)
                {
                    // Expand $HOME if present in path
                    string expandedPath = searchPath;
                    if (searchPath.Contains("$HOME"))
                    {
                        var homeResult = await wslManager.ExecuteWSLCommandAsync("echo $HOME");
                        string homePath = homeResult.output.Trim();
                        expandedPath = searchPath.Replace("$HOME", homePath);
                    }

                    // Check for binary directly in this location (e.g., ~/OpenROAD/build/bin/openroad)
                    var checkResult = await wslManager.ExecuteWSLCommandAsync($"test -f {expandedPath}/openroad && echo 'found' || echo 'not found'");
                    if (checkResult.output.Contains("found"))
                    {
                        location = $"{expandedPath}/openroad";
                        var versionResult = await wslManager.ExecuteWSLCommandAsync($"{location} -version");
                        version = versionResult.output.Split('\n').FirstOrDefault()?.Trim() ?? "detected";
                        
                        // Determine if it's a build or installed binary
                        if (expandedPath.Contains("/build/"))
                        {
                            installType = "Compiled build";
                            onProgress?.Invoke($"✓ OpenROAD found: {location}");
                            onProgress?.Invoke($"  Type: Compiled from source");
                        }
                        else
                        {
                            installType = "Native binary";
                            onProgress?.Invoke($"✓ OpenROAD found: {location}");
                        }
                        
                        onProgress?.Invoke($"  Version: {version}");
                        found = true;
                        break;
                    }

                    // Check for build directory with bin/openroad (for paths that don't include /bin)
                    if (!expandedPath.EndsWith("/bin"))
                    {
                        var checkBinResult = await wslManager.ExecuteWSLCommandAsync($"test -f {expandedPath}/bin/openroad && echo 'found' || echo 'not found'");
                        if (checkBinResult.output.Contains("found"))
                        {
                            location = $"{expandedPath}/bin/openroad";
                            var versionResult = await wslManager.ExecuteWSLCommandAsync($"{location} -version");
                            version = versionResult.output.Split('\n').FirstOrDefault()?.Trim() ?? "detected";
                            installType = "Compiled build";
                            found = true;
                            onProgress?.Invoke($"✓ OpenROAD found: {location}");
                            onProgress?.Invoke($"  Type: Compiled from source");
                            onProgress?.Invoke($"  Version: {version}");
                            break;
                        }
                    }
                }

                // Check for Docker image as fallback
                if (!found && HasDocker)
                {
                    var dockerCheckResult = await wslManager.ExecuteWSLCommandAsync("docker images -q openroad/openroad");
                    if (!string.IsNullOrWhiteSpace(dockerCheckResult.output))
                    {
                        location = "docker:openroad/openroad";
                        version = "Docker image";
                        installType = "Docker container";
                        found = true;
                        onProgress?.Invoke($"✓ OpenROAD Docker image found");
                    }
                }
            }

            if (found)
            {
                DetectedTools["OpenROAD"] = new ToolInfo
                {
                    Name = "OpenROAD",
                    IsAvailable = true,
                    Version = version,
                    Type = installType
                };
                OpenROADLocation = location;
            }
            else
            {
                DetectedTools["OpenROAD"] = new ToolInfo
                {
                    Name = "OpenROAD",
                    IsAvailable = false,
                    Type = "command"
                };
                MissingTools.Add("OpenROAD");
                onProgress?.Invoke("  ⚠ OpenROAD not found");
                onProgress?.Invoke("  Please compile OpenROAD or install via Docker:");
                onProgress?.Invoke("    • Compile: https://github.com/The-OpenROAD-Project/OpenROAD");
                onProgress?.Invoke("    • Docker: docker pull openroad/openroad");
            }
        }

        /// <summary>
        /// Deep scan for OpenLane installation
        /// Checks for Docker images/containers in OpenLane directory
        /// </summary>
        private async Task CheckOpenLaneDirectoryAsync(Action<string>? onProgress = null)
        {
            if (!HasDocker)
                return;

            bool foundDirectory = false;
            string openlaneDir = "";

            // Check for OpenLane installation directory
            foreach (var searchPath in openLaneSearchPaths)
            {
                var checkResult = await wslManager.ExecuteWSLCommandAsync($"test -d {searchPath} && echo 'found' || echo 'not found'");
                if (checkResult.output.Contains("found"))
                {
                    openlaneDir = searchPath;
                    foundDirectory = true;
                    onProgress?.Invoke($"  Found OpenLane directory: {searchPath}");
                    break;
                }
            }

            if (foundDirectory)
            {
                OpenLaneLocation = openlaneDir;
                
                // Check if Docker engine is running
                var dockerRunningCheck = await wslManager.ExecuteWSLCommandAsync("docker info > /dev/null 2>&1 && echo 'running' || echo 'stopped'");
                
                if (dockerRunningCheck.output.Contains("stopped"))
                {
                    onProgress?.Invoke("  ⚠ Docker engine is not running!");
                    onProgress?.Invoke("  Please start Docker: sudo systemctl start docker");
                    onProgress?.Invoke("  Or: sudo service docker start");
                }
                else
                {
                    onProgress?.Invoke("  ✓ Docker engine is running");
                }

                // Check for containers/images in the directory context
                var imagesInDirCheck = await wslManager.ExecuteWSLCommandAsync(
                    $"cd {openlaneDir} && docker images | grep -i openlane");
                
                if (string.IsNullOrWhiteSpace(imagesInDirCheck.output))
                {
                    onProgress?.Invoke("  ⚠ No OpenLane Docker images found");
                    onProgress?.Invoke("  Please pull OpenLane image:");
                    onProgress?.Invoke("    docker pull efabless/openlane:latest");
                    HasOpenLane = false;
                }
            }
        }
    }
}
