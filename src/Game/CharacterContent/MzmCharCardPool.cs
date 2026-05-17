using BaseLib.Abstracts;
using Godot;

namespace MzmChar.Game;

/// <summary>
/// 角色专属卡池。继承 BaseLib.Abstracts.CustomCardPoolModel：
///   - 自动注册（如果 IsShared=true 才需要，这里是角色池所以不用）
///   - 卡通过 [Pool(typeof(MzmCharCardPool))] 自动加进来
///
/// 改主题色：调 ShaderColor 一项即可，BaseLib 会自动用 HSV 变换给卡牌外框上色。
/// </summary>
public class MzmCharCardPool : CustomCardPoolModel
{
    public override bool IsShared => false;
    public override bool IsColorless => false;

    // 主题色（深 sage 绿）—— BaseLib 会用 HSV 把基础卡框染成这个色调
    public override Color ShaderColor => new(0.75f, 0.93f, 0.75f);

    // 牌堆查看界面里卡片的底色（同 sage 系，更亮一些）
    public override Color DeckEntryCardColor => new(0.6f, 0.73f, 0.6f);

    // 能量图标 —— 必须精确尺寸（游戏不 auto-resize）：big 256x256，text 24x24
    public override string? BigEnergyIconPath => "res://MzmChar/characters/energy_big.png";
    public override string? TextEnergyIconPath => "res://MzmChar/characters/energy_text.png";

    // Title 是显示在 UI 的池名 —— BaseLib 会从 characters loc table 里找这个 key
    public override string Title => "test_hero_model.title";
}
