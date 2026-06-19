# MzmChar开发者指南

> 最后更新：2026-06-19（适用游戏版本：v0.107.0+，stable/beta通用）。
> 中文部分在前，英文版本在文档下半部分。
>
> **English readers**: the English version is in the second half of this file — jump to [Developer Guide (English)](#developer-guide-english). Last updated: 2026-06-19 (game v0.107.0+, stable / beta share one dll).

---

## 1. 项目是什么

本项目是为杀戮尖塔2开发的自定义角色mod，加入新角色「若叶睦 / Wakaba Mutsumi」，包含90张专属卡牌、9个专属遗物、3个专属药水、10首专属BGM、中英双语界面，以及建筑师（Architect）与其他先古之民的对话内容。

mod本身是一个.NET 9类库（`MzmChar.dll`）+ 一份Godot打包的资源包（`MzmChar.pck`）+ 一份元数据JSON（`MzmChar.json`），三个文件一起塞进游戏的`mods/MzmChar/`目录就可以生效。

---

## 2. 你需要了解的技术栈

| 名称 | 角色 | 文档 |
|---|---|---|
| C# / .NET 9 | mod主体的编程语言与运行时 | [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) |
| Godot 4.5.1 | 游戏本体使用的引擎，mod的资源也走它的格式 | — |
| MegaDot | MegaCrit改造的Godot命令行工具，把`pack/`下的资源导出成`.pck` | [megadot.megacrit.com](https://megadot.megacrit.com/) |
| [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) | 社区mod框架。提供`CustomCardModel`/`CustomCharacterModel`等基类，让自定义内容能够自动注册到游戏。玩家也必须安装这个mod | [Wiki](https://alchyr.github.io/BaseLib-Wiki/) |
| [Harmony](https://github.com/pardeike/Harmony) | 运行时给游戏方法挂前置/后置补丁，用来修改游戏本体的行为（例如战斗背景音乐替换） | [Wiki](https://github.com/pardeike/Harmony/wiki) |
| Steam上的杀戮尖塔2 | 你的机器上必须安装一份游戏。mod编译时会引用游戏的`sts2.dll` | — |

---

## 3. 一次性环境配置

### 3.1 装好基础工具

1. [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)——执行`dotnet --version`输出9.x即可
2. Steam上的杀戮尖塔2 v0.107.0或更新版本——stable或beta分支都可以（两个分支当前是同一份dll）
3. [BaseLib mod](https://github.com/Alchyr/BaseLib-StS2/releases/latest) v3.3.0或更新版本——下载最新发布的zip，解压到`<游戏目录>/mods/BaseLib/`。**这一步关键**，没有安装BaseLib或者装了低于3.3.0的旧版本，你构建出来的mod进游戏就会崩溃，或者联机消息走不通
4. [MegaDot](https://megadot.megacrit.com/)——下载`*_console.exe`那一版（无界面模式能拿到标准输出便于排查），解压到任意位置

### 3.2 复制`local.props`

项目根目录有一份`local.props.example`。把它复制一份改名为`local.props`，编辑里面两条路径：

```xml
<GameDir>C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2</GameDir>
<MegaDotExe>C:\path\to\MegaDot_v4.5.1-stable_mono_win64_console.exe</MegaDotExe>
```

- `GameDir`指向游戏的安装根目录（包含`SlayTheSpire2.exe`那一级）
- `MegaDotExe`是你刚下载的MegaDot那个`.exe`的完整路径

`local.props`已经在`.gitignore`里，不会被提交，每个开发者各自配各自的。

---

## 4. 构建

```bash
dotnet build
```

会自动完成以下步骤：

1. 编译`src/MzmChar.csproj` → `bin/Debug/MzmChar.dll`
2. 调用MegaDot把`pack/`下的Godot资源（图、音频、场景、本地化JSON）导出成`MzmChar.pck`
3. 把`MzmChar.dll` + `MzmChar.pck` + `MzmChar.json`拷贝到`<GameDir>/mods/MzmChar/`

只要这一步成功，启动游戏就能识别mod。

**重要**：构建之前要关闭游戏，否则dll文件被锁，部署会失败。

游戏日志在`%AppData%\Roaming\SlayTheSpire2\logs\godot*.log`——`godot.log`是最新一次启动的日志，带时间戳的是历史日志。出问题先看日志。

---

## 5. 版本兼容

### 5.1 现状

游戏stable和beta当前都是v0.107.1（同一个commit），API完全一致，**一份dll通吃两个分支**。日常开发不需要管BETA。`dotnet build`跑一遍出来的dll两个分支都能装。

历史上（v0.103 → v0.105）stable和beta的`sts2.dll`有过API分歧（`PowerCmd.Apply`加ctx、`IsInstanced`改`InstanceType`等），现在已经统一。

### 5.2 BETA入口骨架（占位，当前未启用）

为了应对未来beta又破坏API的情况，代码里留了三处骨架，但业务文件里没有`#if BETA`分支：

| 入口 | 作用 |
| --- | --- |
| `Directory.Build.props`的`_IsBetaDll` | 读`<GameDir>/release_info.json`的`version`字段，匹配触发字符串才define BETA |
| `src/Game/Sts2Compat.cs` | vanilla cmd集中入口。未来cmd签名分歧时只改这里包`#if BETA / #else` |
| `src/MzmChar.csproj`的双manifest切换 | `MzmChar.json`/`MzmChar.beta.json`按BETA define选一份部署。当前两份内容一致 |

当前`_IsBetaDll`触发字符串是`v0.999`，现实游戏版本不可能匹配——意味着普通`dotnet build`总是走非BETA分支。

需要强制启用BETA分支测试时：`dotnet build -c Beta`。

### 5.3 未来beta又破坏API时怎么办

简版步骤：

1. `Directory.Build.props`里把`v0.999`改回真实beta版本号（例如`v0.108`）
2. 按分歧类型分别处理：
   - vanilla cmd签名变了（例如`PowerCmd.Apply`加参） → 只改`Sts2Compat.cs`，对应wrapper内部包`#if BETA / #else`，业务文件全部不动
   - hook签名或名字变了（例如`AfterTurnEnd` → `AfterSideTurnEnd`） → 直接在受影响的业务文件里包`#if BETA / #else`（无法走Sts2Compat，因为是override而不是调用）
   - PowerModel字段变了（例如`IsInstanced` → `InstanceType`） → 业务文件里包`#if BETA / #else`
   - ModManifest schema变了 → 只改`MzmChar.beta.json`，`MzmChar.json`保持当前schema
3. 测两遍：`dotnet build`（默认，走非BETA分支）+ `dotnet build -c Beta`（强制BETA分支），两者都通过

### 5.4 发布

单次构建，单份zip：

```bash
dotnet build
# mods/MzmChar/下三件套（dll + pck + json）打包发布
```

不再需要分别给stable和beta出两份zip。

---

## 6. 项目结构

```
StS-MzmChar/
├── MzmChar.sln                 # IDE工程入口
├── Directory.Build.props       # 公共MSBuild + 自动导入local.props + 自动检测BETA常量
├── local.props.example         # 本机路径模板（复制为local.props后修改）
├── MzmChar.json                # mod元数据清单（默认部署）
├── MzmChar.beta.json           # mod元数据清单（BETA触发时部署；当前与上一份一致）
│
├── src/                        # C#代码
│   ├── MzmChar.csproj
│   ├── ModEntry.cs             # mod入口（[ModInitializer]）
│   ├── Config/                 # mod设置面板（BaseLib SimpleModConfig）
│   └── Game/
│       ├── Sts2Compat.cs       # vanilla cmd集中入口（未来版本分歧时只改这里）
│       ├── MutsumiCharacter.cs # 角色主类（继承CustomCharacterModel）
│       ├── CustomBgmPatch.cs   # 战斗背景音乐替换的Harmony补丁
│       └── CharacterContent/
│           ├── MzmCharBaseCard.cs   # 本mod卡牌的公共基类
│           ├── MzmCharCardPool.cs   # 角色专属卡池
│           ├── MzmCharRelicPool.cs  # 角色专属遗物池
│           ├── Forms.cs             # 双形态（小睦/小墨）切换辅助
│           ├── CombatCounters.cs    # 跨卡共享的战斗内计数器
│           ├── Cards/               # 卡牌（每张一个.cs，继承MzmCharBaseCard）
│           ├── Powers/              # 自定义能力（增益/减益）
│           ├── Relics/              # 遗物
│           └── ArchitectDialogue.cs # 建筑师对话注入
│
├── pack/                       # Godot资源项目（由MegaDot导出成MzmChar.pck）
│   ├── project.godot
│   ├── export_presets.cfg
│   └── MzmChar/
│       ├── audio/              # 战斗背景音乐（mp3/ogg/wav）
│       ├── cards/              # 卡牌画
│       ├── characters/         # 角色立绘/选角图/头像
│       ├── powers/             # 能力图标
│       ├── relics/             # 遗物图标
│       ├── scenes/             # 战斗场景tscn/选角背景
│       └── localization/
│           ├── zhs/            # 简体中文本地化（JSON）
│           └── eng/            # 英文本地化
│
└── tests/                      # 测试脚手架（真正的功能测试只能在游戏内进行）
```

---

## 7. 添加和修改内容

### 7.1 加新卡

在`src/Game/CharacterContent/Cards/`下新建一个`.cs`文件，参考已有的卡（例如`Catharsis.cs`）：

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

把卡图放到`pack/MzmChar/cards/mynewcard.png`。`dotnet build`之后游戏会自动识别。

关键的本地化格式：`{Damage:diff()}`——大驼峰命名的变量名 + `:diff()`，让升级前后的值都显示，并且能正确算入活力/力量等加成。

### 7.2 加新能力（Power）

在`src/Game/CharacterContent/Powers/`下继承`CustomPowerModel`：

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

`PowerLoc`三个参数依次是：标题、卡牌悬停时的简略描述、能力图标悬停时的详细描述（可以包含`{Amount}`）。

### 7.3 加新遗物

在`src/Game/CharacterContent/Relics/`下继承`CustomRelicModel`，挂`[Pool(typeof(MzmCharRelicPool))]`，并重写你需要的钩子（例如`AfterRoomEntered`、`BeforePlayerTurnStart`等）。

### 7.4 改战斗背景音乐

直接往`pack/MzmChar/audio/`下放`.mp3`/`.ogg`/`.wav`文件，下一场战斗自动加入随机池（实现见`CustomBgmPatch.cs`）。

### 7.5 改建筑师以及其他先古之民的对话

编辑`pack/MzmChar/localization/zhs/ancients.json`和`eng/ancients.json`。每个先古乘以每次访问的对话都是一组键值对。建筑师（Architect）是众多先古中比较特殊的一位，跟其他先古一样在这两份JSON里维护。

### 7.6 改通用界面/设置/关键字等本地化文本

JSON文件都在`pack/MzmChar/localization/{zhs,eng}/`，包括`settings_ui.json`、`card_keywords.json`等。改完重新构建就可以生效。

### 7.7 重要原则

**调用游戏本体命令时优先走`Sts2Compat`**——例如：

```csharp
// ✗ 不要这样写
await PowerCmd.Apply<StrengthPower>(ctx, target, 2, source, this, false);

// ✓ 走包装器
await Sts2Compat.PowerApply<StrengthPower>(ctx, target, 2, source, this, false);
```

当前`Sts2Compat`的wrapper都是pass-through（没有版本分歧），坚持走包装器是为了未来游戏本体改签名时只需要改`Sts2Compat.cs`一个文件，业务文件不动。

---

## 8. 常见故障排查

| 现象 | 可能原因 |
|---|---|
| `dotnet build`报GameDir未配置 | 没有创建`local.props`，或者路径写错了 |
| `dotnet build`报找不到sts2.dll | `GameDir`指向了错误的位置，不是游戏的安装根目录 |
| `dotnet build`部署失败、dll被锁 | 游戏正在运行——关闭游戏再重新构建 |
| `dotnet build`时MegaDot报错 | `MegaDotExe`路径错；要用`_console.exe`那一版，而不是图形界面版 |
| 游戏启动崩溃 | `BaseLib`没装，或者装的版本低于3.3.0 |
| 联机时`KeyNotFoundException ... CustomMessageWrapper.Deserialize` | `BaseLib`版本低于3.3.0，host和client之间消息key对不齐——升级BaseLib到3.3.0或更新版本 |
| 游戏日志`Mod MzmChar does not declare min game version` | 部署目录的`MzmChar.json`是过期的模板（手动用ZIP装的）——跑一次`dotnet build`，DeployMod会覆盖部署目录 |
| 卡牌伤害显示不带力量/活力加成 | 本地化文本里写的是`{Damage}`，缺少`:diff()`，应该改成`{Damage:diff()}` |

---

---

# Developer Guide (English)

## 1. What this project is

A custom-character mod for *Slay the Spire 2* that adds Wakaba Mutsumi (若叶睦). Contains 90 character-specific cards, 9 dedicated relics, 3 dedicated potions, 10 themed BGM tracks, bilingual UI (Simplified Chinese / English), and Architect dialogue.

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
2. **Slay the Spire 2 v0.107.0 or newer** on Steam — stable / beta both work (currently the same dll)
3. **[BaseLib mod](https://github.com/Alchyr/BaseLib-StS2/releases/latest) v3.3.0 or newer** — extract latest release zip into `<GameDir>/mods/BaseLib/`. **Critical** — without BaseLib (or with BaseLib < 3.3.0) the game will crash on launch or multiplayer messages won't go through
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

## 5. Version compatibility

### 5.1 Current state

Game stable and beta branches are both **v0.107.1** (same commit) — API-identical, so **one dll covers both branches**. Day-to-day development doesn't touch BETA. `dotnet build` once and the resulting dll runs on either branch.

Historically (v0.103 → v0.105) the two branches' `sts2.dll` diverged (`PowerCmd.Apply` added a ctx param, `IsInstanced` renamed to `InstanceType`, etc.). That gap has now closed.

### 5.2 BETA gate scaffolding (dormant)

In case beta diverges again, three scaffolding points remain — but **no business file contains `#if BETA` branches**:

| Entry point                                          | Role                                                                                                       |
| ---------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `Directory.Build.props` `_IsBetaDll`                 | Reads `<GameDir>/release_info.json`'s `version` field; defines BETA only on substring match                |
| `src/Game/Sts2Compat.cs`                             | Central entry to vanilla cmds. Future cmd signature changes get wrapped in `#if BETA / #else` here only    |
| `src/MzmChar.csproj` dual-manifest switch            | `MzmChar.json` / `MzmChar.beta.json` chosen by BETA define. Currently the two files are identical          |

The `_IsBetaDll` trigger string is currently `v0.999`, which no real game version will match — so a plain `dotnet build` always takes the non-BETA path.

Force-enable the BETA branch for testing: `dotnet build -c Beta`.

### 5.3 When beta breaks API again

Short version:

1. Change `v0.999` in `Directory.Build.props` back to a real beta version string (e.g. `v0.108`)
2. Handle the divergence by type:
   - **vanilla cmd signature changed** (e.g. `PowerCmd.Apply` adds a param) → edit only `Sts2Compat.cs`, wrap that wrapper's body in `#if BETA / #else`; business files don't move
   - **hook signature / name changed** (e.g. `AfterTurnEnd` → `AfterSideTurnEnd`) → wrap the affected business file's override in `#if BETA / #else` (can't be hidden in Sts2Compat because it's an override, not a call)
   - **PowerModel field changed** (e.g. `IsInstanced` → `InstanceType`) → wrap in business file
   - **ModManifest schema changed** → edit only `MzmChar.beta.json`; keep `MzmChar.json` on the current schema
3. Build twice: default `dotnet build` (non-BETA path) and `dotnet build -c Beta` (forced BETA), both must succeed

### 5.4 Release

Single build, single zip:

```bash
dotnet build
# bundle mods/MzmChar/ contents (dll + pck + json)
```

No more separate stable / beta zips.

## 6. Project layout

```
StS-MzmChar/
├── MzmChar.sln                 # IDE entry
├── Directory.Build.props       # Shared MSBuild + auto-imports local.props + auto-detects BETA define
├── local.props.example         # Per-machine path template (copy → local.props and edit)
├── MzmChar.json                # Mod manifest (deployed by default)
├── MzmChar.beta.json           # Mod manifest (deployed when BETA is defined; identical for now)
│
├── src/                        # C# code
│   ├── MzmChar.csproj
│   ├── ModEntry.cs             # Mod entry ([ModInitializer])
│   ├── Config/                 # Mod settings panel (BaseLib SimpleModConfig)
│   └── Game/
│       ├── Sts2Compat.cs       # Vanilla cmd central entry (sole place to touch on future divergence)
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
await PowerCmd.Apply<StrengthPower>(ctx, target, 2, source, this, false);

// ✓ Use the wrapper
await Sts2Compat.PowerApply<StrengthPower>(ctx, target, 2, source, this, false);
```

Today every `Sts2Compat` wrapper is a pass-through (no version split). Going through them anyway means **future vanilla signature changes only need updating `Sts2Compat.cs`** — business files don't move.

## 8. Common troubleshooting

| Symptom | Likely cause |
|---|---|
| `dotnet build` complains GameDir not set | No `local.props` or wrong path |
| `dotnet build` says sts2.dll not found | `GameDir` points to the wrong place — should be the game install root |
| Build fails: dll locked | Game is running — close the game, then build |
| MegaDot errors | Wrong `MegaDotExe` path; use the `_console.exe` variant, not the GUI one |
| Game crashes on launch | `BaseLib` missing or installed version is < 3.3.0 |
| Multiplayer `KeyNotFoundException ... CustomMessageWrapper.Deserialize` | `BaseLib` < 3.3.0 — host / client message keys don't line up; upgrade BaseLib to 3.3.0+ |
| Game log `Mod MzmChar does not declare min game version` | The deployed `MzmChar.json` is a stale template (manually ZIP-installed) — run `dotnet build` once so DeployMod overwrites it |
| Card damage display ignores Strength / Vigor | `{Damage}` in loc missing `:diff()` — should be `{Damage:diff()}` |
