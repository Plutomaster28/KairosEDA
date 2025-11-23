# Kairos EDA - Electronic Design Automation Suite

A modern, user-friendly Windows GUI frontend for RTL-to-GDSII electronic design automation workflows.

![Windows 7 Styled](https://img.shields.io/badge/Style-Windows%207%20Aero-blue)
![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

## 🎯 Overview

Kairos EDA provides a comprehensive graphical interface for managing chip design workflows, from Verilog RTL to GDSII tape-out. **Now with real EDA tool integration!**

The application integrates with industry-standard open-source tools:
- **Yosys** for RTL synthesis
- **OpenROAD** for place & route
- **Magic** for DRC and layout
- **Netgen** for LVS verification

### Key Features

- **⚡ Real EDA Integration** - Direct integration with Yosys, OpenROAD, Magic, Netgen
- **🎨 Windows 7 Aero Styling** - Native Windows 7 glass effects using Win32 API
- **📊 Real-time Workflow Monitoring** - Live progress bars, logs, and tool output
- **🔧 Project Management** - Save/load projects with all settings and history
- **📈 Visualization** - Reports, timing analysis, and violation tracking
- **🎓 Dual Mode Interface** - Beginner mode (guided) and Expert mode (full control)
- **� Docker Support** - Pre-configured toolchain for Windows users
- **🔌 Cross-Platform** - Windows (via WSL/Docker), Linux, macOS

## 🏗️ Architecture

### What This Frontend Does

1. **User Interaction**
   - Import Verilog RTL files
   - Select PDK (Sky130, GF180, ASAP7, FreePDK45, Meisei)
   - Configure constraints (clock, power, area, routing)
   - Switch between Beginner/Expert modes

2. **Workflow Orchestration**
   - Dispatch high-level commands to backend
   - Example: `Backend.Run("synthesis", project_config)`
   - No hardcoded tool commands - fully abstracted

3. **Real-time Monitoring**
   - Live console log with syntax highlighting
   - Progress bars for each stage
   - Statistics display (gates, area, timing, power)

4. **Visualization & Reports**
   - Tabular reports for each stage
   - Timing analysis results
   - DRC/LVS violation tracking
   - Export to PDF/CSV (future)

5. **Project Management**
   - JSON-based project files (.kproj)
   - Version history of builds
   - Settings persistence

### EDA Flow Stages

The GUI demonstrates a complete 6-stage RTL-to-GDSII flow:

1. **Synthesis** - RTL → Gate-level netlist (Yosys)
2. **Floorplan** - Define chip area and I/O placement (OpenROAD)
3. **Placement** - Position standard cells (OpenROAD)
4. **Clock Tree Synthesis** - Build clock distribution network (OpenROAD)
5. **Routing** - Connect cells with metal layers (OpenROAD)
6. **Verification** - DRC/LVS checks (Magic/Netgen)

## 🚀 Getting Started

### Prerequisites

- Windows 7 or later (Windows 10/11 recommended)
- .NET 8.0 SDK
- Visual Studio 2022 (or VS Code with C# extension)

### Building the Project

```powershell
# Clone the repository
cd "C:\Users\theni\OneDrive\Documents\KairosEDA"

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run --project KairosEDA\KairosEDA.csproj
```

### Quick Start

1. Launch Kairos EDA
2. Click **File → New Project**
3. Name your project and choose a location
4. Click **Project → Import Verilog** to add RTL files
5. Click **Project → Select PDK** to choose a process
6. Click **Project → Set Constraints** to configure timing/power
7. Click **Flow → Run Complete Flow** to execute all stages
8. View results in the **Console**, **Reports**, and **Timing** tabs

## 📁 Project Structure

```
KairosEDA/
├── KairosEDA.sln              # Visual Studio solution
├── KairosEDA/
│   ├── KairosEDA.csproj       # Project file (.NET 8 WinForms)
│   ├── app.manifest           # Windows compatibility manifest
│   ├── Program.cs             # Entry point
│   ├── Win32Native.cs         # Windows 7 API calls (DWM, UxTheme)
│   ├── MainForm.cs            # Main application window
│   ├── Models/
│   │   ├── ProjectManager.cs  # Project data model & persistence
│   │   └── BackendSimulator.cs # Simulates backend EDA tool execution
│   └── Controls/
│       ├── WorkflowStageControl.cs # Custom workflow stage button
│       └── Dialogs.cs         # Project/PDK/Constraints dialogs
└── README.md
```

## 🎨 Windows 7 Styling Implementation

The application uses direct Win32 API calls to achieve authentic Windows 7 Aero styling:

- **`dwmapi.dll`** - Desktop Window Manager for Aero glass effects
- **`uxtheme.dll`** - Visual styles for controls (Explorer theme)
- **`user32.dll`** - Window composition attributes

Key features:
- Aero glass title bar
- Explorer-themed TreeView and ListView controls
- System-rendered menu bars and toolbars
- Native visual styles for all controls

## ⚡ Backend Integration

KairosEDA now includes **real EDA tool integration**! The backend executes actual synthesis, place & route, and verification flows.

```csharp
// Frontend makes high-level calls
edaBackend.RunStage("synthesis", project);

// Backend generates and executes tool scripts
// → yosys -s synthesis.ys
// → openroad -exit placement.tcl
// → magic -noconsole -dnull drc.tcl
```

### Supported Tools

| Tool | Purpose | Status |
|------|---------|--------|
| **Yosys** | RTL Synthesis | ✅ Integrated |
| **OpenROAD** | Place & Route | ✅ Integrated |
| **Magic** | DRC / GDS | ✅ Integrated |
| **Netgen** | LVS | ✅ Integrated |
| **OpenSTA** | Timing Analysis | 🔄 Planned |
| **KLayout** | GDS Viewer | 🔄 Planned |

### Setup Options

1. **Docker** (Recommended for Windows) - Pre-configured OpenLane container
2. **WSL2** - Run Linux tools on Windows
3. **Native** - Direct installation on Linux/macOS

See [BACKEND_INTEGRATION.md](BACKEND_INTEGRATION.md) for detailed setup instructions

## 🎓 Mode System

### Beginner Mode
- Pre-configured settings for common designs
- Simplified workflow with guidance
- Automatic parameter selection
- Recommended for learning and quick prototyping

### Expert Mode
- Full control over all parameters
- TCL script injection support
- Advanced optimization knobs
- Custom tool command overrides

## 📊 Example Workflow Output

When you run synthesis, the console displays:

```
[14:23:15] === Starting Synthesis ===
[14:23:15] Backend.Run("synthesis", project_config)
[14:23:15] PDK: Sky130
[14:23:15] RTL Files: 3 file(s)
[14:23:15] Invoking Yosys synthesis engine...
[14:23:16] Reading RTL files...
[14:23:17] Elaborating design hierarchy...
[14:23:18] Technology mapping to Sky130...
[14:23:19] Optimizing logic...
[14:23:20] Synthesis complete!
[14:23:20]   Gates: 1,247
[14:23:20]   Flip-Flops: 128
[14:23:20]   Area estimate: 0.15 mm²
```

The Reports tab shows structured results, and the Progress panel updates in real-time.

## 🛠️ Technologies Used

- **C# / .NET 8.0** - Core application framework
- **Windows Forms** - UI framework
- **Win32 API** - Native Windows styling (dwmapi.dll, uxtheme.dll)
- **Newtonsoft.Json** - Project file serialization
- **System.Drawing** - Graphics and custom controls

## 🔮 Future Enhancements

- [ ] Actual backend integration with Yosys/OpenROAD
- [ ] GDS layout viewer (KLayout integration)
- [ ] Timing/power graph visualizations
- [ ] PDF/CSV report export
- [ ] TCL script editor with syntax highlighting
- [ ] Multi-project workspace support
- [ ] Remote execution on Linux servers
- [ ] Plugin architecture for custom tools
- [ ] Dark mode theme
- [ ] Floorplan interactive editor

## 📝 Configuration File Format

Projects are saved as JSON (.kproj):

```json
{
  "Name": "MyChip",
  "Path": "C:\\Projects\\MyChip",
  "RTLFiles": ["adder.v", "cpu.v"],
  "PDK": "Sky130",
  "Constraints": {
    "ClockPeriodNs": 10.0,
    "VoltageV": 1.8,
    "PowerBudgetMw": 100.0,
    "FloorplanWidthUm": 1000.0,
    "FloorplanHeightUm": 1000.0,
    "Utilization": 0.7,
    "RoutingLayers": 6,
    "ClockPort": "clk"
  }
}
```

## 🤝 Contributing

This is a demonstration frontend. Contributions for:
- UI/UX improvements
- Additional visualizations
- Backend integration layer
- Documentation

are welcome!

## 📄 License

MIT License - See LICENSE file for details

## 🙏 Acknowledgments

- **Yosys** - Open-source RTL synthesis
- **OpenROAD** - Autonomous RTL-to-GDSII flow
- **SkyWater PDK** - Open-source 130nm process
- **Magic VLSI** - Layout tool and DRC
- **Netgen** - LVS verification

---

**Note**: This is a frontend demonstration. Backend EDA tool integration requires additional development. The current implementation simulates tool execution for UI/UX demonstration purposes.

Built with ❤️ for the open-source EDA community.
