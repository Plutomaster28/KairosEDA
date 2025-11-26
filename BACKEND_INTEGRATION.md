# Backend Integration - WSL Toolchain Setup

## Overview

KairosEDA now includes a **complete automated setup system** for installing EDA tools in WSL. The system builds **Yosys** and **OpenROAD** from source, providing full synthesis and place & route capabilities without requiring Docker.

### What Gets Installed

- **Yosys** - RTL synthesis tool
- **OpenROAD** - Complete place & route solution (floorplanning, placement, CTS, routing, timing, power)

## Architecture

### Components

1. **WSLManager** (`Models/WSLManager.cs`)
   - Bridge between Windows and WSL
   - Executes commands via `wsl.exe`
   - Path conversion: `C:\...` ↔ `/mnt/c/...`
   - Real-time output capture

2. **ToolchainValidator** (`Models/ToolchainValidator.cs`)
   - Detects installed EDA tools
   - Three operation modes:
     - **Unavailable**: No tools found - triggers setup wizard
     - **Basic**: OpenLane Docker only (legacy support)
     - **Standard**: Yosys + OpenROAD (native) - full functionality
   - Priority detection paths:
     - `$HOME/OpenROAD/build/bin/openroad`
     - `$HOME/yosys`
     - System PATH

3. **ToolchainInstaller** (`Models/ToolchainInstaller.cs`)
   - Automated source compilation
   - Progress tracking with events
   - Two main installation methods:
     - `InstallYosysFromSourceAsync()` - Builds Yosys from GitHub
     - `InstallOpenROADFromSourceAsync()` - Builds OpenROAD with CMake

4. **SetupWizard** (`Controls/SetupWizard.xaml`)
   - User-friendly installation UI
   - Windows 95 retro aesthetic
   - Live console output during builds
   - Progress bar with percentage

## Automated Setup Process

### First Launch Experience

1. KairosEDA starts and detects no toolchain
2. Dialog prompt: **"Backend toolchain not found. Would you like to start the setup now?"**
3. User clicks **"Yes"** to launch the setup wizard
4. Installation proceeds automatically (30-40 minutes)
5. On completion, KairosEDA enters **Standard Mode**

### Installation Steps

#### 1. Yosys Build (~10 minutes)
- **Source**: `https://github.com/YosysHQ/yosys.git`
- **Build Method**: Makefile with parallel compilation
- **Location**: `$HOME/yosys`
- **Dependencies Installed**:
  - build-essential, clang, bison, flex
  - tcl-dev, libffi-dev, libreadline-dev
  - python3, libboost-system-dev, libboost-python-dev
  - graphviz, xdot, pkg-config

**Build Commands:**
```bash
cd $HOME
git clone --recurse-submodules https://github.com/YosysHQ/yosys.git
cd yosys
make -j$(nproc)
sudo make install  # or: make install PREFIX=$HOME/.local

1. Install WSL2 with Ubuntu:
   ```powershell
   wsl --install
   ```

2. Install tools in WSL:
   ```bash
   # Yosys
   sudo apt install yosys

   # OpenROAD
   sudo apt install openroad

   # Magic
   sudo apt install magic

   # Netgen
   sudo apt install netgen
   ```

3. In KairosEDA, go to **Tools → Toolchain Setup**
4. Set paths to WSL tools:
   ```
   Yosys: wsl yosys
   OpenROAD: wsl openroad
   Magic: wsl magic
   Netgen: wsl netgen
   ```

#### Linux

```bash
# Ubuntu/Debian
sudo apt install yosys openroad magic netgen

# Or build from source
git clone https://github.com/YosysHQ/yosys
git clone https://github.com/The-OpenROAD-Project/OpenROAD
git clone https://github.com/RTimothyEdwards/magic
git clone https://github.com/RTimothyEdwards/netgen
```

#### macOS

```bash
brew install yosys openroad magic netgen
```

## Usage

### Creating a Project

1. **File → New Project**
2. Import Verilog files: **Project → Import Verilog**
3. Select PDK: **Project → Select PDK** (default: Sky130)
4. Set constraints: **Project → Set Constraints**

### Running the Flow

#### Complete Flow
**Flow → Run Complete Flow** or click **▶ Run** toolbar button

This runs all stages sequentially:
1. Synthesis
2. Floorplan
3. Placement
4. Clock Tree Synthesis (CTS)
5. Routing
6. Verification (DRC/LVS)

#### Individual Stages
Run stages independently via the workflow panel or **Flow** menu:
- **Flow → Run Synthesis**
- **Flow → Run Floorplan**
- **Flow → Run Placement**
- etc.

### Output Files

All outputs are in `<project_path>/kairos_output/`:

```
kairos_output/
├── synthesis/
│   ├── netlist.v          # Synthesized gate-level netlist
│   ├── netlist.json       # JSON netlist for analysis
│   └── synthesis.ys       # Yosys script
├── floorplan/
│   ├── floorplan.def      # DEF with floorplan
│   └── floorplan.tcl      # OpenROAD script
├── placement/
│   ├── placement.def
│   └── placement.tcl
├── cts/
│   ├── cts.def
│   └── cts.tcl
├── routing/
│   ├── routed.def         # Final routed design
│   └── routing.tcl
└── verification/
    ├── drc.tcl
    └── lvs_results.txt
```

## Tool Configuration

### Yosys Synthesis

The backend generates Yosys scripts automatically based on:
- RTL files in project
- Selected PDK
- Technology mapping rules

Example generated script:
```tcl
read_verilog design.v
hierarchy -check -top top
proc; opt; fsm; opt; memory; opt
techmap -map +/techmap.v
abc -liberty sky130_fd_sc_hd__tt_025C_1v80.lib
clean
write_verilog netlist.v
write_json netlist.json
stat
```

### OpenROAD Stages

Each stage generates a TCL script:

**Floorplan:**
```tcl
read_lef sky130_fd_sc_hd.tlef
read_lef sky130_fd_sc_hd_merged.lef
read_verilog netlist.v
link_design top
initialize_floorplan -utilization 70 -aspect_ratio 1 -core_space 2
write_def floorplan.def
```

**Placement:**
```tcl
read_def floorplan.def
global_placement
detailed_placement
write_def placement.def
```

### Magic DRC

```tcl
drc on
drc check
drc catchup
set drcresult [drc listall why]
puts "DRC violations: $drcresult"
```

### Netgen LVS

```bash
netgen -batch lvs layout.spice schematic.spice
```

## Troubleshooting

### Tool Not Found

**Symptom:** "Tool not configured" error

**Solution:**
1. Go to **Tools → Toolchain Setup**
2. Click **Auto-Detect**
3. If tools are in PATH, they'll be found automatically
4. Otherwise, browse to executable paths manually

### WSL Path Issues

**Symptom:** Can't find WSL tools

**Solution:**
```
Set paths to: wsl <tool_name>
Example: wsl yosys
```

### Docker Issues

**Symptom:** Docker commands fail

**Solution:**
1. Ensure Docker Desktop is running
2. Test: `docker run hello-world`
3. Pull OpenLane image: `docker pull efabless/openlane:latest`

### PDK Not Found

**Symptom:** Technology library errors

**Solution:**
1. Download Sky130 PDK:
   ```bash
   git clone https://github.com/google/skywater-pdk
   ```
2. Set PDK path in **Toolchain Setup**

## Advanced Features

### Custom TCL Injection

**Expert Mode** (coming soon) will allow:
- Custom Yosys synthesis commands
- OpenROAD TCL script editing
- Manual constraint overrides

### Analysis Tools

- **Tools → Timing Analysis** - Static timing analysis with OpenROAD
- **Tools → Power Analysis** - Power estimation
- **Tools → DRC Check** - Design rule checking
- **Tools → LVS Check** - Layout vs schematic

### Docker Integration

When using Docker mode:
1. All tool commands run inside container
2. Project files mounted automatically
3. Output extracted to host filesystem
4. No need to install tools natively

## Contributing

To add support for new tools or improve existing integrations:

1. Edit `Models/EdaToolchain.cs` for tool wrappers
2. Edit `Models/EdaBackend.cs` for flow orchestration
3. Test with actual PDK files
4. Submit PR with documentation

## References

- [Yosys Documentation](https://yosyshq.readthedocs.io/)
- [OpenROAD Documentation](https://openroad.readthedocs.io/)
- [Magic VLSI](http://opencircuitdesign.com/magic/)
- [Netgen LVS](http://opencircuitdesign.com/netgen/)
- [SkyWater PDK](https://skywater-pdk.readthedocs.io/)
- [OpenLane](https://github.com/efabless/openlane2)

## License

KairosEDA backend integration is open-source under MIT License.
Individual EDA tools have their own licenses (check respective repositories).
