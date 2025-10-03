# Kairos EDA - Quick Start Guide

## 🚀 Get Up and Running in 5 Minutes

### Step 1: Verify Prerequisites

Open PowerShell and check your .NET version:
```powershell
dotnet --version
```

You should see version **8.0.x** or higher. If not, download from: https://dotnet.microsoft.com/download

### Step 2: Build the Project

Navigate to the project directory and run:
```powershell
cd "C:\Users\theni\OneDrive\Documents\KairosEDA"
.\build-and-run.ps1
```

Or simply double-click `build-and-run.bat` in Windows Explorer!

### Step 3: Create Your First Project

1. **Launch the application** - It will open with a Windows 7 Aero-styled window
2. Click **File → New Project**
3. Enter project name: `MyFirstChip`
4. Choose a location (default: Documents\KairosProjects)
5. Click **Create**

### Step 4: Add Design Files

1. Click **Project → Import Verilog**
2. Select your `.v` or `.sv` files (or use example files)
3. Files appear in the **Project Explorer** tree on the left

### Step 5: Configure the Design

1. Click **Project → Select PDK**
2. Choose **Sky130** (recommended for beginners)
3. Click **Select**

4. Click **Project → Set Constraints**
5. Adjust values:
   - **Clock Period**: 10.0 ns (100 MHz)
   - **Supply Voltage**: 1.8 V
   - **Power Budget**: 100 mW
   - **Floorplan Size**: 1000x1000 µm
   - **Utilization**: 0.7 (70%)
6. Click **Apply**

### Step 6: Run the Complete Flow

1. Click the big **▶ Run** button in the toolbar, or
2. Click **Flow → Run Complete Flow**

Watch as the application executes all 6 stages:
- ✅ Synthesis
- ✅ Floorplan
- ✅ Placement
- ✅ Clock Tree Synthesis
- ✅ Routing
- ✅ Verification (DRC/LVS)

### Step 7: View Results

**Console Tab** (bottom right):
- Real-time colored logs
- Stage progress messages
- Error/warning highlighting

**Reports Tab** (bottom right):
- Tabular view of metrics
- Gate counts, area, timing
- Status indicators

**Timing Tab**:
- Critical path analysis
- Slack reports
- Clock constraints

**Violations Tab**:
- DRC violations (should be 0!)
- LVS results
- Routing congestion

### Step 8: Explore Individual Stages

Instead of running the complete flow, try individual stages:

1. **Synthesis Only**: Click the "1. Synthesis" panel's **▶ Run** button
2. **Placement Only**: Click the "3. Placement" panel's **▶ Run** button

Each stage updates independently!

### Step 9: Run Analysis Tools

**Menu → Tools** provides analysis options:

- **Timing Analysis** - Check setup/hold violations
- **Power Analysis** - Dynamic + leakage power breakdown
- **DRC Check** - Design rule verification
- **LVS Check** - Layout vs schematic matching

### Step 10: Save Your Project

Click **File → Save Project** to persist:
- All RTL file paths
- PDK selection
- Constraints
- Build history

The project saves as `MyFirstChip.kproj` (JSON format).

## 🎨 Interface Overview

```
┌─────────────────────────────────────────────────────────────────┐
│ File  Project  Flow  Tools  View  Help                          │ ← Menu
├─────────────────────────────────────────────────────────────────┤
│ [New] [Open] [Save] │ [Import] [PDK] │ [▶ Run] [■ Stop]        │ ← Toolbar
├──────────┬────────────────────────┬─────────────────────────────┤
│ Project  │  Workflow Stages       │  Console / Reports          │
│ Explorer │                        │                             │
│          │  1. Synthesis          │  [Console] [Reports]        │
│ □ RTL    │     [▶ Run] ▓▓░░ 60%   │                             │
│   ├─ a.v │                        │  [12:34:56] Starting...     │
│   └─ b.v │  2. Floorplan          │  [12:34:57] Complete!       │
│ □ PDK    │     [▶ Run]            │                             │
│ □ Results│                        │  Gates: 1,247               │
│          │  3. Placement          │  Area: 0.15 mm²             │
│          │     [▶ Run]            │                             │
│          │                        │  ┌──────┬────────┬────┐    │
│          │  ...                   │  │Stage │ Metric │Val │    │
│          │                        │  ├──────┼────────┼────┤    │
│          │  Progress & Stats      │  │Synth │ Gates  │1247│    │
│          │  ▓▓▓▓▓▓░░░░░░░ 60%     │  └──────┴────────┴────┘    │
└──────────┴────────────────────────┴─────────────────────────────┘
│ Ready | No Project Loaded | Backend: Idle                       │ ← Status
└─────────────────────────────────────────────────────────────────┘
```

## 🎓 Mode Selection

### Beginner Mode (Default)
- Pre-configured settings
- Simplified options
- Guided workflow
- **How to Enable**: View → Beginner Mode

### Expert Mode
- Full parameter control
- TCL script injection
- Advanced optimization
- Custom tool commands
- **How to Enable**: View → Expert Mode

## 🔍 Tips & Tricks

### Keyboard Shortcuts (Coming Soon)
- `Ctrl+N` - New Project
- `Ctrl+O` - Open Project
- `Ctrl+S` - Save Project
- `F5` - Run Complete Flow
- `Ctrl+Shift+S` - Run Synthesis
- `Ctrl+L` - Clear Console

### Console Search
Right-click in console → Find to search logs

### Export Reports (Future)
Reports → Export → CSV/PDF

### View GDS Layout
Tools → View GDS Layout (opens KLayout)

## ❓ Troubleshooting

### Issue: Build fails with "SDK not found"
**Solution**: Install .NET 8 SDK from https://dotnet.microsoft.com/download

### Issue: Windows 7 glass effect not showing
**Solution**: Enable Aero theme in Windows settings. Note: Windows 10/11 will show a flat title bar (by design).

### Issue: No RTL files to import
**Solution**: Use sample Verilog files or create a simple `adder.v`:
```verilog
module adder(input [3:0] a, b, output [3:0] sum);
  assign sum = a + b;
endmodule
```

### Issue: Application crashes on startup
**Solution**: 
1. Check Windows Event Viewer for errors
2. Run from command line: `dotnet run --project KairosEDA\KairosEDA.csproj`
3. Check for missing DLLs (dwmapi.dll, uxtheme.dll)

## 📚 Next Steps

1. **Read the Developer Guide**: `DEVELOPER_GUIDE.md`
2. **Explore the code**: Start with `MainForm.cs`
3. **Customize workflows**: Add your own stages
4. **Integrate real tools**: Replace `BackendSimulator` with actual Yosys/OpenROAD calls

## 🤝 Need Help?

- **Documentation**: See `README.md` and `DEVELOPER_GUIDE.md`
- **Examples**: Check `KairosProjects/` for sample designs
- **Issues**: Open a GitHub issue
- **Discussions**: Join the community forum

---

**Congratulations!** 🎉 You've completed the quick start guide.

Now go build something amazing with Kairos EDA!
