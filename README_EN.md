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

## Developer Guide

Full developer documentation (project layout, adding cards / powers / relics, the stable/beta dual-version mechanism, release workflow, troubleshooting) is in **[DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md)**.

---

## License

See `LICENSE`.

## Credits

- [Alchyr](https://github.com/Alchyr)'s [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) framework
- [pardeike/Harmony](https://github.com/pardeike/Harmony) for runtime patching
- The characters and IP from *BanG Dream! It's MyGO!!!!!* / *Ave Mujica* belong to their original creators. This mod is a non-commercial fan work.
