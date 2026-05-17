# Wakaba Mutsumi — Slay the Spire 2 Character Mod

[简体中文](README.md) | **English**

A custom character mod for **Slay the Spire 2**, adding the new character **Wakaba Mutsumi**. Includes a complete set of 88 exclusive cards, exclusive relics, and 5 exclusive BGM tracks. Supports bilingual UI in Chinese and English.

[Bilibili Demo Video](https://www.bilibili.com/video/BV1G3Ln6nESD/)

## Player Installation

1. Download the latest release zip of this mod and extract it.

   * For the stable version of the game, use `MzmChar.zip`; for the beta version of the game, use `MzmChar-beta.zip`.
2. Copy the extracted `MzmChar/` folder into `<Game Directory>/mods/`.

   * This mod depends on [BaseLib](https://github.com/Alchyr/BaseLib-StS2). For convenience, we also provide a BaseLib installer package, `BaseLib.zip`. If you have not installed BaseLib before, please install it as well.
3. Launch the game. The first launch may crash; simply launch it again. Make sure the lower-right corner shows “Mods loaded”.

**Notes**:

* After installing mods, the game will create a brand-new save file. To return to the unmodded version of the game and your original save file, simply delete or rename the `mods/` folder.
* Recommended for use together with the [Slay the Spire 2 Mod Manager](https://github.com/liwenhao0427/StS2ModManager), which makes it easy to switch mods and choose whether to launch the game in unmodded mode.
* You can find the game directory in Steam by selecting “Browse local files”. For Windows users, the default game directory is `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\`.

## Tech Stack / Dependencies

| Item                   | Version / Source                                                                          |
| ---------------------- | ----------------------------------------------------------------------------------------- |
| **Language / Runtime** | C# / .NET 9 SDK                                                                           |
| **Game Engine**        | Godot 4.5.1 (`.pck` exported with the [MegaDot](https://megadot.megacrit.com/) toolchain) |
| **Mod Framework**      | [Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) v3.1.x                      |
| **Runtime Patch**      | [Lib.Harmony](https://github.com/pardeike/Harmony)                                        |
| **Localization**       | LocString provided by BaseLib                                                             |

## Developers: Building from Source

### Initial Environment Setup

1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Install Slay the Spire 2 on Steam.
3. Install the [BaseLib mod](https://github.com/Alchyr/BaseLib-StS2/releases/latest) to `<Game Directory>/mods/BaseLib/`.
4. Download [MegaDot](https://megadot.megacrit.com/) and extract it anywhere.
5. Rename `local.props.example` to `local.props`, then change the two paths inside to match your local machine:

   ```xml
   <GameDir>C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2</GameDir>
   <MegaDotExe>C:\path\to\MegaDot_v4.5.1-stable_mono_win64_console.exe</MegaDotExe>
   ```

### Build

```bash
dotnet build
```

This will automatically compile `MzmChar.dll`, export `MzmChar.pck` with MegaDot, and copy the output to `<GameDir>/mods/MzmChar/`.

> Before building after modifying code, **close the game first**; otherwise, the DLL will be locked and deployment will fail.

## Project Structure

```
StS-MzmChar/
├── MzmChar.sln                 # IDE entry point
├── Directory.Build.props       # Shared MSBuild settings; automatically imports local.props
├── local.props.example         # Local path template; copy to local.props and edit
├── MzmChar.json                # Mod metadata
│
├── src/                        # C# code
│   ├── MzmChar.csproj
│   ├── ModEntry.cs             # Mod entry point ([ModInitializer])
│   ├── Config/                 # Mod settings (BaseLib SimpleModConfig)
│   └── Game/
│       ├── MutsumiCharacter.cs # Main CustomCharacterModel class
│       ├── CustomBgmPatch.cs   # Harmony patch for replacing battle BGM
│       └── CharacterContent/
│           ├── Cards/          # Cards; one .cs file per card, inheriting MzmCharBaseCard
│           ├── Powers/         # Custom Powers (Buffs/Debuffs)
│           ├── Relics/         # Relics
│           ├── Forms.cs        # Helper for switching between two forms (Mutsumi/Sakiko)
│           └── ...
│
├── pack/                       # Godot asset project; exported by MegaDot as MzmChar.pck
│   ├── project.godot
│   ├── export_presets.cfg
│   └── MzmChar/
│       ├── audio/              # Battle BGM mp3 files
│       ├── cards/              # Card art
│       ├── characters/         # Character art / selection screen art / portraits
│       ├── powers/             # Power icons
│       ├── relics/             # Relic icons
│       ├── scenes/             # Battle scenes / character selection backgrounds
│       └── localization/
│           ├── zhs/            # Simplified Chinese localization tables
│           └── eng/            # English localization tables
│
└── tests/                      # Placeholder test framework; real tests can only run in-game
```

## Adding / Modifying Content

* **Add a new card**: Create a new file under `src/Game/CharacterContent/Cards/`, add `[Pool(typeof(MzmCharCardPool))]`, and inherit from `MzmCharBaseCard`.
* **Add a new power**: Create a new file under `src/Game/CharacterContent/Powers/` and inherit from `CustomPowerModel`.
* **Add a new relic**: Create a new file under `src/Game/CharacterContent/Relics/` and add `[Pool(typeof(MzmCharRelicPool))]`.
* **Change BGM**: Drop `.mp3`, `.ogg`, or `.wav` files directly into `pack/MzmChar/audio/`; they will automatically be added to the random pool in the next battle.
* **Change Ancient dialogue**: Edit `pack/MzmChar/localization/{zhs,eng}/ancients.json`.

Use the existing Card / Power / Relic implementations as templates. The constructors of BaseLib’s `Custom*Model` abstract base classes automatically register them with the game’s `ModelDb`; no manual registration call is needed.

---

## License

See `LICENSE`.

## Credits

* [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) framework by [Alchyr](https://github.com/Alchyr)
* Runtime patching by [pardeike/Harmony](https://github.com/pardeike/Harmony)
* BanG Dream! It’s MyGO!!!!! / Ave Mujica character IP belongs to the original rights holders. This mod is a non-profit fan derivative work.
