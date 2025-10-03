# Kairos EDA - Developer Guide

## Architecture Overview

### Frontend-Only Design Philosophy

Kairos EDA's frontend is built as a **standalone demonstration layer** that shows what a complete EDA GUI would look like. The backend integration points are intentionally abstracted through the `BackendSimulator` class.

### Component Hierarchy

```
┌─────────────────────────────────────────────────────────┐
│                      MainForm.cs                        │
│  (Main window, layout orchestration, event handling)   │
└────────────────┬────────────────────────────────────────┘
                 │
      ┌──────────┴─────────┬─────────────┬──────────────┐
      │                    │             │              │
┌─────▼──────┐   ┌────────▼──────┐  ┌──▼────────┐  ┌──▼────────────┐
│  Project   │   │   Backend     │  │  Custom   │  │    Dialogs    │
│  Manager   │   │  Simulator    │  │ Controls  │  │  (New/PDK/    │
│            │   │               │  │           │  │  Constraints) │
└────────────┘   └───────────────┘  └───────────┘  └───────────────┘
```

## Key Classes

### 1. MainForm.cs
**Purpose**: Main application window and UI orchestration

**Responsibilities**:
- Layout management (3-panel splitter)
- Menu bar and toolbar creation
- Event routing from UI → Backend
- Status bar updates
- Console log management

**Key Methods**:
- `OnRunSynthesis()` - Triggers synthesis flow
- `OnRunCompleteFlow()` - Runs all stages sequentially
- `LogMessage()` - Adds colored text to console
- `ApplyWindows7Styling()` - Applies Win32 theme

### 2. Win32Native.cs
**Purpose**: Windows 7 Aero glass and native theming

**Key Win32 APIs Used**:
- `DwmExtendFrameIntoClientArea()` - Extends Aero glass effect
- `DwmSetWindowAttribute()` - Configures window composition
- `SetWindowTheme()` - Applies Explorer theme to controls

**Usage Example**:
```csharp
// Apply Aero glass to title bar
Win32Native.ApplyAeroGlass(this.Handle, 30);

// Apply Explorer theme to TreeView
Win32Native.ApplyTreeViewTheme(projectExplorer);
```

### 3. ProjectManager.cs
**Purpose**: Project data model and persistence

**Data Structure**:
```csharp
Project {
  Name, Path, RTLFiles[], PDK,
  Constraints { ClockPeriod, Voltage, Power, ... },
  BuildHistory[]
}
```

**Serialization**: JSON via Newtonsoft.Json

**File Extension**: `.kproj`

### 4. BackendSimulator.cs
**Purpose**: Simulates EDA tool execution for demonstration

**Key Features**:
- Async task execution
- Progress reporting via events
- Stage-specific simulation (synthesis, placement, etc.)
- Cancellation support

**Event System**:
```csharp
event LogReceived      → Console text updates
event ProgressChanged  → Progress bar updates
event StageCompleted   → Report table rows
```

**Simulation Flow**:
```csharp
RunStage("synthesis") 
  → SimulateSynthesis()
    → OnLog("Reading RTL...")
    → OnProgress(40)
    → Task.Delay(200)
    → OnStageComplete("Synthesis", "Gates", "1247")
```

### 5. WorkflowStageControl.cs
**Purpose**: Custom UI control for workflow stage buttons

**Features**:
- Colored accent bar on left
- Run button with icon
- Progress bar (marquee style)
- Status label
- Hover effects

**Visual Design**:
```
┌────┬──────────────────────────────────────────┐
│ ██ │  1. Synthesis                            │
│ ██ │  Converts RTL → Gate-level netlist       │
│ ██ │  [▶ Run]  ▓▓▓▓▓▓▓▓░░░░░░ 60%            │
└────┴──────────────────────────────────────────┘
```

### 6. Dialogs.cs
**Purpose**: Project creation, PDK selection, constraints dialogs

**Dialogs Included**:
- `NewProjectDialog` - Create new project with name/path
- `PDKSelectionDialog` - Choose from 5 PDKs with descriptions
- `ConstraintsDialog` - Set timing/power/area constraints

## Windows 7 Styling Deep Dive

### Aero Glass Effect

The application uses the Desktop Window Manager (DWM) API to extend Aero glass into the client area:

```csharp
[DllImport("dwmapi.dll")]
private static extern int DwmExtendFrameIntoClientArea(
    IntPtr hWnd, ref MARGINS pMarInset);

struct MARGINS {
    int leftWidth, rightWidth, topHeight, bottomHeight;
}

// Extend glass 30 pixels into title bar
MARGINS margins = new MARGINS { topHeight = 30 };
DwmExtendFrameIntoClientArea(windowHandle, ref margins);
```

### Explorer Theme

TreeViews and ListViews use the Explorer visual style for a polished look:

```csharp
[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
private static extern int SetWindowTheme(
    IntPtr hWnd, string pszSubAppName, string pszSubIdList);

SetWindowTheme(treeView.Handle, "explorer", null);
```

### System-Rendered Controls

Menus and toolbars use `RenderMode.System` to leverage native Windows rendering:

```csharp
menuStrip.RenderMode = ToolStripRenderMode.System;  // Windows 7 style
toolStrip.RenderMode = ToolStripRenderMode.System;
```

## Event Flow Diagram

```
User clicks "Run Synthesis"
         │
         ▼
MainForm.OnRunSynthesis()
         │
         ▼
backendSimulator.RunStage("synthesis", project)
         │
         ▼
BackendSimulator.SimulateSynthesis()
         │
         ├──► OnLog("Reading RTL...") ──► LogReceived event
         │                                      │
         │                                      ▼
         │                            MainForm.OnLogReceived()
         │                                      │
         │                                      ▼
         │                            consoleLog.AppendText(...)
         │
         ├──► OnProgress("Synthesis", 60) ──► ProgressChanged event
         │                                          │
         │                                          ▼
         │                                MainForm.OnProgressChanged()
         │                                          │
         │                                          ▼
         │                                statusBar update
         │
         └──► OnStageComplete(...) ──► StageCompleted event
                                            │
                                            ▼
                                  MainForm.OnStageCompleted()
                                            │
                                            ▼
                                  reportGrid.Rows.Add(...)
```

## Console Log Color Coding

```csharp
enum LogLevel {
    Info    → Gray     (220, 220, 220)
    Warning → Yellow   (255, 200, 100)
    Error   → Red      (255, 100, 100)
    Success → Green    (100, 255, 150)
    Stage   → Blue     (100, 200, 255)
}
```

## Backend Abstraction Layer

### Current Implementation (Simulation)
```csharp
BackendSimulator.RunStage("synthesis", project);
// Internally: await Task.Delay() + fake progress
```

### Future Implementation (Real Backend)
```csharp
BackendClient.RunStage("synthesis", project);
// Internally: HTTP request to backend service
// Backend runs: yosys -c synth.tcl
// Backend streams: logs, progress, results
```

### API Contract
```csharp
interface IBackend {
    void RunStage(string stage, Project project);
    void RunAnalysis(string type, Project project);
    void Stop();
    
    event EventHandler<LogEventArgs> LogReceived;
    event EventHandler<ProgressEventArgs> ProgressChanged;
    event EventHandler<StageCompletedEventArgs> StageCompleted;
}
```

This abstraction means the GUI **never hardcodes tool commands**. Changing from Yosys to a proprietary synthesizer requires zero frontend changes.

## EDA Flow Stage Details

### 1. Synthesis
**Tool**: Yosys (simulated)
**Input**: Verilog RTL files
**Output**: Gate-level netlist
**Metrics**: Gate count, FF count, area estimate

### 2. Floorplan
**Tool**: OpenROAD (simulated)
**Input**: Netlist, PDK, area constraints
**Output**: Die area, I/O placement, power grid
**Metrics**: Die area (mm²), utilization target

### 3. Placement
**Tool**: OpenROAD (simulated)
**Input**: Netlist, floorplan
**Output**: Cell coordinates
**Metrics**: Utilization %, HPWL (half-perimeter wire length)

### 4. Clock Tree Synthesis
**Tool**: OpenROAD (simulated)
**Input**: Placed netlist, clock constraints
**Output**: Clock tree topology
**Metrics**: Clock skew, buffer count

### 5. Routing
**Tool**: OpenROAD (simulated)
**Input**: Placed netlist with clock tree
**Output**: Metal layer connections
**Metrics**: Wire length, via count, congestion

### 6. Verification
**Tools**: Magic DRC, Netgen LVS (simulated)
**Input**: Final layout (GDS)
**Output**: DRC violations, LVS match status
**Metrics**: Violation count, LVS result

## Extending the GUI

### Adding a New Workflow Stage

1. Add menu item in `CreateMenu()`:
```csharp
flowMenu.DropDownItems.Add("Run &NewStage", null, OnRunNewStage);
```

2. Add toolbar button in `CreateToolbar()`:
```csharp
mainToolbar.Items.Add(CreateToolButton("New", "Run New Stage", OnRunNewStage));
```

3. Add workflow panel stage in `CreateWorkflowPanel()`:
```csharp
CreateWorkflowStage("7. New Stage", "Description", OnRunNewStage, Color.FromArgb(...));
```

4. Add event handler in `MainForm.cs`:
```csharp
private void OnRunNewStage(object? sender, EventArgs e)
{
    LogMessage("=== Starting New Stage ===", LogLevel.Stage);
    backendSimulator.RunStage("newstage", projectManager.CurrentProject);
}
```

5. Add simulation in `BackendSimulator.cs`:
```csharp
case "newstage":
    await SimulateNewStage(project, token);
    break;
```

### Adding a New Analysis Tool

```csharp
// In MainForm.cs
private void OnCustomAnalysis(object? sender, EventArgs e)
{
    backendSimulator.RunAnalysis("custom", projectManager.CurrentProject);
}

// In BackendSimulator.cs
case "custom":
    OnLog("Running custom analysis...", LogLevel.Info);
    await Task.Delay(1000);
    OnLog("Results: XYZ", LogLevel.Success);
    break;
```

### Adding a New Dialog

```csharp
public class CustomDialog : Form
{
    public CustomDialog()
    {
        Text = "Custom Settings";
        Size = new Size(400, 300);
        // ... add controls
    }
}

// Usage in MainForm.cs
var dialog = new CustomDialog();
if (dialog.ShowDialog() == DialogResult.OK)
{
    // Process results
}
```

## Performance Considerations

### Console Log Buffering
The `RichTextBox` console can become slow with thousands of lines. Consider:
- Limiting to last 10,000 lines
- Using virtual mode
- Offloading to file with "View Full Log" button

### Progress Updates
Progress events are throttled to ~100ms intervals to avoid UI flooding.

### Async Execution
All backend simulation uses `async/await` to keep UI responsive.

## Testing Strategy

### Unit Tests (Future)
- `ProjectManager`: Load/Save validation
- `BackendSimulator`: Event firing sequence
- `Constraints`: Validation ranges

### Integration Tests
- Full flow execution
- Project persistence
- Console log color coding

### UI Tests
- Dialog workflows
- Menu/toolbar actions
- Keyboard shortcuts

## Deployment

### Building for Release
```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

This creates a standalone `.exe` with no .NET runtime dependency.

### Installer (Future)
- Use WiX Toolset for MSI installer
- Register `.kproj` file association
- Add Start Menu shortcuts
- Include sample projects

## Known Limitations

1. **Simulation Only**: No actual tool execution
2. **No GDS Viewer**: Would integrate KLayout or custom viewer
3. **No TCL Editor**: Expert mode would include syntax-highlighted editor
4. **No Remote Execution**: Desktop-only, no cloud/cluster support
5. **Windows Only**: Win32 APIs limit to Windows platform

## Future Roadmap

### Phase 1: Backend Integration
- REST API client for backend communication
- WebSocket for real-time log streaming
- File upload/download for RTL and results

### Phase 2: Visualization
- Timing path viewer with waveforms
- Power heatmap overlay
- Interactive floorplan editor
- GDS layout viewer (KLayout integration)

### Phase 3: Advanced Features
- Multi-project workspace tabs
- Git integration for version control
- Synthesis comparison (before/after)
- Custom script editor (TCL/Python)
- Plugin architecture

### Phase 4: Cloud/Remote
- SSH connection to Linux build servers
- Distributed compute support
- Collaborative design (multi-user)

## Contributing Guidelines

1. **Code Style**: Follow Microsoft C# conventions
2. **Naming**: PascalCase for public, camelCase for private
3. **Comments**: XML docs for public APIs
4. **Events**: Use EventArgs-derived classes
5. **Win32**: Keep all P/Invoke in `Win32Native.cs`
6. **Async**: Use async/await, avoid `.Result`

## Resources

- [Yosys Documentation](https://yosyshq.net/yosys/)
- [OpenROAD Documentation](https://openroad.readthedocs.io/)
- [SkyWater PDK](https://github.com/google/skywater-pdk)
- [DWM API Reference](https://learn.microsoft.com/en-us/windows/win32/dwm/dwm-overview)
- [Windows Forms Guide](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/)

## License

MIT License - See `LICENSE` file for details.

---

**Happy Coding!** 🚀

For questions or contributions, open an issue on GitHub.
