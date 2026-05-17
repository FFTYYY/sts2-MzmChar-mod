# pack/

这里是 Godot 项目目录。MegaDot 在构建期把它打包成 `MzmChar.pck` 一并部署进游戏。

## 当前内容

```
pack/MzmChar/
├── characters/
│   ├── portrait.png   # 1024x1024 — 战斗中角色立绘
│   ├── select.png     #  512x768  — 选角界面立绘
│   └── button.png     #  256x256  — 选角按钮 / 头像
├── cards/
│   ├── strike.png     #  512x384 — Strike 牌面
│   └── defend.png     #  512x384 — Defend 牌面
└── relics/
    └── starter.png    #  256x256 — 起始遗物
```

这些都是带文字标签的占位 PNG（不同纯色背景），方便你直接看到「哪个图替哪个槽」。
**直接覆盖同名文件就能换图。** 想改路径，去 `src/Content/.../*.cs` 里改 `ArtPath` 字段。

## 让美术真正进游戏（一次性配置）

1. 安装 [MegaDot](https://megadot.megacrit.com/)（基于 Godot 4.5.1 .NET 的官方定制版）。
2. 用 MegaDot 打开 `pack/` 目录，点 "Import"；它会在这里生成 `project.godot` + `.godot/`。
3. 在 `local.props` 里设置 `MegaDotExe` 指向 MegaDot 可执行文件。
4. 之后 `dotnet build` 会自动调 MegaDot 把 `pack/` 导出为 `MzmChar.pck` 跟着 dll 一起部署。

> 没设 `MegaDotExe` 时构建静默跳过 .pck 步骤，dll 仍会正常部署 —— 在你还没碰美术的早期阶段不会被卡。
