# Wakaba Mutsumi / 若叶睦 — Slay the Spire 2 Character Mod

**English** | [简体中文](README.md)

A custom-character mod for **Slay the Spire 2** that adds **Wakaba Mutsumi** (若叶睦) as a playable character.

## Player Install

1. Download the latest release zip and extract it.
2. Copy the extracted folders `MzmChar/` and `BaseLib/` into `<GameDir>/mods/`.
   - This mod depends on [BaseLib](https://github.com/Alchyr/BaseLib-StS2). For convenience the release zip already bundles a copy of BaseLib. If you already have BaseLib installed, you can skip the `BaseLib/` folder and just copy `MzmChar/`.
3. Launch the game (the very first launch may crash once — just launch again). Make sure you see the "mods loaded" notice in the bottom-right.

**Notes**:
- Installing mods makes the game create a fresh save. To go back to the unmodded game with your original save, just remove (or rename) the `mods/` folder.
- We recommend pairing with the [Slay the Spire 2 Mod Manager](https://github.com/liwenhao0427/StS2ModManager) for easy toggling between modded and unmodded launches.
- You can find the game directory by right-clicking the game in Steam → Manage → Browse local files. On Windows it usually lives at `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\`.

## Features

- New character "Wakaba Mutsumi" + dual-persona (Mu / Mo) mechanic
- 88 character-specific cards, dedicated relic pool
- Optional combat BGM swap to character-themed tracks (Settings → Mods → Mutsumi Character → Custom Combat BGM)
- Bilingual UI (Simplified Chinese / English)

## Tech Stack / Dependencies

| Item | Version / Source |
|---|---|
| **Language / Runtime** | C# / .NET 9 SDK |
| **Game Engine** | Godot 4.5.1 ([MegaDot](https://megadot.megacrit.com/) toolchain to export `.pck`) |
| **Mod Framework** | [Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) v3.1.x |
| **Runtime Patching** | [Lib.Harmony](https://github.com/pardeike/Harmony) |
| **Localization** | BaseLib's built-in LocString |

## Developer: Build from Source

### One-Time Setup

1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Install Slay the Spire 2 (Steam)
3. Install the [BaseLib mod](https://github.com/Alchyr/BaseLib-StS2/releases/latest) into `<GameDir>/mods/BaseLib/`
4. Download [MegaDot](https://megadot.megacrit.com/) and extract anywhere
5. Rename `local.props.example` → `local.props`, and update the two paths inside to match your machine:
   ```xml
   <GameDir>C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2</GameDir>
   <MegaDotExe>C:\path\to\MegaDot_v4.5.1-stable_mono_win64_console.exe</MegaDotExe>
   ```

### Build

```bash
dotnet build
```

This will automatically: compile `MzmChar.dll` → run MegaDot to export `MzmChar.pck` → copy everything to `<GameDir>/mods/MzmChar/`.

> **Close the game before building**, otherwise the dll will be locked and deployment will fail.

## Project Layout

```
StS-MzmChar/
├── MzmChar.sln                 # IDE entry
├── Directory.Build.props       # Shared MSBuild (auto-imports local.props)
├── local.props.example         # Per-machine path template (copy → local.props and edit)
├── MzmChar.json                # Mod manifest
│
├── src/                        # C# code
│   ├── MzmChar.csproj
│   ├── ModEntry.cs             # Mod entry point ([ModInitializer])
│   ├── Config/                 # Mod settings (BaseLib SimpleModConfig)
│   └── Game/
│       ├── MutsumiCharacter.cs # CustomCharacterModel main class
│       ├── CustomBgmPatch.cs   # Combat BGM swap Harmony patch
│       └── CharacterContent/
│           ├── Cards/          # Cards (one .cs each, extends MzmCharBaseCard)
│           ├── Powers/         # Custom powers (buffs / debuffs)
│           ├── Relics/         # Relics
│           ├── Forms.cs        # Dual-persona (Mu / Mo) switching helper
│           └── ...
│
├── pack/                       # Godot asset project (MegaDot exports to MzmChar.pck)
│   ├── project.godot
│   ├── export_presets.cfg
│   └── MzmChar/
│       ├── audio/              # Combat BGM mp3s
│       ├── cards/              # Card portraits
│       ├── characters/         # Character art / select screen / icon
│       ├── powers/             # Power icons
│       ├── relics/             # Relic icons
│       ├── scenes/             # Combat scenes / select screen background
│       └── localization/
│           ├── zhs/            # Simplified Chinese loc table
│           └── eng/            # English loc table
│
└── tests/                      # Stub test framework (real testing happens in-game)
```

## Adding / Modifying Content

- **Add a new card**: create a file in `src/Game/CharacterContent/Cards/`, annotate `[Pool(typeof(MzmCharCardPool))]`, extend `MzmCharBaseCard`
- **Add a new power**: create a file in `src/Game/CharacterContent/Powers/`, extend `CustomPowerModel`
- **Add a new relic**: create a file in `src/Game/CharacterContent/Relics/`, annotate `[Pool(typeof(MzmCharRelicPool))]`
- **Swap BGM**: drop `.mp3` / `.ogg` / `.wav` files into `pack/MzmChar/audio/` — they're auto-added to the random pool next combat
- **Edit Ancient dialogue**: edit `pack/MzmChar/localization/{zhs,eng}/ancients.json`

Use the existing Card / Power / Relic implementations as templates. BaseLib's `Custom*Model` base classes auto-register through their constructor — no manual `ModelDb.Inject` call needed.

---

## License

See `LICENSE`.

## Credits

- [Alchyr](https://github.com/Alchyr)'s [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) framework
- [pardeike/Harmony](https://github.com/pardeike/Harmony) for runtime patching
- The characters and IP from *BanG Dream! It's MyGO!!!!!* / *Ave Mujica* belong to their original creators. This mod is a non-commercial fan work.
