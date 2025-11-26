# KairosEDA Distribution Guide

## Building for Distribution

### Quick Build

Run the automated build script:
```powershell
.\build-release.ps1
```

This creates a **single self-contained .exe** at:
```
KairosEDA\bin\Release\net8.0-windows\win-x64\publish\KairosEDA.exe
```

### Manual Build

If you prefer to build manually:
```powershell
dotnet publish KairosEDA/KairosEDA.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

---

## Distributing to Testers

### What to Send

Send testers the **single .exe file**:
- **File**: `KairosEDA.exe`
- **Size**: ~80-150 MB (includes all dependencies)
- **No installation needed** - just run it

### Distribution Methods

#### Option 1: GitHub Releases (Recommended)
1. Go to your repo: https://github.com/Plutomaster28/KairosEDA
2. Click "Releases" → "Create a new release"
3. Tag version (e.g., `v0.1.0-alpha`)
4. Upload `KairosEDA.exe`
5. Write release notes
6. Check "This is a pre-release"
7. Publish

Testers download from: `https://github.com/Plutomaster28/KairosEDA/releases`

#### Option 2: Google Drive / Dropbox
1. Upload `KairosEDA.exe` to cloud storage
2. Share link with testers
3. Simple but less professional

#### Option 3: Discord / Email
- **Discord**: Works if file < 25 MB (unlikely with self-contained)
- **Email**: Attach directly or use file transfer service

---

## Tester Instructions

Copy this to your testers:

### KairosEDA Installation Instructions

1. **Download** `KairosEDA.exe`
2. **Save** it to a folder (e.g., `C:\KairosEDA\`)
3. **Double-click** to run
4. **First Launch**:
   - If WSL2 is not installed, you'll be prompted
   - Click "Yes" to start backend setup (takes 30-40 min)
   - Tools will compile automatically in WSL

**System Requirements:**
- Windows 10/11 (64-bit)
- 8 GB RAM minimum
- 5 GB free disk space
- Internet connection (for initial setup)

**No .NET installation required** - everything is included!

---

## Version Management

### Versioning Scheme

Use semantic versioning: `MAJOR.MINOR.PATCH`

- **0.1.0** - First alpha release
- **0.2.0** - Beta with core features
- **1.0.0** - Production release

### Update Version Number

Edit `KairosEDA.csproj`:
```xml
<PropertyGroup>
  <Version>0.1.0</Version>
  <AssemblyVersion>0.1.0.0</AssemblyVersion>
  <FileVersion>0.1.0.0</FileVersion>
</PropertyGroup>
```

Then rebuild with `.\build-release.ps1`

---

## Build Configurations

### Current Setup: Self-Contained Single File

**Pros:**
- ✅ One file to distribute
- ✅ No .NET runtime needed
- ✅ Works on any Windows 10/11 x64 machine
- ✅ Includes Newtonsoft.Json and all dependencies

**Cons:**
- ❌ Larger file size (~80-150 MB)
- ❌ Separate build needed for ARM64 (if needed)

### Alternative: Framework-Dependent (Smaller)

If testers can install .NET 8.0 Desktop Runtime:
```powershell
dotnet publish -c Release --self-contained false
```

**Output size**: ~10-20 MB  
**Requires**: [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Testing Checklist Before Distribution

### Pre-Release Testing

- [ ] Clean build completes without errors
- [ ] Application starts and shows splash screen
- [ ] WSL detection works correctly
- [ ] Setup wizard launches if no toolchain detected
- [ ] Main window loads properly
- [ ] Example projects open (adder_4bit.v, counter_8bit.v, traffic_light.v)
- [ ] Console output displays correctly
- [ ] About dialog shows correct version

### Test on Fresh Machine

Ideally test on a VM or fresh Windows install:
- [ ] Run .exe without any prerequisites
- [ ] Verify WSL installation prompt works
- [ ] Complete backend setup process
- [ ] Run a simple synthesis workflow

---

## File Structure

### What's Included in the .exe

```
KairosEDA.exe (self-contained)
├── Application code
├── .NET 8.0 runtime
├── WPF framework
├── Windows Forms framework
├── Newtonsoft.Json
├── Icon resources
└── All dependencies
```

### What's NOT Included (User Downloads)

- WSL2 (installed via Windows Features)
- Ubuntu WSL distro (installed automatically)
- Yosys source code (cloned from GitHub during setup)
- OpenROAD source code (cloned from GitHub during setup)

---

## Troubleshooting

### "Windows protected your PC" SmartScreen Warning

This is normal for unsigned executables. Testers should:
1. Click "More info"
2. Click "Run anyway"

**To avoid this**: Sign the executable with a code signing certificate (~$100/year)

### Antivirus False Positives

Self-extracting .exe files sometimes trigger antivirus. Solutions:
1. Ask testers to add exception
2. Submit to antivirus vendors for whitelisting
3. Code sign the executable (reduces false positives)

### Large File Size

If 80-150 MB is too large:
1. Use framework-dependent build (requires .NET install)
2. Enable trimming: `-p:PublishTrimmed=true` (may break reflection)
3. Use Ready2Run: `-p:PublishReadyToRun=true` (faster startup, larger size)

---

## Advanced: Automated Builds with GitHub Actions

Want automatic builds on every release? Add `.github/workflows/release.yml`:

```yaml
name: Build Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Publish
        run: dotnet publish KairosEDA/KairosEDA.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
      - name: Upload Release
        uses: actions/upload-artifact@v3
        with:
          name: KairosEDA-win-x64
          path: KairosEDA/bin/Release/net8.0-windows/win-x64/publish/KairosEDA.exe
```

Then just push a tag: `git tag v0.1.0 && git push --tags`

---

## Quick Reference

### Build Command
```powershell
.\build-release.ps1
```

### Output Location
```
KairosEDA\bin\Release\net8.0-windows\win-x64\publish\KairosEDA.exe
```

### Distribute This File
- Single .exe (self-contained)
- ~80-150 MB
- No installation required
- Windows 10/11 x64 only

### Tester Requirements
- Windows 10/11 (64-bit)
- Internet connection
- WSL2 (prompted if missing)

---

**Ready to distribute!** Just run `.\build-release.ps1` and send the resulting .exe to your testers.
