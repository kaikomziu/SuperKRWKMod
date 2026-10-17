# Developer Setup

This document provides instructions for setting up a development environment, building the mod, and understanding the project structure. It covers prerequisites, build configurations, project organization, and information about the third-party libraries used.

## Table of Contents

- [Notes](#notes)
- [Prerequisites](#prerequisites)
- [Initial Setup](#initial-setup)
- [Building](#building)
- [Project Structure](#project-structure)
- [Development Workflow](#development-workflow)
- [Releasing](#releasing)
- [Key Files](#key-files)
- [Third-Party Libraries](#third-party-libraries)
  - [UnityDoorstop](#unitydoorstop--winhttpdll)
  - [Harmony](#harmony--0harmonydll)
  - [Licenses](#licenses)
- [README ↗](../README.md)

## Notes

- Dependencies:
  - **UnityDoorstop** to inject into the Unity runtime
  - **Harmony** for runtime method patching
- Target framework is **.NET Framework 4.8**
- Unity assemblies are referenced from `BongoCat_Data\Managed\`
- The build system automatically creates zip files for distribution
- Release builds do not generate PDB files (debug symbols)
- Installation scripts automatically detect Steam installation paths from registry (HKCU, then HKLM, then common locations)

## Prerequisites

- **Visual Studio 2019 or later** (or any IDE that supports .NET Framework 4.8 projects)
  - Download: [Visual Studio 2026 Community ↗](https://visualstudio.microsoft.com/downloads/) (free)
  - Or [Build Tools for Visual Studio 2026 ↗](https://aka.ms/vs/stable/vs_BuildTools.exe) (minimal installation, no IDE)
- **Bongo Cat** installed via Steam
- **.NET Framework 4.8**

## Initial Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Shilo/bongo-cat-catnip-mod.git
   cd bongo-cat-catnip-mod
   ```

2. **Configure build paths:**
   
   Copy `Directory.Local.props.example` to `Directory.Local.props`:
   ```bash
   copy Directory.Local.props.example Directory.Local.props
   ```
   
   Edit `Directory.Local.props` and change `<SteamPath>` if your Steam installation is in a different location:
   ```xml
   <Project>
     <PropertyGroup>
       <SteamPath>C:\Program Files (x86)\Steam</SteamPath>
       ...
     </PropertyGroup>
   </Project>
   ```
   
   > **Note:** If the `<SteamPath>` doesn't update after editing, try unloading and reloading the project from the `Solution Explorer` pane in Visual Studio.

3. **Open the solution:**
   - Open `SuperKRWKMod.slnx` in Visual Studio

## Building

The project has three build configurations:

- **Debug** - Builds with debug symbols, launches via Steam
- **Debug (No Steam)** - Builds with debug symbols, launches `BongoCat.exe` directly
- **Release** - Optimized build without debug symbols, launches via Steam

**Quick Build (No Visual Studio Required):**

For users who want to compile the project without opening Visual Studio:

1. Double-click `build.bat` in the project root directory
2. The script will automatically find MSBuild and build the Release configuration
3. The compiled files will be in `SuperKRWKMod\bin\Release\`
4. A zip file will be created at `SuperKRWKMod\bin\SuperKRWKMod.zip`
5. The output directory (`SuperKRWKMod\bin`) will automatically open in Windows Explorer after a successful build
6. **Install the mod** by following the [Installation ↗](../README.md#installation) instructions using the files from `SuperKRWKMod\bin\Release\BongoCat\`

> **Note:** You still need Visual Studio or Visual Studio Build Tools installed for MSBuild to be available. The script will automatically detect MSBuild from common installation paths, including Visual Studio 2017, 2019, 2022, 2025, and 2026 (version 18).
> 
> **Don't have Visual Studio?** See [Prerequisites](#prerequisites) for download links.

**Building in Visual Studio:**

1. Select your desired configuration from the dropdown
2. Build the solution (F6 or Build → Build Solution)

**Build outputs:**
- Debug builds: `SuperKRWKMod\bin\Debug\`
- Release builds: `SuperKRWKMod\bin\Release\`
- Zip files are automatically created in `SuperKRWKMod\bin\` after building
  - Debug builds create `SuperKRWKMod-Debug.zip`
  - Release builds create `SuperKRWKMod.zip`

**Automatic deployment:**
- After building, `BongoCat.exe` is automatically closed if running
- Files are automatically copied to your Bongo Cat installation directory
- The game is automatically launched (via Steam for Debug/Release, or directly for "Debug (No Steam)")
- The mod is ready to test immediately after building

## Project Structure

```
SuperKRWKMod/
├── SuperKRWKMod.cs          # Main mod class
├── Entrypoint.cs         # UnityDoorstop entry point (runs when game starts)
├── Assets/
│   ├── BongoCat/         # Mod files organized for release
│   │   ├── SuperKRWKMod/
│   │   │   └── ...              # Mod assets
│   │   ├── doorstop_config.ini  # UnityDoorstop configuration
│   │   └── winhttp.dll          # UnityDoorstop loader
│   ├── install.bat       # Installation script (copied to release)
│   └── uninstall.bat     # Uninstallation script (copied to release)
└── Properties/
    └── AssemblyInfo.cs   # Assembly metadata
```

## Development Workflow

1. **Make your changes** in `SuperKRWKMod.cs` or `Entrypoint.cs`
2. **Build the project** (files are automatically copied to the game directory)
3. **Launch the game** to test your changes

## Releasing

Every update is published as a new GitHub release with `release.ps1` (needs `gh` logged in):

```powershell
pwsh ./release.ps1              # patch bump, e.g. 0.3.0 -> 0.3.1
pwsh ./release.ps1 -Bump minor  # 0.3.1 -> 0.4.0
pwsh ./release.ps1 -DryRun      # build and check only
```

The script bumps the version, builds without debug symbols, runs `tools/RefCheck` (fails if the DLL uses an API stripped from the game's assemblies), then commits, tags, pushes and creates the release with `SuperKRWKMod.zip`. The game's DLLs can't be redistributed, so releases are built locally rather than in CI.

## Key Files

- **`Entrypoint.cs`** - This is where your mod code runs when the game starts. The `Start()` method is called by UnityDoorstop before the game's main code executes.
- **`SuperKRWKMod.cs`** - Main mod class where you can add your mod functionality.
- **`doorstop_config.ini`** - Configures UnityDoorstop to load your mod DLL.
- **`install.bat`** - Automated installation script that:
  - Automatically detects Bongo Cat installation path from Windows registry
  - Closes Bongo Cat if running
  - Copies mod files and verifies installation
  - Opens game directory after completion
- **`uninstall.bat`** - Automated uninstallation script that:
  - Automatically detects Bongo Cat installation path from Windows registry
  - Closes Bongo Cat if running
  - Removes mod files and verifies deletion
  - Opens game directory after completion

## Third-Party Libraries

### [UnityDoorstop →](https://github.com/NeighTools/UnityDoorstop) (`winhttp.dll`)

**Version:** [4.4.1+ ↗](https://github.com/NeighTools/UnityDoorstop/releases/tag/v4.4.1)

A library that enables modding Unity games by injecting and executing managed .NET assemblies early in the Unity runtime, before the game's main code runs. This allows mods to hook into game systems and modify behavior.

### [Harmony →](https://github.com/pardeike/Harmony) (`0Harmony.dll`)

**Version:** [Harmony-Fat 2.4.2.0+ (net48) ↗](https://github.com/pardeike/Harmony/releases/tag/v2.4.2.0)

A library for patching, replacing and decorating .NET methods during runtime. Used by this mod to patch game methods and modify behavior.

### Licenses

**UnityDoorstop** - Licensed under the **GNU Lesser General Public License v2.1 (LGPL-2.1)**
- Permits use, modification, and distribution
- Requires derivative works to also be licensed under LGPL-2.1
- Allows linking with proprietary code without requiring the entire project to be open source

**Harmony** - Licensed under the **MIT License**
- Permits use, modification, and distribution with minimal restrictions
- Only requires preservation of copyright notice and license text
- Compatible with proprietary and open source projects

See [THIRD_PARTY_LICENSES.md ↗](THIRD_PARTY_LICENSES.md) for the full license texts of all third-party libraries used in this project.

