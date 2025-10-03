# Kairos EDA - Complete Project Structure

```
KairosEDA/
│
├── 📄 KairosEDA.sln                    # Visual Studio solution file
├── 📄 README.md                        # Main project documentation
├── 📄 QUICK_START.md                   # Quick start guide for users
├── 📄 DEVELOPER_GUIDE.md               # Comprehensive developer documentation
├── 📄 LICENSE                          # MIT License
├── 📄 .gitignore                       # Git ignore patterns
├── 📄 build-and-run.bat                # Windows batch build script
├── 📄 build-and-run.ps1                # PowerShell build script
│
├── 📁 KairosEDA/                       # Main application project
│   │
│   ├── 📄 KairosEDA.csproj             # C# project file (.NET 8.0)
│   ├── 📄 app.manifest                 # Windows compatibility manifest
│   ├── 📄 Program.cs                   # Application entry point
│   ├── 📄 MainForm.cs                  # Main window and UI orchestration
│   ├── 📄 Win32Native.cs               # Windows 7 API (DWM, UxTheme)
│   │
│   ├── 📁 Models/                      # Data models and business logic
│   │   ├── 📄 ProjectManager.cs        # Project model, load/save logic
│   │   └── 📄 BackendSimulator.cs      # Backend simulation (events)
│   │
│   ├── 📁 Controls/                    # Custom UI controls
│   │   ├── 📄 WorkflowStageControl.cs  # Workflow stage panel
│   │   └── 📄 Dialogs.cs               # New Project, PDK, Constraints dialogs
│   │
│   ├── 📁 Resources/                   # Application resources (future)
│   │   └── 📄 kairos.ico               # Application icon (to be added)
│   │
│   ├── 📁 bin/                         # Build output (Debug/Release)
│   │   └── ...                         # Generated executables
│   │
│   └── 📁 obj/                         # Build intermediate files
│       └── ...
│
├── 📁 Examples/                        # Sample Verilog RTL files
│   ├── 📄 adder_4bit.v                 # 4-bit ripple carry adder
│   ├── 📄 counter_8bit.v               # 8-bit counter with enable
│   └── 📄 traffic_light.v              # Traffic light FSM controller
│
└── 📁 Docs/                            # Additional documentation (future)
    ├── 📄 Screenshots/                 # UI screenshots
    ├── 📄 Architecture.md              # Architecture deep dive
    └── 📄 API_Reference.md             # Backend API specification
```

## File Descriptions

### Root Level

#### Solution Files
- **KairosEDA.sln** - Visual Studio solution that ties all projects together
- **.gitignore** - Ignores build artifacts, user files, and project outputs

#### Documentation
- **README.md** - Overview, features, installation, usage
- **QUICK_START.md** - 5-minute tutorial for new users
- **DEVELOPER_GUIDE.md** - Architecture, APIs, contribution guide
- **LICENSE** - MIT open source license

#### Build Scripts
- **build-and-run.bat** - Simple double-click Windows batch script
- **build-and-run.ps1** - PowerShell script with colored output

### KairosEDA/ - Main Application

#### Core Files
- **KairosEDA.csproj** - .NET 8.0 Windows Forms project
  - Targets: `net8.0-windows`
  - Framework: `UseWindowsForms=true`
  - Packages: Newtonsoft.Json

- **app.manifest** - Windows application manifest
  - DPI awareness settings
  - OS compatibility (Win7/8/10/11)
  - Common Controls v6 for theming

- **Program.cs** - Application entry point
  - `[STAThread]` for COM threading
  - Enables visual styles
  - Applies Windows 7 theme
  - Launches MainForm

- **Win32Native.cs** - P/Invoke Win32 APIs
  - `dwmapi.dll` - Aero glass effects
  - `uxtheme.dll` - Control theming (Explorer style)
  - `user32.dll` - Window messages

- **MainForm.cs** - Main application window (1000+ lines)
  - 3-panel layout (project explorer, workflow, console)
  - Menu bar, toolbar, status bar
  - Event routing and orchestration
  - Console log management

#### Models/ - Data Layer

- **ProjectManager.cs** - Project data model
  - `Project` class: name, path, RTL files, PDK, constraints
  - `Constraints` class: clock, voltage, power, area
  - JSON serialization (.kproj files)
  - File I/O operations

- **BackendSimulator.cs** - Simulates EDA tool execution
  - Async stage execution (synthesis, placement, routing, etc.)
  - Event system: LogReceived, ProgressChanged, StageCompleted
  - Simulates realistic tool output with delays
  - Cancellation support

#### Controls/ - Custom UI

- **WorkflowStageControl.cs** - Custom Panel-derived control
  - Colored accent bar (per-stage theme)
  - Run button with icon
  - Progress bar (marquee style)
  - Status label (Ready/Complete/Error)
  - Hover effects

- **Dialogs.cs** - Modal dialog forms
  - `NewProjectDialog` - Create project with name/path browser
  - `PDKSelectionDialog` - Choose from 5 PDKs with descriptions
  - `ConstraintsDialog` - Set timing/power/area parameters

### Examples/ - Sample Designs

- **adder_4bit.v** - Simple combinational logic
  - Good for testing synthesis
  - ~15 gates, no FFs

- **counter_8bit.v** - Sequential logic with clock
  - Tests floorplan and placement
  - ~50 gates, 8 FFs

- **traffic_light.v** - State machine (FSM)
  - Complex control logic
  - Tests full flow including routing

## Build Artifacts (Auto-Generated)

```
KairosEDA/bin/Debug/net8.0-windows/
├── KairosEDA.exe                  # Main executable
├── KairosEDA.dll                  # Application DLL
├── KairosEDA.pdb                  # Debug symbols
├── Newtonsoft.Json.dll            # JSON library
└── KairosEDA.runtimeconfig.json   # Runtime configuration

KairosEDA/obj/Debug/net8.0-windows/
├── KairosEDA.csproj.FileListAbsolute.txt
├── KairosEDA.assets.json
└── ...                            # Build intermediate files
```

## Project File Format (.kproj)

Example of saved project JSON:

```json
{
  "Name": "MyChip",
  "Path": "C:\\Projects\\MyChip",
  "RTLFiles": [
    "C:\\Projects\\MyChip\\adder.v",
    "C:\\Projects\\MyChip\\counter.v"
  ],
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
  },
  "Settings": {},
  "BuildHistory": [
    {
      "Stage": "synthesis",
      "Timestamp": "2025-10-03T14:23:45",
      "Success": true,
      "Metrics": {
        "GateCount": 1247,
        "FFCount": 128,
        "AreaMm2": 0.15
      }
    }
  ],
  "Created": "2025-10-03T10:00:00",
  "LastModified": "2025-10-03T14:30:00"
}
```

## Dependencies

### NuGet Packages
- **Newtonsoft.Json** (13.0.3) - JSON serialization

### Windows APIs
- **dwmapi.dll** - Desktop Window Manager (Aero)
- **uxtheme.dll** - Visual Styles (Explorer theme)
- **user32.dll** - Window management

### .NET Framework
- **.NET 8.0** - Windows Desktop Runtime

## Lines of Code

```
File                          Lines  Purpose
-------------------------------------------------------------------------------------------------
MainForm.cs                   ~800   UI layout, event handling, orchestration
Win32Native.cs                ~150   Windows API P/Invoke wrappers
ProjectManager.cs             ~100   Data model and serialization
BackendSimulator.cs           ~400   EDA tool simulation with events
WorkflowStageControl.cs       ~120   Custom workflow panel control
Dialogs.cs                    ~350   New Project, PDK, Constraints dialogs
Program.cs                    ~20    Application entry point
-------------------------------------------------------------------------------------------------
Total                         ~1940  lines of C# code
```

## Workflow Data Flow

```
User Action (Click "Run Synthesis")
         ↓
MainForm.OnRunSynthesis()
         ↓
BackendSimulator.RunStage("synthesis", project)
         ↓
BackendSimulator.SimulateSynthesis()
         ↓
    ┌────┴────┬────────────┬───────────────┐
    ↓         ↓            ↓               ↓
OnLog()  OnProgress()  OnStageComplete()  Task.Delay()
    ↓         ↓            ↓               ↓
LogReceived ProgressChanged StageCompleted (simulate work)
    ↓         ↓            ↓
    └─────────┴────────────┘
              ↓
    MainForm event handlers
              ↓
    ┌─────────┼─────────┐
    ↓         ↓         ↓
Console   StatusBar  ReportGrid
Update    Update     Add Row
```

## UI Component Hierarchy

```
MainForm (Form)
│
├── MainMenuStrip (MenuStrip)
│   ├── File
│   ├── Project
│   ├── Flow
│   ├── Tools
│   ├── View
│   └── Help
│
├── MainToolbar (ToolStrip)
│   └── [Buttons]
│
├── MainSplitter (SplitContainer)
│   │
│   ├── Panel1: Project Explorer
│   │   ├── Label ("Project Explorer")
│   │   └── TreeView (project structure)
│   │
│   └── Panel2: RightSplitter (SplitContainer)
│       │
│       ├── Panel1: Center Area
│       │   ├── WorkflowPanel (FlowLayoutPanel)
│       │   │   ├── WorkflowStageControl (Synthesis)
│       │   │   ├── WorkflowStageControl (Floorplan)
│       │   │   ├── WorkflowStageControl (Placement)
│       │   │   ├── WorkflowStageControl (CTS)
│       │   │   ├── WorkflowStageControl (Routing)
│       │   │   └── WorkflowStageControl (Verification)
│       │   │
│       │   └── ProgressPanel (Panel)
│       │       ├── Label ("Progress & Statistics")
│       │       ├── ProgressBar (overall)
│       │       └── Label (statistics)
│       │
│       └── Panel2: Right Tabs
│           └── TabControl
│               ├── TabPage: Console
│               │   └── RichTextBox (colored logs)
│               ├── TabPage: Reports
│               │   └── DataGridView (metrics table)
│               ├── TabPage: Timing
│               │   └── RichTextBox (timing analysis)
│               └── TabPage: Violations
│                   └── DataGridView (DRC/LVS)
│
└── StatusBar (StatusStrip)
    ├── ToolStripStatusLabel (status message)
    ├── ToolStripStatusLabel (project name)
    └── ToolStripStatusLabel (backend status)
```

## Future Expansion Points

### Additional Folders (Planned)
```
KairosEDA/
├── Services/               # Backend communication services
│   ├── BackendClient.cs    # REST/WebSocket client
│   └── FileTransfer.cs     # Upload/download RTL and GDS
│
├── Analyzers/              # Analysis result parsers
│   ├── TimingParser.cs     # Parse STA reports
│   ├── PowerParser.cs      # Parse power reports
│   └── DRCParser.cs        # Parse DRC violations
│
├── Visualizers/            # Custom drawing controls
│   ├── FloorplanView.cs    # Interactive floorplan editor
│   ├── TimingGraph.cs      # Timing path viewer
│   └── GDSViewer.cs        # Layout renderer (KLayout integration)
│
└── Plugins/                # Plugin architecture
    ├── IPlugin.cs          # Plugin interface
    └── PluginManager.cs    # Plugin discovery and loading
```

## Key Technologies Summary

| Component | Technology | Purpose |
|-----------|-----------|---------|
| GUI Framework | Windows Forms (.NET 8) | Native Windows UI |
| Styling | Win32 API (dwmapi, uxtheme) | Windows 7 Aero glass |
| Data Model | C# Classes | Project, Constraints, BuildResult |
| Serialization | Newtonsoft.Json | .kproj file format |
| Async | async/await, Task | Non-blocking backend simulation |
| Events | EventHandler<T> | Decoupled communication |
| P/Invoke | DllImport | Native Windows API calls |

---

**Last Updated**: October 3, 2025

This structure provides a solid foundation for a production-ready EDA GUI application!
