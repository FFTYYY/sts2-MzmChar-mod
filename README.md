# 若叶睦 / Wakaba Mutsumi — Slay the Spire 2角色mod

**简体中文** | [English](README_EN.md)

为**杀戮尖塔2（Slay the Spire 2）**开发的自定义角色mod，加入新角色**若叶睦**。包含完整的90张专属卡牌、9个专属遗物、3个专属药水，以及10首专属BGM。支持中英双语UI。

[B站演示视频](https://www.bilibili.com/video/BV1Ecj76WEU2/)

## 玩家安装

**版本要求**：游戏本体v0.107.0或更新版本（stable或beta分支都可以）；BaseLib v3.3.0或更新版本。

1. 下载本mod最新release的`MzmChar.zip`并解压
2. 将解压得到的文件夹`MzmChar/`复制到`<游戏目录>/mods/`中
   - 本mod依赖[BaseLib](https://github.com/Alchyr/BaseLib-StS2)。为了方便使用，我们也提供了一个BaseLib的安装包`BaseLib.zip`。如果你之前没有安装过BaseLib，或者安装的版本较低，请一并安装
3. 启动游戏（第一次启动可能会闪退，再次启动应该就可以了），确保右下角提示「模组已加载」


**注意事项**：
- 杀戮尖塔2现在已经支持Steam创意工坊。未来Mod将会在创意工坊上同时更新，此处仅留作备份和参考。
- 老版本玩家请注意：从v0.2.4开始，stable和beta分支共用同一份mod包（之前需要根据游戏分支选`MzmChar.zip`或`MzmChar-beta.zip`）。同时**最低BaseLib版本要求提升到3.3.0**。低于3.3.0的BaseLib跟当前游戏版本不兼容，会导致联机崩溃。

## 开发者指南

完整开发文档（项目结构、加新卡/power/遗物、版本兼容机制、发布流程、常见故障排查）见[DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md)。

## License

见`LICENSE`。

## 致谢

- [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2)框架
- [pardeike/Harmony](https://github.com/pardeike/Harmony)运行时patch
- BanG Dream! It's MyGO!!!!! / Ave Mujica角色IP归属原作方（本mod为非盈利的同人二次创作）
