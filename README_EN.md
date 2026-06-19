# Wakaba Mutsumi / 若叶睦 — Slay the Spire 2 Character Mod

**English** | [简体中文](README.md)

A custom character mod developed for **Slay the Spire 2**, adding the new character **Mutsumi Wakaba**. It includes a complete set of 90 exclusive cards, 9 exclusive relics, 3 exclusive potions, and 10 exclusive BGM tracks. Supports bilingual UI in Chinese and English.

[Bilibili demo video](https://www.bilibili.com/video/BV1G3Ln6nESD/)

## Player Installation

**Requirements**: game v0.107.0 or newer (stable or beta both fine); BaseLib v3.3.0 or newer.

1. Download the latest release's `MzmChar.zip` and extract it.
2. Copy the extracted `MzmChar/` folder into `<game directory>/mods/`.
   * This mod depends on [BaseLib](https://github.com/Alchyr/BaseLib-StS2). For convenience, we also provide a BaseLib installation package, `BaseLib.zip`. If you haven't installed BaseLib before, or your installed version is too old, please install it as well.
3. Launch the game. The first launch may crash; launching again should work. Make sure the bottom-right corner shows "Mods loaded".

**Notes:**

* Slay the Spire 2 now supports Steam Workshop. The mod will be updated on the Workshop going forward; this repository is kept only as a backup and reference.
* Note for returning players: starting with v0.2.4, stable and beta share the same mod package (previously you had to pick `MzmChar.zip` or `MzmChar-beta.zip` based on your game branch). The minimum required BaseLib version is now **3.3.0**. BaseLib versions older than 3.3.0 are incompatible with the current game and will break multiplayer.

## Developer Guide

For the complete development documentation, including project structure, adding new cards / powers / relics, the version-compatibility mechanism, release workflow, and common troubleshooting, see **[DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md)**.

## License

See `LICENSE`.

## Acknowledgements

* [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) framework by [Alchyr](https://github.com/Alchyr)
* Runtime patching by [pardeike/Harmony](https://github.com/pardeike/Harmony)
* The character IPs of BanG Dream! It's MyGO!!!!! / Ave Mujica belong to their original rights holders. This mod is a non-commercial fan work.
