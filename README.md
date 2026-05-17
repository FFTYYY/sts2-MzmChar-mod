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

## 开发者：从源码构建

### 初始环境配置

1. 安装 [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. 安装好 Slay the Spire 2（Steam）
3. 安装 [BaseLib mod](https://github.com/Alchyr/BaseLib-StS2/releases/latest) 到 `<游戏目录>/mods/BaseLib/`
4. 下载 [MegaDot](https://megadot.megacrit.com/) 解压到任意位置
5. 将`local.props.example`改名为`local.props`，把里面两条路径改成你本机的：
   ```xml
   <GameDir>C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2</GameDir>
   <MegaDotExe>C:\path\to\MegaDot_v4.5.1-stable_mono_win64_console.exe</MegaDotExe>
   ```

### 构建

```bash
dotnet build
```

会自动：编译 `MzmChar.dll` → MegaDot 导出 `MzmChar.pck` → 拷贝到 `<GameDir>/mods/MzmChar/`。

> 改完代码 build 之前要**关掉游戏**，不然 dll 被锁定部署失败。

## 项目结构

```
StS-MzmChar/
├── MzmChar.sln                 # IDE 入口
├── Directory.Build.props       # 公共 MSBuild（自动 import local.props）
├── local.props.example         # 本机路径模板（复制为 local.props 后改）
├── MzmChar.json                # mod 元数据
│
├── src/                        # C# 代码
│   ├── MzmChar.csproj
│   ├── ModEntry.cs             # mod 入口（[ModInitializer]）
│   ├── Config/                 # mod 设置（BaseLib SimpleModConfig）
│   └── Game/
│       ├── MutsumiCharacter.cs # CustomCharacterModel 主类
│       ├── CustomBgmPatch.cs   # 战斗 BGM 替换 Harmony patch
│       └── CharacterContent/
│           ├── Cards/          # 卡牌（每张一个 .cs，继承 MzmCharBaseCard）
│           ├── Powers/         # 自定义 Power (Buff/Debuff)
│           ├── Relics/         # 遗物
│           ├── Forms.cs        # 双形态（小睦/小墨）切换 helper
│           └── ...
│
├── pack/                       # Godot 资源项目（MegaDot 导出为 MzmChar.pck）
│   ├── project.godot
│   ├── export_presets.cfg
│   └── MzmChar/
│       ├── audio/              # 战斗 BGM mp3
│       ├── cards/              # 卡牌画
│       ├── characters/         # 角色立绘 / 选角图 / 头像
│       ├── powers/             # power 图标
│       ├── relics/             # 遗物图标
│       ├── scenes/             # 战斗场景 / 选角背景
│       └── localization/
│           ├── zhs/            # 简体中文 loc table
│           └── eng/            # 英文 loc table
│
└── tests/                      # 占位测试框架（真测试只能在游戏内跑）
```


## 添加 / 修改内容

- **加新卡**：在 `src/Game/CharacterContent/Cards/` 新建文件，挂 `[Pool(typeof(MzmCharCardPool))]`，继承 `MzmCharBaseCard`
- **加新 power**：在 `src/Game/CharacterContent/Powers/` 新建，继承 `CustomPowerModel`
- **加新遗物**：在 `src/Game/CharacterContent/Relics/` 新建，挂 `[Pool(typeof(MzmCharRelicPool))]`
- **改 BGM**：直接往 `pack/MzmChar/audio/` 丢 `.mp3` / `.ogg` / `.wav`，下次战斗自动加入随机池
- **改先古对话**：编辑 `pack/MzmChar/localization/{zhs,eng}/ancients.json`

参考已有的 Card / Power / Relic 实现作为模板。BaseLib 各 `Custom*Model` 抽象基类的 ctor 自动注册到游戏 ModelDb，无需手动调用。

---

## License

见 `LICENSE`。

## 致谢

- [Alchyr](https://github.com/Alchyr) 的 [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) 框架
- [pardeike/Harmony](https://github.com/pardeike/Harmony) 运行时 patch
- BanG Dream! It's MyGO!!!!! / Ave Mujica 角色 IP 归属原作方（本 mod 为非盈利的同人二次创作）
