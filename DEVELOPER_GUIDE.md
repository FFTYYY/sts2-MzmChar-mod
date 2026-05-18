# MzmChar 开发者指南

> 最后更新：2026-05-17（适用游戏版本：stable v0.103.x / public-beta v0.105.x）。
> 中文部分在前，英文版本在文档下半部分。
>
> **English readers**: the English version is in the second half of this file — jump to [Developer Guide (English)](#developer-guide-english). Last updated: 2026-05-17 (covers game stable v0.103.x / public-beta v0.105.x).

---

## 1. 项目是什么

本项目是为 *杀戮尖塔2*（Slay the Spire 2）开发的**自定义角色 mod**，加入新角色「若叶睦 / Wakaba Mutsumi」，含 88 张专属卡牌、专属遗物、5 首专属战斗背景音乐、中英双语界面，以及建筑师（Architect）与其他先古之民的对话内容。

mod 本身是个 .NET 9 类库（`MzmChar.dll`）+ 一份 Godot 打包的资源包（`MzmChar.pck`）+ 一份元数据 JSON（`MzmChar.json`），三个文件一起塞进游戏的 `mods/MzmChar/` 目录就生效。

---

## 2. 你需要了解的技术栈

| 名称 | 角色 | 文档 |
|---|---|---|
| **C# / .NET 9** | mod 主体的编程语言与运行时 | [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) |
| **Godot 4.5.1** | 游戏本体用的引擎，mod 的资源也走它的格式 | — |
| **MegaDot** | MegaCrit 改造的 Godot 命令行工具，把 `pack/` 下的资源导出成 `.pck` | [megadot.megacrit.com](https://megadot.megacrit.com/) |
| **[BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2)** | 社区 mod 框架。提供 `CustomCardModel` / `CustomCharacterModel` 等基类，让自定义内容能自动注册到游戏。**玩家也必须安装这个 mod** | [Wiki](https://alchyr.github.io/BaseLib-Wiki/) |
| **[Harmony](https://github.com/pardeike/Harmony)** | 运行时给游戏方法挂前置 / 后置补丁，用来修改游戏本体的行为（例如战斗背景音乐替换） | [Wiki](https://github.com/pardeike/Harmony/wiki) |
| **Steam 上的杀戮尖塔2** | 你机器上必须装一份游戏。mod 编译时会引用游戏的 `sts2.dll` | — |

---

## 3. 一次性环境配置

### 3.1 装好基础工具

1. **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)**——执行 `dotnet --version` 输出 9.x 即可
2. **Steam 上的杀戮尖塔2**——任意官方分支（stable / public-beta 都行）
3. **[BaseLib mod](https://github.com/Alchyr/BaseLib-StS2/releases/latest)**——下载最新发布的 zip，解压到 `<游戏目录>/mods/BaseLib/`。**这一步关键**，没装 BaseLib 你构建出来的 mod 进游戏就崩
4. **[MegaDot](https://megadot.megacrit.com/)**——下载 `*_console.exe` 那一版（无界面模式能拿到标准输出便于排查），解压到任意位置

### 3.2 复制 `local.props`

项目根目录有一份 `local.props.example`。把它复制一份改名 `local.props`，编辑里面两条路径：

```xml
<GameDir>C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2</GameDir>
<MegaDotExe>C:\path\to\MegaDot_v4.5.1-stable_mono_win64_console.exe</MegaDotExe>
```

- `GameDir` 指向游戏的安装根目录（含 `SlayTheSpire2.exe` 那一级）
- `MegaDotExe` 是你刚下载的 MegaDot 那个 `.exe` 的**完整路径**

`local.props` 已经在 `.gitignore` 中，不会被提交，每个开发者各自配各自的。

---

## 4. 构建

```bash
dotnet build
```

会自动完成以下步骤：

1. 编译 `src/MzmChar.csproj` → `bin/Debug/MzmChar.dll`
2. 调用 MegaDot 把 `pack/` 下的 Godot 资源（图、音频、场景、本地化 JSON）导出成 `MzmChar.pck`
3. 把 `MzmChar.dll` + `MzmChar.pck` + `MzmChar.json` 拷贝到 `<GameDir>/mods/MzmChar/`

只要这一步成功，启动游戏就能识别 mod。

**重要**：构建之前要**关闭游戏**，否则 dll 文件被锁，部署失败。

游戏日志在 `%AppData%\Roaming\SlayTheSpire2\logs\godot*.log`——`godot.log` 是最新一次启动的日志，带时间戳的是历史日志。出问题先看日志。

---

## 5. stable / beta 双版本兼容

### 5.1 背景

杀戮尖塔2 目前有两个 Steam 分支：
- **stable**（默认）：当前是 v0.103.x
- **public-beta**（玩家在 Steam 客户端手动启用）：当前是 v0.105.x

两个分支的 `sts2.dll` 内部 **API 不兼容**。具体差异：

| API | stable 旧签名 | beta 新签名 |
|---|---|---|
| `PowerCmd.Apply<T>` | `(target, amount, applier, card, silent)` | `(ctx, target, amount, applier, card, silent)` |
| `PowerCmd.ModifyAmount` | `(power, offset, applier, card, silent)` | `(ctx, power, offset, applier, card, silent)` |
| `CardPileCmd.AddGeneratedCard(s)ToCombat` | 带 `addedByPlayer` 参数 | 砍掉 `addedByPlayer`，新增 `Player creator` 必填 |
| `PowerModel.IsInstanced` | `virtual bool IsInstanced` | 改成 `virtual PowerInstanceType InstanceType` 枚举 |
| `CardPile.maxCardsInHand` | 小写 | 改成大写 `MaxCardsInHand` |
| `ModManifest` JSON 结构 | `dependencies: ["BaseLib"]` 字符串数组 | `dependencies: [{ "id": "...", "min_version": "..." }]` 对象数组 + 新增 `min_game_version` 必填 |

也就是说，**一份 dll 不能两边都跑**——必须按目标版本分别编译。

### 5.2 自动检测

`Directory.Build.props` 在构建时读取 `<GameDir>/release_info.json` 的 `version` 字段。如果其值含 `v0.105` / `v0.106` / `v0.107` / `v0.108` / `v0.109` / `v0.11` 之一 → 自动定义一个 `BETA` 常量，代码里 `#if BETA` 分支会走 beta 版 API；否则不定义这个常量，走 stable 版 API。

**含义**：你在 Steam 切到 beta 分支 → `dotnet build` 自动产出 beta 版 dll；切回 stable → 自动产出 stable 版 dll。**不需要你手动开关**。

需要强制覆盖时可以用 `dotnet build -c Beta`（强制定义 BETA，不读 `release_info.json`）。

### 5.3 双版本机制涉及的文件

- `Directory.Build.props`——`BETA` 常量的自动检测逻辑
- `src/Game/Sts2Compat.cs`——**集中**所有跨版本 API 包装器（例如 `Sts2Compat.PowerApply<T>(ctx, ...)`）。业务代码全部走包装器，不直接调用游戏本体的命令
- `src/Game/CharacterContent/Powers/*.cs`——5 处 `IsInstanced` / `InstanceType` 的重写，用 `#if BETA` 包裹
- `MzmChar.json`——stable 用的旧版清单
- `MzmChar.beta.json`——beta 用的新版清单（含 `min_game_version` 和 ModDependency 对象格式）
- `src/MzmChar.csproj`——根据是否定义 `BETA` 选哪份清单复制到部署目录

### 5.4 发布双版本

发布前手动跑两次构建：

```bash
# 步骤 1：Steam 切到 stable 分支 → 让 Steam 下载 stable 版游戏
dotnet build
# 拿到 mods/MzmChar/MzmChar.dll → 重命名 → 压成 MzmChar-stable.zip

# 步骤 2：Steam 切到 public-beta 分支 → 让 Steam 下载 beta 版游戏
dotnet build
# 拿到 mods/MzmChar/MzmChar.dll → 压成 MzmChar-beta.zip
```

两个 zip 都发布。玩家按自己的游戏分支下载对应那份。

### 5.5 长期：等 stable 也升到 v0.105+ 之后

等 Steam stable 也升到 v0.105 或更高（按节奏估计 1~3 个月内）时，两边 API 就一致了，可以**删掉所有 `#if BETA / #else` 分支**回归单一构建、单一 dll、单一 zip 发布。届时的清理清单：

1. 删 `Sts2Compat.cs` 里所有 `#else` 分支，只保留 `#if BETA` 内的实现
2. 删 5 处 Power 文件的 `#else IsInstanced` 分支
3. 把 `ConcertPower.cs` 里的 `Sts2Compat.MaxCardsInHand` 改回直接用 `CardPile.MaxCardsInHand`
4. 删 csproj 里两份清单的条件复制逻辑，直接用 `MzmChar.beta.json`（改名为 `MzmChar.json`）
5. 删 `Directory.Build.props` 里自动检测 `BETA` 那段

---

## 6. 项目结构

```
StS-MzmChar/
├── MzmChar.sln                 # IDE 工程入口
├── Directory.Build.props       # 公共 MSBuild + 自动导入 local.props + 自动检测 BETA 常量
├── local.props.example         # 本机路径模板（复制为 local.props 后修改）
├── MzmChar.json                # mod 元数据清单（stable 旧版结构）
├── MzmChar.beta.json           # mod 元数据清单（beta 新版结构）
│
├── src/                        # C# 代码
│   ├── MzmChar.csproj
│   ├── ModEntry.cs             # mod 入口（[ModInitializer]）
│   ├── Config/                 # mod 设置面板（BaseLib SimpleModConfig）
│   └── Game/
│       ├── Sts2Compat.cs       # 跨 stable / beta 版本的游戏本体 API 包装器
│       ├── MutsumiCharacter.cs # 角色主类（继承 CustomCharacterModel）
│       ├── CustomBgmPatch.cs   # 战斗背景音乐替换的 Harmony 补丁
│       └── CharacterContent/
│           ├── MzmCharBaseCard.cs   # 本 mod 卡牌的公共基类
│           ├── MzmCharCardPool.cs   # 角色专属卡池
│           ├── MzmCharRelicPool.cs  # 角色专属遗物池
│           ├── Forms.cs             # 双形态（小睦 / 小墨）切换辅助
│           ├── CombatCounters.cs    # 跨卡共享的战斗内计数器
│           ├── Cards/               # 卡牌（每张一个 .cs，继承 MzmCharBaseCard）
│           ├── Powers/              # 自定义能力（增益 / 减益）
│           ├── Relics/              # 遗物
│           └── ArchitectDialogue.cs # 建筑师对话注入
│
├── pack/                       # Godot 资源项目（由 MegaDot 导出成 MzmChar.pck）
│   ├── project.godot
│   ├── export_presets.cfg
│   └── MzmChar/
│       ├── audio/              # 战斗背景音乐（mp3 / ogg / wav）
│       ├── cards/              # 卡牌画
│       ├── characters/         # 角色立绘 / 选角图 / 头像
│       ├── powers/             # 能力图标
│       ├── relics/             # 遗物图标
│       ├── scenes/             # 战斗场景 tscn / 选角背景
│       └── localization/
│           ├── zhs/            # 简体中文本地化（JSON）
│           └── eng/            # 英文本地化
│
└── tests/                      # 测试脚手架（真正的功能测试只能在游戏内进行）
```

---

## 7. 添加 / 修改内容

### 7.1 加新卡

在 `src/Game/CharacterContent/Cards/` 新建一个 `.cs` 文件，参考已有卡（如 `Catharsis.cs`）：

```csharp
[Pool(typeof(MzmCharCardPool))]               // 自动加入角色卡池
public class MyNewCard : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mynewcard.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(8, ValueProp.Move),     // 显示变量：伤害
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    public MyNewCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(play.Target).Execute(ctx);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("我的新卡", "造成{Damage:diff()}点伤害。"),
        _     => new CardLoc("My New Card", "Deal {Damage:diff()} damage."),
    };
}
```

把卡图丢到 `pack/MzmChar/cards/mynewcard.png`。`dotnet build` 之后游戏会自动识别。

**关键的本地化格式**：`{Damage:diff()}` —— 大驼峰命名的变量名 + `:diff()`，让升级前后的值都显示且能正确算入活力 / 力量等加成。

### 7.2 加新能力（Power）

在 `src/Game/CharacterContent/Powers/` 下继承 `CustomPowerModel`：

```csharp
public class MyPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string? CustomPackedIconPath => "res://MzmChar/powers/mypower.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        Flash();
        await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner, Amount, Owner, null, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("我的能力", "回合开始时，获得X点力量。", "回合开始时，获得{Amount}点力量。"),
        _     => new PowerLoc("My Power",  "At turn start, gain Strength.",  "At turn start, gain {Amount} Strength."),
    };
}
```

`PowerLoc` 三个参数依次是：标题、卡牌悬停时的简略描述、能力图标悬停时的详细描述（可以包含 `{Amount}`）。

### 7.3 加新遗物

在 `src/Game/CharacterContent/Relics/` 下继承 `CustomRelicModel`，挂 `[Pool(typeof(MzmCharRelicPool))]`，并重写你需要的钩子（例如 `AfterRoomEntered`、`BeforePlayerTurnStart` 等）。

### 7.4 改战斗背景音乐

直接往 `pack/MzmChar/audio/` 丢 `.mp3` / `.ogg` / `.wav` 文件，下次战斗自动加入随机池（实现见 `CustomBgmPatch.cs`）。

### 7.5 改建筑师以及其他先古之民的对话

编辑 `pack/MzmChar/localization/zhs/ancients.json` 和 `eng/ancients.json`。每个先古 × 每次访问的对话都是一组键值对。建筑师（Architect）是众多先古中较特殊的一位，跟其他先古一样在这两份 JSON 里维护。

### 7.6 改通用界面 / 设置 / 关键字等本地化文本

JSON 文件都在 `pack/MzmChar/localization/{zhs,eng}/`，包括 `settings_ui.json`、`card_keywords.json` 等。改完重新构建即可生效。

### 7.7 重要原则

**调用游戏本体命令时优先走 `Sts2Compat`**——例如：

```csharp
// ✗ 不要这样写
await PowerCmd.Apply<StrengthPower>(target, 2, source, this, false);

// ✓ 走包装器
await Sts2Compat.PowerApply<StrengthPower>(ctx, target, 2, source, this, false);
```

这样以后游戏本体改签名时只需要改 `Sts2Compat.cs` 一个文件。

---

## 8. 常见故障排查

| 现象 | 可能原因 |
|---|---|
| `dotnet build` 报 GameDir 未配置 | 没创建 `local.props` 或路径写错 |
| `dotnet build` 报找不到 sts2.dll | `GameDir` 指向了错误的位置，不是游戏的安装根目录 |
| `dotnet build` 部署失败、dll 被锁 | 游戏正在运行——关闭游戏再重新构建 |
| `dotnet build` 时 MegaDot 报错 | `MegaDotExe` 路径错；要用 `_console.exe` 那一版而不是图形界面版 |
| 游戏启动崩溃 | `BaseLib` 没装、版本不对，或 mod 跟当前游戏分支不匹配（例如 stable 版构建产物跑在 beta 游戏上） |
| 游戏日志第一行 ERROR `old-style dependencies` | 部署的清单是旧版结构（stable 用的），但游戏是 beta 分支——重新构建一次，构建脚本会自动换上 beta 版清单 |
| 卡牌伤害显示不带力量 / 活力加成 | 本地化文本里写的是 `{Damage}`，缺少 `:diff()`，应改为 `{Damage:diff()}` |

---

---

# Developer Guide (English)

## 1. What this project is

A **custom-character mod** for *Slay the Spire 2* that adds **Wakaba Mutsumi (若叶睦)**. Contains 88 character-specific cards, a dedicated relic pool, 5 themed combat BGM tracks, bilingual UI (Simplified Chinese / English), and Architect dialogue.

The mod ships as a .NET 9 class library (`MzmChar.dll`) + a Godot resource pack (`MzmChar.pck`) + a JSON manifest (`MzmChar.json`). Drop all three into the game's `mods/MzmChar/` directory and it works.

## 2. Tech stack you should know

| Name | Role | Docs |
|---|---|---|
| **C# / .NET 9** | Mod language / runtime | [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) |
| **Godot 4.5.1** | Engine the base game uses; mod assets use Godot's resource format | — |
| **MegaDot** | MegaCrit's Godot CLI fork — exports `pack/` into `.pck` | [megadot.megacrit.com](https://megadot.megacrit.com/) |
| **[BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2)** | Community mod framework providing `CustomCardModel` / `CustomCharacterModel` etc. so your content auto-registers. **Players also need to install BaseLib.** | [Wiki](https://alchyr.github.io/BaseLib-Wiki/) |
| **[Harmony](https://github.com/pardeike/Harmony)** | Runtime prefix/postfix patching of game methods (e.g. for BGM swap) | [Wiki](https://github.com/pardeike/Harmony/wiki) |
| **Steam STS2** | You must have the game installed; mod compiles against the game's `sts2.dll` | — |

## 3. One-time setup

### 3.1 Tools

1. **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** — verify with `dotnet --version` → 9.x
2. **Slay the Spire 2** on Steam — either branch (stable / public-beta) works
3. **[BaseLib mod](https://github.com/Alchyr/BaseLib-StS2/releases/latest)** — extract latest release zip into `<GameDir>/mods/BaseLib/`. **Critical** — without BaseLib your mod will crash on launch
4. **[MegaDot](https://megadot.megacrit.com/)** — download the `*_console.exe` variant (headless mode gives readable stdout) and extract anywhere

### 3.2 Configure `local.props`

In the project root, copy `local.props.example` → `local.props` and update both paths:

```xml
<GameDir>C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2</GameDir>
<MegaDotExe>C:\path\to\MegaDot_v4.5.1-stable_mono_win64_console.exe</MegaDotExe>
```

- `GameDir` → the game install root (the folder containing `SlayTheSpire2.exe`)
- `MegaDotExe` → the **full path** to the MegaDot executable you downloaded

`local.props` is gitignored — each developer has their own.

## 4. Build

```bash
dotnet build
```

This automatically:
1. Compiles `src/MzmChar.csproj` → `bin/Debug/MzmChar.dll`
2. Runs MegaDot to export `pack/` (images, audio, scenes, loc JSON) → `MzmChar.pck`
3. Copies `MzmChar.dll` + `MzmChar.pck` + `MzmChar.json` into `<GameDir>/mods/MzmChar/`

After that, just launch the game — the mod is loaded.

**Important**: **close the game before building** — otherwise the dll is locked and deployment fails.

Game logs are at `%AppData%\Roaming\SlayTheSpire2\logs\godot*.log`. `godot.log` is the latest, timestamped files are history. Check the log first when things break.

## 5. stable / beta dual-version compatibility

### 5.1 Background

Slay the Spire 2 currently has two Steam branches:
- **stable** (default): currently v0.103.x
- **public-beta** (opt-in): currently v0.105.x

The `sts2.dll` between these is **API-incompatible**. IL-verified differences:

| API | stable signature | beta signature |
|---|---|---|
| `PowerCmd.Apply<T>` | `(target, amount, applier, card, silent)` | `(ctx, target, amount, applier, card, silent)` |
| `PowerCmd.ModifyAmount` | `(power, offset, applier, card, silent)` | `(ctx, power, offset, applier, card, silent)` |
| `CardPileCmd.AddGeneratedCard(s)ToCombat` | had `addedByPlayer` param | dropped `addedByPlayer`, added required `Player creator` |
| `PowerModel.IsInstanced` | `virtual bool IsInstanced` | renamed to `virtual PowerInstanceType InstanceType` enum |
| `CardPile.maxCardsInHand` | lowercase | renamed to `MaxCardsInHand` |
| `ModManifest` JSON schema | `dependencies: ["BaseLib"]` strings | `dependencies: [{ "id": "...", "min_version": "..." }]` objects + required `min_game_version` |

A single dll **cannot run on both branches** — you have to build separately per target.

### 5.2 Auto-detection

`Directory.Build.props` reads `<GameDir>/release_info.json`'s `version` field at build time. If it contains `v0.105` / `v0.106` / `v0.107` / `v0.108` / `v0.109` / `v0.11` → automatically defines a `BETA` constant, and code paths under `#if BETA` use the beta API. Otherwise the stable API path is used.

**Meaning**: switching Steam between stable and beta makes `dotnet build` automatically produce the matching dll. **You don't have to flip any switch manually.**

If you need to force-override, run `dotnet build -c Beta` (forces `BETA` regardless of `release_info.json`).

### 5.3 Files involved in the dual-version mechanism

- `Directory.Build.props` — `BETA` auto-detection logic
- `src/Game/Sts2Compat.cs` — **central** wrappers for cross-version vanilla APIs (e.g. `Sts2Compat.PowerApply<T>(ctx, ...)`). Business code goes through the wrappers, never directly through vanilla cmds
- `src/Game/CharacterContent/Powers/*.cs` — 5 `IsInstanced` / `InstanceType` overrides guarded by `#if BETA`
- `MzmChar.json` — stable manifest (old schema)
- `MzmChar.beta.json` — beta manifest (new schema with `min_game_version` + ModDependency objects)
- `src/MzmChar.csproj` — picks which manifest to deploy based on `BETA` define

### 5.4 Releasing both versions

Manually build twice before release:

```bash
# Step 1: switch Steam to stable → let Steam download the stable game files
dotnet build
# Take mods/MzmChar/MzmChar.dll → rename → zip as MzmChar-stable.zip

# Step 2: switch Steam to public-beta → let Steam download the beta game files
dotnet build
# Take mods/MzmChar/MzmChar.dll → zip as MzmChar-beta.zip
```

Publish both zips. Players download the one matching their game branch.

### 5.5 Long term: when stable catches up to v0.105+

Once Steam stable also reaches v0.105 or later (probably within 1–3 months), both branches will share the same API and you can **delete all `#if BETA / #else` blocks** and revert to a single build / single dll / single zip. Checklist for that time:

1. Delete all `#else` branches in `Sts2Compat.cs`, keep only the `#if BETA` body
2. Delete the 5 `#else IsInstanced` branches in Power files
3. Replace `Sts2Compat.MaxCardsInHand` in `ConcertPower.cs` with direct `CardPile.MaxCardsInHand`
4. Drop the manifest conditional copy in csproj, just use `MzmChar.beta.json` (rename to `MzmChar.json`)
5. Drop the BETA auto-detect block in `Directory.Build.props`

## 6. Project layout

```
StS-MzmChar/
├── MzmChar.sln                 # IDE entry
├── Directory.Build.props       # Shared MSBuild + auto-imports local.props + auto-detects BETA define
├── local.props.example         # Per-machine path template (copy → local.props and edit)
├── MzmChar.json                # Mod manifest (stable old schema)
├── MzmChar.beta.json           # Mod manifest (beta new schema)
│
├── src/                        # C# code
│   ├── MzmChar.csproj
│   ├── ModEntry.cs             # Mod entry ([ModInitializer])
│   ├── Config/                 # Mod settings panel (BaseLib SimpleModConfig)
│   └── Game/
│       ├── Sts2Compat.cs       # Cross-stable/beta vanilla API wrappers
│       ├── MutsumiCharacter.cs # CustomCharacterModel main class
│       ├── CustomBgmPatch.cs   # Combat BGM swap Harmony patch
│       └── CharacterContent/
│           ├── MzmCharBaseCard.cs   # Shared card base class
│           ├── MzmCharCardPool.cs   # Character-specific card pool
│           ├── MzmCharRelicPool.cs  # Character-specific relic pool
│           ├── Forms.cs             # Dual-persona (Mu / Mo) switching helper
│           ├── CombatCounters.cs    # Per-combat shared counters
│           ├── Cards/               # Cards (one .cs each, extends MzmCharBaseCard)
│           ├── Powers/              # Custom Powers (buff / debuff)
│           ├── Relics/              # Relics
│           └── ArchitectDialogue.cs # Architect dialogue injection
│
├── pack/                       # Godot asset project (MegaDot exports to MzmChar.pck)
│   ├── project.godot
│   ├── export_presets.cfg
│   └── MzmChar/
│       ├── audio/              # Combat BGM mp3 / ogg / wav
│       ├── cards/              # Card portraits
│       ├── characters/         # Character art / select screen / icon
│       ├── powers/             # Power icons
│       ├── relics/             # Relic icons
│       ├── scenes/             # Combat scenes / select-screen background
│       └── localization/
│           ├── zhs/            # Simplified Chinese loc table (JSON)
│           └── eng/            # English loc table
│
└── tests/                      # Stub tests (real testing happens in-game)
```

## 7. Adding / modifying content

### 7.1 New card

Create a `.cs` in `src/Game/CharacterContent/Cards/`, modeled after existing ones like `Catharsis.cs`:

```csharp
[Pool(typeof(MzmCharCardPool))]               // auto-add to character pool
public class MyNewCard : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mynewcard.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(8, ValueProp.Move),     // display var: damage
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    public MyNewCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(play.Target).Execute(ctx);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("我的新卡", "造成{Damage:diff()}点伤害。"),
        _     => new CardLoc("My New Card", "Deal {Damage:diff()} damage."),
    };
}
```

Drop the portrait at `pack/MzmChar/cards/mynewcard.png`. `dotnet build` and the game picks it up.

**Key loc format**: `{Damage:diff()}` — PascalCase var name + `:diff()` so both base/upgraded values render and modifiers (Vigor / Strength) are applied.

### 7.2 New Power

Extend `CustomPowerModel` in `src/Game/CharacterContent/Powers/`:

```csharp
public class MyPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string? CustomPackedIconPath => "res://MzmChar/powers/mypower.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        Flash();
        await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner, Amount, Owner, null, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("我的能力", "回合开始时，获得X点力量。", "回合开始时，获得{Amount}点力量。"),
        _     => new PowerLoc("My Power",  "At turn start, gain Strength.",  "At turn start, gain {Amount} Strength."),
    };
}
```

`PowerLoc` three args: title, card-hover short description, active-buff detailed description (with `{Amount}`).

### 7.3 New relic

Extend `CustomRelicModel` in `src/Game/CharacterContent/Relics/` with `[Pool(typeof(MzmCharRelicPool))]`. Override the hooks you need (e.g. `AfterRoomEntered`, `BeforePlayerTurnStart`).

### 7.4 New BGM

Drop `.mp3` / `.ogg` / `.wav` into `pack/MzmChar/audio/`. Auto-added to the random pool next combat (logic in `CustomBgmPatch.cs`).

### 7.5 Architect and other ancients' dialogue

Edit `pack/MzmChar/localization/zhs/ancients.json` and `eng/ancients.json`. Each ancient × each visit is a set of key-value pairs. The Architect is one (notable) ancient among several — they all live in these two JSON files.

### 7.6 UI / settings / keyword localization

All JSON files live under `pack/MzmChar/localization/{zhs,eng}/`, including `settings_ui.json`, `card_keywords.json`, etc. Rebuild after editing.

### 7.7 Important rule

**Prefer `Sts2Compat` over raw vanilla cmds**:

```csharp
// ✗ Don't write this
await PowerCmd.Apply<StrengthPower>(target, 2, source, this, false);

// ✓ Use the wrapper
await Sts2Compat.PowerApply<StrengthPower>(ctx, target, 2, source, this, false);
```

Future vanilla signature changes only need updating `Sts2Compat.cs`.

## 8. Common troubleshooting

| Symptom | Likely cause |
|---|---|
| `dotnet build` complains GameDir not set | No `local.props` or wrong path |
| `dotnet build` says sts2.dll not found | `GameDir` points to the wrong place — should be the game install root |
| Build fails: dll locked | Game is running — close the game, then build |
| MegaDot errors | Wrong `MegaDotExe` path; use the `_console.exe` variant, not the GUI one |
| Game crashes on launch | `BaseLib` missing / wrong version / mod-game-branch mismatch (stable-built dll on beta game etc.) |
| Game log ERROR line 1: `old-style dependencies` | manifest is old schema (stable) but game is beta — rebuild and the beta manifest will be used |
| Card damage display ignores Strength / Vigor | `{Damage}` in loc missing `:diff()` — should be `{Damage:diff()}` |
