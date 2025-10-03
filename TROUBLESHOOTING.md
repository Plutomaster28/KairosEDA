# Kairos EDA - Troubleshooting Guide

## 🔧 Common Issues & Solutions

### Build & Compilation Issues

#### Issue 1: "SDK 'Microsoft.NET.Sdk' not found"
**Symptom**: Build fails immediately with SDK error

**Solution**:
1. Install .NET 8.0 SDK from https://dotnet.microsoft.com/download
2. Verify installation:
   ```powershell
   dotnet --version
   # Should show 8.0.x or higher
   ```
3. Restart your terminal/IDE
4. Run `dotnet restore` again

---

#### Issue 2: "The type or namespace 'Forms' does not exist"
**Symptom**: Compilation errors about missing Windows Forms

**Solution**:
1. Verify `KairosEDA.csproj` contains:
   ```xml
   <UseWindowsForms>true</UseWindowsForms>
   <TargetFramework>net8.0-windows</TargetFramework>
   ```
2. Run `dotnet restore`
3. Clean and rebuild:
   ```powershell
   dotnet clean
   dotnet build
   ```

---

#### Issue 3: "Newtonsoft.Json not found"
**Symptom**: Build error about missing package

**Solution**:
```powershell
dotnet add package Newtonsoft.Json --version 13.0.3
dotnet restore
```

---

### Runtime Issues

#### Issue 4: Application crashes on startup
**Symptom**: Window appears briefly then closes

**Solution**:
1. Run from command line to see error:
   ```powershell
   dotnet run --project KairosEDA\KairosEDA.csproj
   ```
2. Check for missing DLLs (dwmapi.dll, uxtheme.dll)
3. Ensure Windows version is 7 or later
4. Try disabling Aero glass:
   ```csharp
   // In MainForm.cs, comment out:
   // Win32Native.ApplyAeroGlass(this.Handle, 30);
   ```

---

#### Issue 5: "DllNotFoundException: Unable to load DLL 'dwmapi.dll'"
**Symptom**: Error when applying Windows 7 styling

**Solution**:
This should never happen on Windows 7+, but if it does:
1. Check Windows version: `winver`
2. Ensure Desktop Window Manager service is running:
   ```powershell
   Get-Service -Name UxSms
   # Should show "Running"
   ```
3. As last resort, disable Aero features in `Win32Native.cs`

---

#### Issue 6: Console log text is not colored
**Symptom**: All console text appears in default color

**Solution**:
1. Check `RichTextBox.ReadOnly` is set to `true`
2. Verify `SelectionColor` is being set before `AppendText`:
   ```csharp
   consoleLog.SelectionColor = color;
   consoleLog.AppendText(message);
   ```
3. Try increasing font size if colors are too subtle

---

### UI Issues

#### Issue 7: Windows 7 glass effect not visible
**Symptom**: Title bar is flat, no translucency

**Solution**:
**This is normal on Windows 10/11** - Aero glass is a Windows 7 feature. On newer Windows:
- Title bar will be flat (by design)
- Rest of UI will still look good
- All functionality remains intact

To test on Windows 7:
- Use a VM with Windows 7
- Or accept that glass is not available

---

#### Issue 8: TreeView has no lines/icons
**Symptom**: Project explorer looks plain

**Solution**:
1. Ensure `ShowLines = true` and `ShowRootLines = true`
2. Call `Win32Native.ApplyTreeViewTheme(projectExplorer)` in `OnLoad`
3. Verify theme service is running:
   ```powershell
   Get-Service -Name Themes
   ```

---

#### Issue 9: Menu bar is too small or blurry
**Symptom**: Text is hard to read

**Solution**:
1. Check DPI settings in Windows Display Settings
2. Application manifest handles DPI, but you can adjust:
   ```csharp
   // In Program.cs, add:
   Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
   ```

---

### Functionality Issues

#### Issue 10: "Run Synthesis" button does nothing
**Symptom**: Click button but no logs appear

**Solution**:
1. Verify project is loaded (check status bar)
2. Check event handler is wired:
   ```csharp
   runButton.Click += OnRunSynthesis;
   ```
3. Ensure `BackendSimulator` events are connected:
   ```csharp
   backendSimulator.LogReceived += OnLogReceived;
   ```

---

#### Issue 11: Progress bar doesn't update
**Symptom**: Progress stays at 0% or doesn't move

**Solution**:
1. Check `ProgressChanged` event is wired up
2. Verify `Invoke` is used for cross-thread updates:
   ```csharp
   if (statusBar.InvokeRequired)
       statusBar.Invoke(new Action(() => UpdateProgress(e)));
   ```

---

#### Issue 12: Project won't save
**Symptom**: Save succeeds but file not created

**Solution**:
1. Check write permissions on project folder
2. Verify path contains no invalid characters
3. Check disk space
4. Try saving to a different location

---

#### Issue 13: Imported Verilog files don't appear in tree
**Symptom**: Files selected but not visible

**Solution**:
1. Check `projectManager.AddRTLFile()` is called
2. Verify `TreeView` is refreshed after adding
3. Try expanding the "RTL Files" node manually

---

### Performance Issues

#### Issue 14: Console log becomes slow with many lines
**Symptom**: UI freezes when lots of logs

**Solution**:
1. Limit console to last 10,000 lines:
   ```csharp
   if (consoleLog.Lines.Length > 10000)
   {
       consoleLog.Lines = consoleLog.Lines.Skip(5000).ToArray();
   }
   ```
2. Use virtual scrolling (future enhancement)
3. Add "Clear Console" button

---

#### Issue 15: Application uses too much memory
**Symptom**: Task Manager shows high memory usage

**Solution**:
1. Clear console log periodically
2. Dispose of old project data when loading new project
3. Check for event handler leaks (unsubscribe when done)

---

### Platform-Specific Issues

#### Issue 16: Can't build on Linux/Mac
**Symptom**: Build fails on non-Windows platforms

**Solution**:
**This app is Windows-only by design** (uses Win32 APIs). Options:
1. Use Windows VM
2. Use Windows Subsystem for Linux (WSL) with GUI support
3. Fork and create cross-platform version (remove Win32Native.cs)

---

#### Issue 17: Can't run on Windows XP/Vista
**Symptom**: Application won't start on old Windows

**Solution**:
**Not supported** - Requires Windows 7 or later. Manifest specifies:
```xml
<supportedOS Id="{35138b9a-5d96-4fbd-8e2d-a2440225f93a}" /> <!-- Win7 -->
```

---

## 🔍 Debugging Tips

### Enable Verbose Logging
Add debug output to trace issues:
```csharp
// In OnRunSynthesis()
Console.WriteLine($"[DEBUG] Starting synthesis for project: {projectManager.CurrentProject?.Name}");
```

### Use Visual Studio Debugger
1. Open solution in Visual Studio
2. Set breakpoints in event handlers
3. Press F5 to debug
4. Step through code to find issues

### Check Event Viewer
Windows Event Viewer may have crash details:
1. Open Event Viewer
2. Navigate to "Windows Logs → Application"
3. Look for errors from "KairosEDA.exe"

### Run with Diagnostics
```powershell
# Enable detailed .NET diagnostics
$env:DOTNET_STARTUP_HOOKS="*"
dotnet run --project KairosEDA\KairosEDA.csproj
```

---

## 🆘 Getting Help

### Before Asking for Help

Please gather this information:
1. **Windows Version**: Run `winver` and note the version
2. **.NET Version**: Run `dotnet --version`
3. **Error Message**: Full text of any errors
4. **Steps to Reproduce**: What you did before the error
5. **Screenshots**: If it's a visual issue

### Where to Get Help

1. **Check Documentation**:
   - README.md
   - DEVELOPER_GUIDE.md
   - QUICK_START.md

2. **Search Issues**: Check if someone else had the same problem

3. **Open an Issue**: Provide all gathered information

4. **Community Forums**: Ask in EDA/FPGA communities

---

## 🔄 Reset Everything

If all else fails, start fresh:

```powershell
# Delete all build artifacts
Remove-Item -Recurse -Force KairosEDA\bin
Remove-Item -Recurse -Force KairosEDA\obj

# Restore packages
dotnet restore

# Clean build
dotnet clean
dotnet build --configuration Release

# Run
dotnet run --project KairosEDA\KairosEDA.csproj
```

---

## 🛠️ Known Limitations

### By Design
1. **Windows Only**: Uses Win32 APIs (dwmapi.dll, uxtheme.dll)
2. **Simulation Only**: No real EDA tool execution (demo mode)
3. **No GDS Viewer**: Would require KLayout integration
4. **No Remote Execution**: Desktop-only, no cloud support

### Future Enhancements
1. Cross-platform version (Avalonia or .NET MAUI)
2. Real backend integration
3. Advanced visualizations
4. Plugin architecture

---

## 📞 Support Channels

- **Documentation**: See markdown files in root
- **Code Issues**: Open GitHub issue
- **Feature Requests**: GitHub discussions
- **General Questions**: Community forums

---

## ✅ Verification Checklist

Before reporting an issue, verify:
- [ ] .NET 8.0 SDK installed
- [ ] Windows 7 or later
- [ ] All files present (check PROJECT_STRUCTURE.md)
- [ ] `dotnet restore` completed successfully
- [ ] No firewall/antivirus blocking
- [ ] Write permissions on project folder
- [ ] Sufficient disk space (>500 MB)

---

## 🎯 Quick Fixes

### Fix 1: Rebuild Solution
```powershell
dotnet clean
dotnet restore
dotnet build
```

### Fix 2: Delete bin/obj
```powershell
Remove-Item -Recurse KairosEDA\bin, KairosEDA\obj
dotnet build
```

### Fix 3: Reinstall NuGet Packages
```powershell
Remove-Item -Recurse ~/.nuget/packages/newtonsoft.json
dotnet restore
```

### Fix 4: Reset Visual Studio
In Visual Studio:
1. Tools → Options → Projects and Solutions
2. Click "Reset All Settings"
3. Restart VS
4. Reopen solution

---

**Most issues can be resolved by ensuring .NET 8 SDK is installed and running `dotnet restore`!**

Need more help? Check the DEVELOPER_GUIDE.md for architecture details.
