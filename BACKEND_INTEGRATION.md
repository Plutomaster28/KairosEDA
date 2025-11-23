# KairosEDA Backend Integration Guide

## Overview

KairosEDA now integrates with real EDA tools for a complete ASIC design flow:

- **Yosys** - RTL synthesis
- **OpenROAD** - Floorplanning, placement, CTS, routing
- **Magic** - DRC checking and GDS generation
- **Netgen** - LVS verification

## Architecture

### Backend Components

1. **EdaToolchain** (`Models/EdaToolchain.cs`)
   - Manages tool paths and configuration
   - Auto-detects tools in system PATH
   - Executes tool processes and captures output
   - Supports Docker-based toolchain (recommended for Windows)

2. **EdaBackend** (`Models/EdaBackend.cs`)
   - Orchestrates the complete ASIC design flow
   - Replaces the old `BackendSimulator` with real tool execution
   - Manages project output directories
   - Handles stage dependencies

3. **ToolchainSetupDialog** (`Controls/ToolchainSetupDialog.cs`)
   - GUI for configuring tool paths
   - Auto-detection feature
   - Docker configuration option

## Setup Instructions

### Option 1: Docker (Recommended for Windows)

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/)
2. Pull the OpenLane container (includes all tools):
   ```bash
   docker pull efabless/openlane:latest
   ```
3. In KairosEDA, go to **Tools → Toolchain Setup**
4. Check "Use Docker"
5. Ensure Docker image is set to `efabless/openlane:latest`
6. Click Save

### Option 2: Native Installation

#### Windows (WSL2)

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
