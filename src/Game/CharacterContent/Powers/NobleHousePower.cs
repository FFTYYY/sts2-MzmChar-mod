using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 名门：回合开始时，获得 (演艺热情 × 本 power 层数) 活力。
/// 层数 = Amount（框架自动注入到 SmartDescription）。
/// </summary>
public class NobleHousePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/noble_house.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/noble_house.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        var pp = Owner.GetPower<PerformancePassionPower>();
        int passion = pp != null ? (int)pp.Amount : 0;
        int vigorGain = passion * (int)Amount;
        if (vigorGain <= 0) return;
        Flash();
        await PowerCmd.Apply<VigorPower>(Owner, vigorGain, Owner, null, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        // 卡 hover：无数字 vague 版（canonical 拿不到 card 升级状态，写死任何数字都会在某种情况出错）
        // buff hover：{Amount} 框架自动注入，显示真实层数
        "zhs" => new PowerLoc("名门",
            "回合开始时，你每有1层[gold]演艺热情[/gold]，就获得[gold]活力[/gold]。",
            "回合开始时，你每有1层[gold]演艺热情[/gold]，就获得{Amount}层[gold]活力[/gold]。"),
        _ => new PowerLoc("Noble House",
            "At turn start, per stack of [gold]Performance Passion[/gold], gain [gold]Vigor[/gold].",
            "At turn start, per stack of [gold]Performance Passion[/gold], gain {Amount} [gold]Vigor[/gold]."),
    };
}
