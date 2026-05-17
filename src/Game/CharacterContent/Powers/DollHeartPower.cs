using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 人偶之心：每打出能力（Power）卡，获得 Amount 点力量。
/// 注意：打出"人偶之心"这张卡本身**不**触发——它只对**之后**打出的能力牌生效。
/// </summary>
public class DollHeartPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/doll_heart.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/doll_heart.png";

    // 记下刚 apply 我的那张卡 → 跳过它的 AfterCardPlayed 一次，避免 power 触发自己
    private CardModel? _appliedFromCard;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _appliedFromCard = cardSource;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (cardPlay.Card?.Owner != Owner?.Player) return;
        if (cardPlay.Card?.Type != CardType.Power) return;
        // 跳过 apply 我的这张卡（首张触发是它自己的 AfterCardPlayed —— 不应得 +1）
        if (cardPlay.Card == _appliedFromCard)
        {
            _appliedFromCard = null;   // 仅跳过一次
            return;
        }
        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner!, (int)Amount, Owner, null, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("人偶之心",
            "每当你打出[gold]能力[/gold]牌，获得1点[gold]力量[/gold]。",
            "每当你打出[gold]能力[/gold]牌，获得{Amount}点[gold]力量[/gold]。"),
        _ => new PowerLoc("Doll Heart",
            "Whenever you play a [gold]Power[/gold] card, gain 1 [gold]Strength[/gold].",
            "Whenever you play a [gold]Power[/gold] card, gain {Amount} [gold]Strength[/gold]."),
    };
}
