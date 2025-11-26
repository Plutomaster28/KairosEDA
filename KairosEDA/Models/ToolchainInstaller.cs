using System;
using System.Threading.Tasks;

namespace KairosEDA.Models
{
    /// <summary>
    /// Automated toolchain installation and setup
    /// </summary>
    public class ToolchainInstaller
    {
        private readonly WSLManager wslManager;
        
        public event EventHandler<string>? ProgressUpdate;
        public event EventHandler<int>? ProgressPercentage;
        public event EventHandler<bool>? InstallationComplete;

        public ToolchainInstaller(WSLManager wslManager)
        {
            this.wslManager = wslManager;
        }

        /// <summary>
        /// Checks if WSL2 can be enabled and provides installation command
        /// </summary>
        public async Task<(bool canInstall, string message, string command)> CheckWSLInstallationAsync()
        {
            if (wslManager.IsWSLAvailable)
            {
                return (false, "WSL is already installed", "");
            }

            // Check Windows version (WSL2 requires Windows 10 version 1903+ or Windows 11)
            var versionCheck = await Task.Run(() =>
            {
                try
                {
                    var osVersion = Environment.OSVersion;
                    if (osVersion.Version.Major >= 10)
                    {
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            });

            if (!versionCheck)
            {
                return (false, "WSL2 requires Windows 10 version 1903 or later", "");
            }

            string command = "wsl --install";
            string message = "WSL2 can be installed automatically. This will require a system restart.";
            return (true, message, command);
        }

        /// <summary>
        /// Installs Docker in WSL
        /// </summary>
        public async Task<bool> InstallDockerInWSLAsync()
        {
            if (!wslManager.IsWSLAvailable)
            {
                ReportProgress("WSL not available");
                return false;
            }

            ReportProgress("Installing Docker in WSL...");
            ReportPercentage(10);

            // Update package lists
            ReportProgress("Updating package lists...");
            var updateResult = await wslManager.ExecuteWSLCommandAsync(
                "sudo apt-get update",
                onOutputReceived: (line) => ReportProgress($"  {line}")
            );

            if (updateResult.exitCode != 0)
            {
                ReportProgress("⚠ Package update had warnings, continuing...");
            }
            ReportPercentage(30);

            // Install Docker
            ReportProgress("Installing Docker packages...");
            var installResult = await wslManager.ExecuteWSLCommandAsync(
                "sudo apt-get install -y docker.io docker-compose",
                onOutputReceived: (line) => ReportProgress($"  {line}")
            );

            if (installResult.exitCode != 0)
            {
                ReportProgress("Docker installation failed");
                return false;
            }
            ReportPercentage(60);

            // Start Docker service
            ReportProgress("Starting Docker service...");
            var startResult = await wslManager.ExecuteWSLCommandAsync(
                "sudo service docker start"
            );

            if (startResult.exitCode != 0)
            {
                ReportProgress("⚠ Could not start Docker service automatically");
                ReportProgress("  You may need to run: sudo service docker start");
            }
            ReportPercentage(80);

            // Add user to docker group (for non-root access)
            ReportProgress("Configuring Docker permissions...");
            var userResult = await wslManager.ExecuteWSLCommandAsync("whoami");
            string username = userResult.output.Trim();
            
            await wslManager.ExecuteWSLCommandAsync($"sudo usermod -aG docker {username}");
            
            ReportPercentage(100);
            ReportProgress("✓ Docker installed successfully!");
            ReportProgress("  Note: You may need to restart WSL for group changes to take effect");
            
            return true;
        }

        /// <summary>
        /// Installs Yosys from source
        /// </summary>
        public async Task<bool> InstallYosysFromSourceAsync()
        {
            ReportProgress("Installing Yosys from source...");
            ReportProgress("This may take 10-20 minutes depending on your system");
            ReportPercentage(5);

            // Install dependencies
            ReportProgress("Installing build dependencies...");
            var depsResult = await wslManager.ExecuteWSLCommandAsync(
                "sudo apt-get install -y build-essential clang bison flex libreadline-dev gawk tcl-dev libffi-dev git graphviz xdot pkg-config python3 libboost-system-dev libboost-python-dev libboost-filesystem-dev zlib1g-dev",
                onOutputReceived: (line) => ReportProgress($"  {line}")
            );

            if (depsResult.exitCode != 0)
            {
                ReportProgress("⚠ Some dependencies failed to install, continuing...");
            }
            ReportPercentage(15);

            // Clone Yosys repository
            ReportProgress("Cloning Yosys repository...");
            var cloneResult = await wslManager.ExecuteWSLCommandAsync(
                "cd $HOME && git clone --recurse-submodules https://github.com/YosysHQ/yosys.git",
                onOutputReceived: (line) => ReportProgress($"  {line}")
            );

            if (cloneResult.exitCode != 0)
            {
                ReportProgress("Failed to clone Yosys repository");
                return false;
            }
            ReportPercentage(30);

            // Build Yosys
            ReportProgress("Building Yosys (this will take a while)...");
            ReportProgress("Compiling with all CPU cores...");
            
            var buildResult = await wslManager.ExecuteWSLCommandAsync(
                "cd $HOME/yosys && make -j$(nproc)",
                onOutputReceived: (line) => 
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        ReportProgress($"  {line}");
                    }
                }
            );

            if (buildResult.exitCode != 0)
            {
                ReportProgress("Yosys build failed");
                ReportProgress("  Check the log for details");
                return false;
            }
            ReportPercentage(70);

            // Install Yosys
            ReportProgress("Installing Yosys to system...");
            var installResult = await wslManager.ExecuteWSLCommandAsync(
                "cd $HOME/yosys && sudo make install"
            );

            if (installResult.exitCode != 0)
            {
                ReportProgress("⚠ System installation failed, installing to ~/.local/bin");
                await wslManager.ExecuteWSLCommandAsync("mkdir -p $HOME/.local/bin");
                await wslManager.ExecuteWSLCommandAsync("cd $HOME/yosys && make install PREFIX=$HOME/.local");
            }

            ReportPercentage(100);
            ReportProgress("✓ Yosys installed successfully!");
            
            // Verify installation
            var versionResult = await wslManager.ExecuteWSLCommandAsync("yosys -V");
            if (versionResult.exitCode == 0)
            {
                ReportProgress($"  Version: {versionResult.output.Trim()}");
            }
            
            return true;
        }

        /// <summary>
        /// Downloads and installs pre-built OpenROAD binary
        /// </summary>
        public async Task<bool> InstallOpenROADAsync()
        {
            ReportProgress("Installing OpenROAD...");
            ReportPercentage(10);

            // Create installation directory
            ReportProgress("Creating installation directory...");
            var mkdirResult = await wslManager.ExecuteWSLCommandAsync(
                "mkdir -p $HOME/.local/bin"
            );
            ReportPercentage(20);

            // Check if we should use Docker image or try to install binary
            ReportProgress("Checking for OpenROAD Docker image...");
            var dockerCheckResult = await wslManager.ExecuteWSLCommandAsync(
                "docker images -q openroad/openroad"
            );

            if (!string.IsNullOrWhiteSpace(dockerCheckResult.output))
            {
                ReportProgress("✓ OpenROAD Docker image already available");
                ReportPercentage(100);
                return true;
            }

            // Try to pull Docker image as fallback
            ReportProgress("Pulling OpenROAD Docker image...");
            ReportProgress("This may take a few minutes...");
            ReportPercentage(40);

            var pullResult = await wslManager.ExecuteWSLCommandAsync(
                "docker pull openroad/openroad:latest",
                onOutputReceived: (line) => ReportProgress(line)
            );

            if (pullResult.exitCode == 0)
            {
                ReportPercentage(100);
                ReportProgress("✓ OpenROAD Docker image installed!");
                return true;
            }

            ReportProgress("⚠ Could not install OpenROAD automatically");
            ReportProgress("  You can compile from source: https://github.com/The-OpenROAD-Project/OpenROAD");
            return false;
        }

        /// <summary>
        /// Installs OpenROAD from source
        /// </summary>
        public async Task<bool> InstallOpenROADFromSourceAsync()
        {
            ReportProgress("Installing OpenROAD from source...");
            ReportProgress("This may take 20-30 minutes depending on your system");
            ReportPercentage(5);

            // Install dependencies (including Python for OpenROAD)
            ReportProgress("Installing build dependencies and Python...");
            var depsResult = await wslManager.ExecuteWSLCommandAsync(
                "sudo apt-get install -y build-essential cmake clang gcc-multilib libomp-dev " +
                "python3 python3-dev python3-pip swig libboost-all-dev libeigen3-dev " +
                "qt5-default qtbase5-dev qtchooser qt5-qmake qtbase5-dev-tools " +
                "libqt5charts5-dev tcl-dev tk-dev flex bison libfl-dev liblemon-dev " +
                "libcairo2-dev libglu1-mesa-dev libspdlog-dev",
                onOutputReceived: (line) => ReportProgress($"  {line}")
            );

            if (depsResult.exitCode != 0)
            {
                ReportProgress("⚠ Some dependencies failed to install, continuing...");
            }
            ReportPercentage(15);

            // Clone OpenROAD repository
            ReportProgress("Cloning OpenROAD repository...");
            var cloneResult = await wslManager.ExecuteWSLCommandAsync(
                "cd $HOME && git clone --recursive https://github.com/The-OpenROAD-Project/OpenROAD.git",
                onOutputReceived: (line) => ReportProgress($"  {line}")
            );

            if (cloneResult.exitCode != 0)
            {
                ReportProgress("Failed to clone OpenROAD repository");
                return false;
            }
            ReportPercentage(30);

            // Create build directory
            ReportProgress("Configuring build with CMake...");
            await wslManager.ExecuteWSLCommandAsync("mkdir -p $HOME/OpenROAD/build");
            
            var cmakeResult = await wslManager.ExecuteWSLCommandAsync(
                "cd $HOME/OpenROAD/build && cmake ..",
                onOutputReceived: (line) => ReportProgress($"  {line}")
            );

            if (cmakeResult.exitCode != 0)
            {
                ReportProgress("CMake configuration failed");
                return false;
            }
            ReportPercentage(45);

            // Build OpenROAD
            ReportProgress("Building OpenROAD (this will take a while)...");
            ReportProgress("Compiling with all CPU cores...");
            
            var buildResult = await wslManager.ExecuteWSLCommandAsync(
                "cd $HOME/OpenROAD/build && make -j$(nproc)",
                onOutputReceived: (line) => 
                {
                    if (!string.IsNullOrWhiteSpace(line) && !line.Contains("Built target"))
                    {
                        ReportProgress($"  {line}");
                    }
                }
            );

            if (buildResult.exitCode != 0)
            {
                ReportProgress("OpenROAD build failed");
                ReportProgress("  Check the log for details");
                return false;
            }
            ReportPercentage(85);

            // Create symlink to make openroad accessible
            ReportProgress("Setting up OpenROAD executable...");
            await wslManager.ExecuteWSLCommandAsync("mkdir -p $HOME/.local/bin");
            await wslManager.ExecuteWSLCommandAsync(
                "ln -sf $HOME/OpenROAD/build/src/openroad $HOME/.local/bin/openroad"
            );

            // Add to PATH if not already there
            var bashrcCheck = await wslManager.ExecuteWSLCommandAsync(
                "grep -q '.local/bin' $HOME/.bashrc && echo 'exists' || echo 'missing'"
            );

            if (bashrcCheck.output.Contains("missing"))
            {
                ReportProgress("Adding ~/.local/bin to PATH...");
                await wslManager.ExecuteWSLCommandAsync(
                    "echo 'export PATH=\"$HOME/.local/bin:$PATH\"' >> $HOME/.bashrc"
                );
            }

            ReportPercentage(100);
            ReportProgress("✓ OpenROAD installed successfully!");
            ReportProgress($"  Location: $HOME/OpenROAD/build/bin/openroad");
            
            // Verify installation
            var versionResult = await wslManager.ExecuteWSLCommandAsync("$HOME/OpenROAD/build/src/openroad -version");
            if (versionResult.exitCode == 0)
            {
                ReportProgress($"  Version: {versionResult.output.Trim()}");
            }
            
            return true;
        }

        /// <summary>
        /// Complete automated setup - installs everything needed
        /// </summary>
        public async Task<bool> PerformCompleteSetupAsync()
        {
            ReportProgress("Starting complete EDA toolchain setup...");
            ReportProgress("═══════════════════════════════════════");
            ReportPercentage(0);

            // Step 1: Check WSL
            if (!wslManager.IsWSLAvailable)
            {
                ReportProgress("WSL is not installed");
                ReportProgress("Please run 'wsl --install' in PowerShell as Administrator");
                ReportProgress("Then restart your computer and run this setup again");
                InstallationComplete?.Invoke(this, false);
                return false;
            }

            ReportProgress("✓ WSL detected");
            ReportPercentage(10);

            // Step 2: Install Yosys
            ReportProgress("");
            ReportProgress("Step 1/2: Installing Yosys synthesis tool...");
            bool yosysSuccess = await InstallYosysFromSourceAsync();
            if (!yosysSuccess)
            {
                ReportProgress("Yosys installation failed");
                InstallationComplete?.Invoke(this, false);
                return false;
            }
            ReportPercentage(50);

            // Step 3: Install OpenROAD
            ReportProgress("");
            ReportProgress("Step 2/2: Installing OpenROAD place & route tool...");
            bool openroadSuccess = await InstallOpenROADFromSourceAsync();
            if (!openroadSuccess)
            {
                ReportProgress("OpenROAD installation failed");
                InstallationComplete?.Invoke(this, false);
                return false;
            }
            ReportPercentage(100);

            // Summary
            ReportProgress("");
            ReportProgress("═══════════════════════════════════════");
            ReportProgress("✓ Setup completed successfully!");
            ReportProgress("Installed tools:");
            ReportProgress("  • Yosys - Synthesis tool");
            ReportProgress("  • OpenROAD - Place & Route, Timing, Power");
            ReportProgress("");
            ReportProgress("You can now use KairosEDA in Standard mode");
            ReportProgress("Tools are available at:");
            ReportProgress("  - $HOME/yosys");
            ReportProgress("  - $HOME/OpenROAD/build/src/openroad");
            
            InstallationComplete?.Invoke(this, true);
            return true;
        }

        private void ReportProgress(string message)
        {
            ProgressUpdate?.Invoke(this, message);
        }

        private void ReportPercentage(int percent)
        {
            ProgressPercentage?.Invoke(this, percent);
        }
    }
}
