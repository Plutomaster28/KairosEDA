# Kairos EDA - Feature Showcase

## 🎨 Visual Design Features

### Windows 7 Aero Glass Styling

The application leverages Windows Desktop Window Manager (DWM) APIs to create an authentic Windows 7 experience:

- **Translucent Title Bar** - Aero glass effect extends into window chrome
- **Explorer-Themed Controls** - TreeViews and ListViews use native Explorer styling
- **System-Rendered Menus** - Native Windows menu rendering for consistency
- **Hover Effects** - Subtle animations on workflow stage panels
- **Color-Coded Console** - Syntax-highlighted logs with per-level coloring

**API Implementation:**
```csharp
// Extend Aero glass 30 pixels into title bar
DwmExtendFrameIntoClientArea(handle, new MARGINS { topHeight = 30 });

// Apply Explorer visual theme
SetWindowTheme(treeView.Handle, "explorer", null);
```

---

## 🎯 Core Features

### 1. Project Management

**Create New Projects**
- Wizard-based project creation
- Configurable project location
- Automatic directory structure generation

**Save/Load Projects**
- JSON-based `.kproj` format
- Human-readable and git-friendly
- Preserves all settings and history

**Project Explorer**
- Tree view of project structure
- RTL files, constraints, PDK, results
- Context menus for quick actions

**Example Project JSON:**
```json
{
  "Name": "MyChip",
  "RTLFiles": ["adder.v", "cpu.v"],
  "PDK": "Sky130",
  "Constraints": { "ClockPeriodNs": 10.0, ... }
}
```

---

### 2. RTL Import & Management

**Verilog Support**
- Import `.v` and `.sv` files
- Multi-file selection
- Automatic dependency detection (future)

**Supported Languages** (Future):
- Verilog
- SystemVerilog
- VHDL
- Chisel (via Firrtl)

**File Organization:**
- Grouped by type in project explorer
- Quick access from tree view
- External editor integration

---

### 3. PDK Selection

**Supported PDKs:**

| PDK | Node | Voltage | Layers | Type |
|-----|------|---------|--------|------|
| **Sky130** | 130nm | 1.8V/3.3V/5V | 5 metal | Open source |
| **GF180** | 180nm | 1.8V-6V | 6 metal | Open source |
| **ASAP7** | 7nm | 0.7V | FinFET | Academic |
| **FreePDK45** | 45nm | 1.1V | Standard | Academic |
| **Meisei** | Custom | TBD | TBD | Coming soon |

**PDK Information Display:**
- Detailed specs for each PDK
- Voltage ranges
- Metal layer count
- Technology notes

---

### 4. Design Constraints

**Configurable Parameters:**

**Timing Constraints:**
- Clock period (ns) - Target frequency
- Setup/hold margins
- Clock port name

**Power Constraints:**
- Supply voltage (V)
- Power budget (mW)
- Power domains (future)

**Physical Constraints:**
- Floorplan width × height (µm)
- Target utilization (0.0-1.0)
- Core-to-die ratio (future)

**Routing Constraints:**
- Number of routing layers
- Via stack limits (future)
- Track spacing rules (future)

**Example Values (100 MHz design):**
```
Clock Period: 10.0 ns
Voltage: 1.8 V
Power: 100 mW
Floorplan: 1000×1000 µm
Utilization: 70%
Layers: 6
```

---

### 5. EDA Workflow Execution

**6-Stage RTL-to-GDSII Flow:**

#### Stage 1: Synthesis
- **Tool**: Yosys (simulated)
- **Input**: Verilog RTL
- **Output**: Gate-level netlist
- **Metrics**: Gate count, FF count, area estimate
- **Duration**: ~2-5 seconds (simulated)

**Simulation Output:**
```
Reading RTL files...
Elaborating design hierarchy...
Technology mapping to Sky130...
Optimizing logic...
✓ Synthesis complete!
  Gates: 1,247
  Flip-Flops: 128
  Area: 0.15 mm²
```

#### Stage 2: Floorplan
- **Tool**: OpenROAD (simulated)
- **Input**: Netlist, PDK
- **Output**: Die area, I/O placement, power grid
- **Metrics**: Die area, utilization
- **Duration**: ~2-3 seconds (simulated)

#### Stage 3: Placement
- **Tool**: OpenROAD (simulated)
- **Input**: Netlist, floorplan
- **Output**: Cell coordinates
- **Metrics**: Utilization %, HPWL
- **Duration**: ~3-5 seconds (simulated)

#### Stage 4: Clock Tree Synthesis (CTS)
- **Tool**: OpenROAD (simulated)
- **Input**: Placed netlist
- **Output**: Clock tree topology
- **Metrics**: Clock skew, buffer count
- **Duration**: ~2-3 seconds (simulated)

#### Stage 5: Routing
- **Tool**: OpenROAD (simulated)
- **Input**: Placed + CTS netlist
- **Output**: Metal layer connections
- **Metrics**: Wire length, via count, congestion
- **Duration**: ~4-6 seconds (simulated)

#### Stage 6: Verification (DRC/LVS)
- **Tool**: Magic, Netgen (simulated)
- **Input**: Final layout (GDS)
- **Output**: Violation reports, LVS match
- **Metrics**: DRC count, LVS status
- **Duration**: ~2-4 seconds (simulated)

**Complete Flow Duration**: ~20-30 seconds (simulated)

---

### 6. Real-Time Monitoring

**Console Log Panel:**
- **Color Coding**: Info (gray), Warning (yellow), Error (red), Success (green), Stage (blue)
- **Timestamps**: `[HH:mm:ss]` prefix
- **Auto-Scroll**: Keeps latest messages visible
- **Search**: Find in logs (future)
- **Export**: Save to file (future)

**Example Log:**
```
[14:23:15] === Starting Synthesis ===
[14:23:15] Backend.Run("synthesis", project_config)
[14:23:16] Reading RTL files...
[14:23:17] Elaborating design hierarchy...
[14:23:18] Technology mapping to Sky130...
[14:23:19] ✓ Synthesis complete!
```

**Progress Panel:**
- Overall flow progress bar
- Real-time statistics display
- Live metric updates

**Status Bar:**
- Current status message
- Project name indicator
- Backend activity status

---

### 7. Results & Reports

**Reports Tab:**
Tabular view of all stage metrics:

| Stage | Metric | Value | Status |
|-------|--------|-------|--------|
| Synthesis | Gate Count | 1,247 | ✓ Pass |
| Synthesis | FF Count | 128 | ✓ Pass |
| Synthesis | Area | 0.15 mm² | ✓ Pass |
| Placement | Utilization | 68.5% | ✓ Pass |
| Routing | Wire Length | 45.7 mm | ✓ Pass |
| Verification | DRC | 0 violations | ✓ Pass |
| Verification | LVS | MATCH | ✓ Pass |

**Timing Tab:**
- Critical path analysis
- Worst Negative Slack (WNS)
- Total Negative Slack (TNS)
- Clock constraint summary

**Violations Tab:**
- DRC violation details (type, location, severity)
- LVS mismatch reports
- Routing congestion warnings

**Export Options** (Future):
- PDF report generation
- CSV data export
- HTML summary page

---

### 8. Analysis Tools

**Timing Analysis:**
```
Running timing analysis...
WNS: -0.15 ns
TNS: -2.3 ns
Critical Path: clk -> FF_reg[7]/D (15 levels)
```

**Power Analysis:**
```
Total Power: 45.7 mW
  Dynamic: 38.2 mW
  Leakage: 7.5 mW
```

**DRC Check:**
```
Found 3 violations:
  - Metal1 spacing (2 instances)
  - Via enclosure (1 instance)
```

**LVS Check:**
```
Layout vs Schematic: MATCH ✓
```

---

### 9. Mode System

**Beginner Mode:**
- ✅ Pre-configured settings
- ✅ Simplified workflow
- ✅ Guided tooltips
- ✅ Recommended values
- ❌ Advanced parameters hidden
- ❌ TCL injection disabled

**Expert Mode:**
- ✅ Full parameter control
- ✅ Advanced optimization knobs
- ✅ TCL script injection
- ✅ Custom tool commands
- ✅ Debug output
- ✅ Performance profiling

**Mode Switching:**
- View → Beginner Mode
- View → Expert Mode
- Instant UI reconfiguration

---

### 10. User Experience Features

**Workflow Stage Panels:**
- **Visual Design**: Colored accent bar per stage
- **Run Button**: One-click execution
- **Progress Bar**: Marquee animation during execution
- **Status Label**: Ready / Running / Complete / Error
- **Hover Effect**: Subtle background color change

**Stage Color Scheme:**
- Synthesis: Steel Blue (#4682B4)
- Floorplan: Sea Green (#3CB371)
- Placement: Dark Orange (#FF8C00)
- CTS: Medium Orchid (#BA55D3)
- Routing: Crimson (#DC143C)
- Verification: Gold (#FFD700)

**Keyboard Shortcuts** (Planned):
- `Ctrl+N` - New Project
- `Ctrl+O` - Open Project
- `Ctrl+S` - Save Project
- `F5` - Run Complete Flow
- `Ctrl+Shift+S` - Synthesis
- `Esc` - Stop Flow

---

## 🚀 Performance Features

### Asynchronous Execution
- Non-blocking UI during backend execution
- Responsive interface even under load
- Cancellable long-running operations

### Event-Driven Architecture
- Decoupled components via event system
- Efficient message passing
- No polling overhead

### Optimized Rendering
- Double-buffered controls
- Efficient console log updates
- Minimal redraws

---

## 🔌 Backend Abstraction

**High-Level API:**
```csharp
// Frontend makes abstract calls
BackendSimulator.RunStage("synthesis", project);
BackendSimulator.RunAnalysis("timing", project);
```

**Backend Independence:**
- No hardcoded tool paths
- No direct tool command execution
- Swappable backend implementations

**Future Backend Types:**
- Local execution (direct tool calls)
- Remote execution (SSH to Linux server)
- Cloud execution (AWS/Azure/GCP)
- Hybrid (local GUI, remote compute)

---

## 📊 Visualization Features

**Console:**
- Color-coded log levels
- Timestamp prefixes
- Auto-scrolling
- Monospace font (Consolas)

**Reports:**
- Sortable data grid
- Filterable columns (future)
- Export to CSV/PDF (future)

**Charts** (Future):
- Area utilization bar chart
- Power breakdown pie chart
- Timing histogram
- Congestion heatmap

---

## 🛠️ Developer Features

**Code Architecture:**
- Clean separation of concerns
- Event-driven design
- Easily extensible
- Well-documented APIs

**Custom Controls:**
- Reusable WorkflowStageControl
- Themeable dialogs
- Custom drawing support

**Plugin Architecture** (Future):
- IPlugin interface
- Dynamic plugin loading
- Custom tool integration
- User scripts

---

## 🔮 Future Features (Roadmap)

### Phase 1: Core Enhancement
- [ ] Real backend integration (Yosys, OpenROAD)
- [ ] GDS layout viewer (KLayout)
- [ ] Timing waveform viewer
- [ ] Interactive floorplan editor

### Phase 2: Advanced Analysis
- [ ] Power heatmap visualization
- [ ] Congestion analysis viewer
- [ ] Critical path highlighting
- [ ] What-if scenario comparison

### Phase 3: Collaboration
- [ ] Multi-user support
- [ ] Git integration
- [ ] Comment/annotation system
- [ ] Shared result dashboards

### Phase 4: Cloud & Scale
- [ ] Remote execution on Linux clusters
- [ ] Distributed parallel routing
- [ ] Cloud storage integration
- [ ] Real-time collaboration

---

## 📚 Documentation Features

**Built-in Help:**
- Tooltips on all controls
- Context-sensitive help (F1)
- Tutorial walkthroughs
- Example projects

**External Resources:**
- README.md - Project overview
- QUICK_START.md - 5-minute guide
- DEVELOPER_GUIDE.md - Architecture docs
- PROJECT_STRUCTURE.md - File organization

---

## 🎯 Target Users

**Students:**
- Learn EDA concepts
- Experiment with flows
- Visualize design steps

**Researchers:**
- Rapid prototyping
- Algorithm evaluation
- Tool comparison

**Professionals:**
- Quick design iterations
- Constraint exploration
- Flow debugging

**Hobbyists:**
- Open-source chip design
- Tapeout preparation
- Learning platform

---

## 🏆 Competitive Advantages

**vs. Command-Line Tools:**
- ✅ Visual feedback
- ✅ No script writing required
- ✅ Real-time progress
- ✅ Beginner-friendly

**vs. Commercial EDA GUIs:**
- ✅ Free and open source
- ✅ Modern Windows UI
- ✅ Lightweight and fast
- ✅ Customizable and extensible

**vs. Web-Based Tools:**
- ✅ Native performance
- ✅ No internet required
- ✅ Full Windows integration
- ✅ Secure (local execution)

---

**Kairos EDA** - Bringing professional EDA workflows to everyone! 🚀
