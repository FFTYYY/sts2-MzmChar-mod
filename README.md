# 若叶睦 / Wakaba Mutsumi — Slay the Spire 2 角色 mod

**简体中文** | [English](README_EN.md)

为 **杀戮尖塔2（Slay the Spire 2）** 开发的自定义角色 mod，加入新角色**若叶睦**。包含完整的 88 张专属卡牌、专属遗物，以及 5 首专属 BGM。支持中英双语 UI。

[B站演示视频](https://www.bilibili.com/video/BV1G3Ln6nESD/)

## 玩家安装

1. 下载本 mod 最新 release zip 并解压
   - 对于正式版游戏本体，请使用MzmChar.zip；对于beta版游戏本体，请使用MzmChar-beta.zip
2. 将解压得到的文件夹 `MzmChar/` 复制到 `<游戏目录>/mods/` 中
   - 本 mod 依赖 [BaseLib](https://github.com/Alchyr/BaseLib-StS2)。为了方便使用，我们也提供了一个 BaseLib 的安装包 `BaseLib.zip`。如果你之前没有安装过 BaseLib，请一并安装之。
3. 启动游戏（第一次启动可能会闪退，再次启动就好），确保右下角提示「模组已加载」。


**注意事项**：
- 安装 mod 后，游戏会开一个全新的存档。只需要删除 `mods/` 文件夹（或者改名）就可以回到无 mod 版的游戏以及原来的存档。
- 推荐和[杀戮尖塔2 Mod 管理器](https://github.com/liwenhao0427/StS2ModManager)一起使用，可以方便地切换 mod 以及选择是否以无 mod 模式启动游戏。
- 游戏目录可以在 steam 中选择「浏览本地文件」查看。对于windows用户，游戏目录默认在 `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\`。

## 技术栈 / 依赖

| 项 | 版本 / 来源 |
|---|---|
| **语言 / 运行时** | C# / .NET 9 SDK |
| **游戏引擎** | Godot 4.5.1（[MegaDot](https://megadot.megacrit.com/) 工具链导出 `.pck`）|
| **Mod 框架** | [Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) v3.1.x |
| **运行时 Patch** | [Lib.Harmony](https://github.com/pardeike/Harmony) |
| **多语言** | BaseLib 自带 LocString |

## 从源码构建

1. 装 [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)、Slay the Spire 2、[BaseLib mod](https://github.com/Alchyr/BaseLib-StS2/releases/latest) 到 `<游戏目录>/mods/BaseLib/`、[MegaDot](https://megadot.megacrit.com/)
2. 复制 `local.props.example` → `local.props`，填入本机 `GameDir` 和 `MegaDotExe` 路径
3. `dotnet build` —— 编译会**自动检测你 Steam 装的是 stable 还是 beta 分支**，出对应版本的 dll 并部署到 `<GameDir>/mods/MzmChar/`

完整开发文档（项目结构、加新卡 / power / 遗物、stable/beta 双版本机制、发布流程、常见故障排查）见 **[DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md)**。

---

## License

见 `LICENSE`。

## 致谢

- [Alchyr](https://github.com/Alchyr) 的 [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) 框架
- [pardeike/Harmony](https://github.com/pardeike/Harmony) 运行时 patch
- BanG Dream! It's MyGO!!!!! / Ave Mujica 角色 IP 归属原作方（本 mod 为非盈利的同人二次创作）
