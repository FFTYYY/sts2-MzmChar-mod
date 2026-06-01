# Wakaba Mutsumi / 若叶睦 — Slay the Spire 2 Character Mod

**English** | [简体中文](README.md)

A custom character mod developed for **Slay the Spire 2**, adding the new character **Mutsumi Wakaba**. It includes a complete set of 90 exclusive cards, 9 exclusive relics, 3 exclusive potions, and 10 exclusive BGM tracks. Supports bilingual UI in Chinese and English.

[Bilibili demo video](https://www.bilibili.com/video/BV1G3Ln6nESD/)

## Player Installation

1. Download the latest release zip of this mod and extract it.
   * For the stable version of the game, use `MzmChar.zip`; for the beta version of the game, use `MzmChar-beta.zip`.
2. Copy the extracted `MzmChar/` folder into `<game directory>/mods/`.
   * This mod depends on [BaseLib](https://github.com/Alchyr/BaseLib-StS2). For convenience, we also provide a BaseLib installation package, `BaseLib.zip`. If you have not installed BaseLib before, please install it as well.
3. Launch the game. The first launch may crash; launching it again should work. Make sure the bottom-right corner shows “Mods loaded”.

**Notes:**

* After installing mods, the game will create a brand-new save file. To return to the unmodded game and your original save, simply delete the `mods/` folder or rename it.
* Recommended for use together with the [Slay the Spire 2 Mod Manager](https://github.com/liwenhao0427/StS2ModManager), which makes it convenient to switch mods and choose whether to launch the game in unmodded mode.
* You can find the game directory in Steam by selecting “Browse local files”. For Windows users, the default game directory is `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\`.

## Developer Guide

For the complete development documentation, including project structure, adding new cards / powers / relics, the stable/beta dual-version mechanism, release workflow, and common troubleshooting, see **[DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md)**.

## License

See `LICENSE`.

## Acknowledgements

* [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) framework by [Alchyr](https://github.com/Alchyr)
* Runtime patching by [pardeike/Harmony](https://github.com/pardeike/Harmony)
* The character IPs of BanG Dream! It's MyGO!!!!! / Ave Mujica belong to their original rights holders. This mod is a non-commercial fan work.
