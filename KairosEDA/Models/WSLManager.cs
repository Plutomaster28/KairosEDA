using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace KairosEDA.Models
{
    /// <summary>
    /// Manages WSL detection and command execution for EDA toolchain
    /// </summary>
    public class WSLManager
    {
        public bool IsWSLAvailable { get; private set; }
        public string DefaultDistro { get; private set; } = "";
        public string WSLVersion { get; private set; } = "";

        public WSLManager()
        {
            DetectWSL();
        }

        /// <summary>
        /// Detects if WSL is installed and gets default distribution
        /// </summary>
        private void DetectWSL()
        {
            try
            {
                var result = ExecuteWindowsCommand("wsl", "--status");
                if (result.exitCode == 0)
                {
                    IsWSLAvailable = true;
                    WSLVersion = ParseWSLVersion(result.output);

                    // Get default distro
                    var distroResult = ExecuteWindowsCommand("wsl", "--list --quiet");
                    if (distroResult.exitCode == 0 && !string.IsNullOrWhiteSpace(distroResult.output))
                    {
                        // First line is usually the default distro
                        var lines = distroResult.output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            DefaultDistro = lines[0].Trim().Replace("\0", "");
                        }
                    }
                }
            }
            catch
            {
                IsWSLAvailable = false;
            }
        }

        private string ParseWSLVersion(string output)
        {
            // Try to extract version from "Default Version: 2" line
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Default Version:"))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1)
                    {
                        return "WSL " + parts[1].Trim();
                    }
                }
            }
            return "WSL";
        }

        /// <summary>
        /// Executes a command in WSL and returns output
        /// </summary>
        public async Task<(int exitCode, string output, string error)> ExecuteWSLCommandAsync(
            string command, 
            string workingDirectory = "",
            Action<string>? onOutputReceived = null,
            Action<string>? onErrorReceived = null)
        {
            if (!IsWSLAvailable)
            {
                return (-1, "", "WSL is not available");
            }

            // Convert Windows path to WSL path if provided
            string wslWorkingDir = "";
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                wslWorkingDir = ConvertToWSLPath(workingDirectory);
            }

            // Build WSL command
            string fullCommand = command;
            if (!string.IsNullOrEmpty(wslWorkingDir))
            {
                fullCommand = $"cd '{wslWorkingDir}' && {command}";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = $"-d {DefaultDistro} -- bash -c \"{fullCommand.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            using var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    onOutputReceived?.Invoke(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                    onErrorReceived?.Invoke(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
        }

        /// <summary>
        /// Checks if a command exists in WSL
        /// </summary>
        public async Task<bool> CheckCommandExistsAsync(string command)
        {
            var result = await ExecuteWSLCommandAsync($"command -v {command}");
            return result.exitCode == 0 && !string.IsNullOrWhiteSpace(result.output);
        }

        /// <summary>
        /// Checks if a Docker image exists in WSL
        /// </summary>
        public async Task<bool> CheckDockerImageExistsAsync(string imageName)
        {
            var result = await ExecuteWSLCommandAsync($"docker images -q {imageName}");
            return result.exitCode == 0 && !string.IsNullOrWhiteSpace(result.output);
        }

        /// <summary>
        /// Gets version of a command in WSL
        /// </summary>
        public async Task<string> GetCommandVersionAsync(string command, string versionFlag = "--version")
        {
            var result = await ExecuteWSLCommandAsync($"{command} {versionFlag}");
            if (result.exitCode == 0)
            {
                // Return first line of output (usually contains version)
                var lines = result.output.Split('\n');
                if (lines.Length > 0)
                {
                    return lines[0].Trim();
                }
            }
            return "";
        }

        /// <summary>
        /// Converts Windows path to WSL path format (/mnt/c/...)
        /// </summary>
        public string ConvertToWSLPath(string windowsPath)
        {
            if (string.IsNullOrEmpty(windowsPath))
                return "";

            // Replace backslashes with forward slashes
            string path = windowsPath.Replace('\\', '/');

            // Convert drive letter (C: -> /mnt/c)
            if (path.Length >= 2 && path[1] == ':')
            {
                char driveLetter = char.ToLower(path[0]);
                path = $"/mnt/{driveLetter}" + path.Substring(2);
            }

            return path;
        }

        /// <summary>
        /// Converts WSL path to Windows path format
        /// </summary>
        public string ConvertToWindowsPath(string wslPath)
        {
            if (string.IsNullOrEmpty(wslPath))
                return "";

            // Convert /mnt/c/... to C:\...
            if (wslPath.StartsWith("/mnt/"))
            {
                var parts = wslPath.Substring(5).Split('/');
                if (parts.Length > 0)
                {
                    string driveLetter = parts[0].ToUpper();
                    string rest = string.Join("\\", parts, 1, parts.Length - 1);
                    return $"{driveLetter}:\\{rest}";
                }
            }

            return wslPath;
        }

        /// <summary>
        /// Execute a Windows command (for WSL detection)
        /// </summary>
        private (int exitCode, string output) ExecuteWindowsCommand(string command, string args)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return (-1, "");

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return (process.ExitCode, output);
            }
            catch
            {
                return (-1, "");
            }
        }
    }
}
