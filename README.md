# SuperKRWKMod

![Bongo Cat Logo](SuperKRWKMod/Assets/BongoCat/SuperKRWKMod/logo.png)

> Visual-only mod for [Bongo Cat](#bongo-cat-game-): zoomies, a typing speed meter and a sleepy cat. Free, safe, and no cheats.

Based on [Bongo Cat Catnip Mod ↗](https://github.com/Shilo/bongo-cat-catnip-mod/) by Shilo.

This mod extends the [Bongo Cat](#bongo-cat-game-) game with additional features and improvements by modifying the game's code while it runs, without changing the original game files.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
  - [Verification](#verification)
  - [Uninstallation](#uninstallation)
- [Bongo Cat Game](#bongo-cat-game-)
- [Developer Setup ↗](docs/DEVELOPER.md)
- [License ↗](LICENSE.md)

## Features

All features are visual only. The mod listens to taps but never changes the game's counters, Steam stats, items or drops.

- **Zoomies:** type faster than `BounceKps` keys per second and the cat hops and wobbles; past `RainbowKps` it shimmers in rainbow colors.
- **Speed meter:** current keys per second (KPS), session best and session taps above the cat. Toggle with `F7`.
- **Sleep mode:** after `IdleSeconds` without input the cat breathes slowly, closes its eyes and floats a "Zzz". Any key wakes it. Late at night (23:00-5:00 by default) it dozes off after `NightIdleSeconds` instead.
- **Eyes follow the cursor:** the pupils look toward your mouse. Eyes are detected automatically, so skins work too.
- **Combo counter:** "x128 COMBO" pops up next to the cat while you keep typing without a pause, changing colour at 50 / 100 / 200.
- **Records:** today's taps, typing time, best KPS and longest combo, all-time bests and the last 7 days, shown on the Mods page and saved in `SuperKRWKMod
ecords.csv`. Counted by the mod only; the game's stats are untouched.
- **App rules:** switch features off while a given app is running, and back on when it closes. Edit them in game under Settings → Mods → App rules: "+ Add from running apps" lists apps with an open window, then tick the features to switch off. They are stored in `superkrwk.ini`; for example `A Dance of Fire and Ice.exe=rainbow` under `[AppRules]` in `superkrwk.ini` turns off the rainbow glow while playing ADOFAI. Features: `zoomies`, `rainbow`, `meter`, `combo`, `sleep`, `eyes`, `all`. Only app names are checked, locally.
- **Mod list:** open the game menu → Settings → **Mods** to see installed mods and switch each feature on or off.
- **Settings:** `SuperKRWKMod\superkrwk.ini` in the game folder is created on first launch. Edit it and press `F9` (with Bongo Cat focused) to reload.

Hotkeys only work while the Bongo Cat window has focus (click the cat first).

## Installation

1. Go to the [Releases ↗](https://github.com/kaikomziu/SuperKRWKMod/releases) page
2. Download the latest `SuperKRWKMod.zip` file (or build it yourself with `build.bat`, see [Developer Setup ↗](docs/DEVELOPER.md))
3. Extract the contents of the zip file

**Automatic Installation (Recommended)**

- Double-click `install.bat` in the extracted folder
- The installer will automatically detect your Bongo Cat installation path
- If Bongo Cat is running, it will be automatically closed to ensure a clean installation
- Only mod files (`winhttp.dll`, `doorstop_config.ini`, and `SuperKRWKMod\` folder) will be copied to the game directory
- The game directory will open in Windows Explorer after successful installation

**Manual Installation**

- Copy all files and folders from the `BongoCat` folder to your Bongo Cat installation directory:
  - **Default location:** `C:\Program Files (x86)\Steam\steamapps\common\BongoCat\`
  - The files to copy include:
    - `winhttp.dll` (in the root)
    - `doorstop_config.ini` (in the root)
    - `SuperKRWKMod\` folder (containing `SuperKRWKMod.dll` and assets)

Your game directory structure should look like this:
```
BongoCat/
├── BongoCat.exe
├── winhttp.dll          ← Mod file
├── doorstop_config.ini  ← Mod file
├── SuperKRWKMod/           ← Mod folder
│   ├── SuperKRWKMod.dll
│   └── ... (mod assets)
└── ... (other game files)
```

4. Launch Bongo Cat through Steam as normal

### Verification

Open `%USERPROFILE%\AppData\LocalLow\Irox Games\BongoCat\Player.log` after launching. It should contain `[SuperKRWKMod] SuperKRWKMod | Features ready`.

### Uninstallation

**Automatic Uninstallation**

- Double-click `uninstall.bat` in the extracted mod folder
- The uninstaller will automatically detect your Bongo Cat installation path
- If Bongo Cat is running, it will be automatically closed to ensure a clean uninstallation
- Only mod files (`winhttp.dll`, `doorstop_config.ini`, and `SuperKRWKMod\` folder) will be deleted from the game directory
- The game directory will open in Windows Explorer after successful uninstallation

**Manual Uninstallation**

- Delete the mod files from your Bongo Cat installation directory:
  - **Default location:** `C:\Program Files (x86)\Steam\steamapps\common\BongoCat\`
  - The files to delete include:
    - `winhttp.dll` (in the root)
    - `doorstop_config.ini` (in the root)
    - `SuperKRWKMod\` folder (containing `SuperKRWKMod.dll` and assets)

## [Bongo Cat Game →](https://store.steampowered.com/app/3419430/Bongo_Cat/)

[![Steam](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3419430/2e6c93c020fe13d09d8120bb9164ed7cb58f00c5/header.jpg?t=1759317272)](https://store.steampowered.com/app/3419430/Bongo_Cat/)

> Bongo cat needs your help. Bongo cat needz more hatz!!! Every time you press a key, Bongo cat will punch your taskbar. Type, click, play, work to collect more points. Which hats will you find?

**Publisher:** [Irox Games ↗](https://store.steampowered.com/publisher/irox?snr=1_5_9__2000)  
**Developer:** [Marcel Zurawka ↗](https://store.steampowered.com/developer/irox?snr=1_5_9__2000), [Julius Krüger ↗](https://store.steampowered.com/developer/irox?snr=1_5_9__2000)

*Bongo Cat © Irox Games, Marcel Zurawka, Julius Krüger. Bongo Cat is a trademark of its respective owners. This mod is not affiliated with, endorsed by, or sponsored by the creators of Bongo Cat.*