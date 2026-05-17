using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 人格解体：每切换一次人格，随机给牌组中 Amount 张牌添加「虚无」(Ethereal)。
/// 切人格触发由 Forms.OnPersonaSwitched 派发到 OnPersonaSwitch 方法。
/// </summary>
public class DisintegrationPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/disintegration.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/disintegration.png";

    public Task OnPersonaSwitch(PlayerChoiceContext ctx, CardModel? source)
    {
        var player = Owner?.Player;
        if (player?.PlayerCombatState == null) return Task.CompletedTask;

        // 收集所有当前战斗里玩家的牌（手 / 抽 / 弃 / 消耗）。不改 master deck 因为战斗实例是副本
        var allCards = player.PlayerCombatState.AllCards
            .Where(c => !c.Keywords.Contains(CardKeyword.Ethereal)).ToList();
        if (allCards.Count == 0) return Task.CompletedTask;

        var rng = player.RunState?.Rng?.CombatCardSelection;
        if (rng == null) return Task.CompletedTask;

        Flash();
        int n = (int)Amount;
        for (int i = 0; i < n && allCards.Count > 0; i++)
        {
            var pick = rng.NextItem(allCards);
            if (pick == null) break;
            // 参考 vanilla SCULPTING_STRIKE：用 CardCmd.ApplyKeyword 而不是裸 AddKeyword（多人同步 / 动画）
            CardCmd.ApplyKeyword(pick, new[] { CardKeyword.Ethereal });
            allCards.Remove(pick);
        }
        return Task.CompletedTask;
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("人格解体",
            "你每切换一次人格，随机给牌组中一张牌添加[gold]虚无[/gold]。",
            "你每切换一次人格，随机给牌组中{Amount}张牌添加[gold]虚无[/gold]。"),
        _ => new PowerLoc("Disintegration",
            "Whenever you switch personas, add [gold]Ethereal[/gold] to a random card in your deck.",
            "Whenever you switch personas, add [gold]Ethereal[/gold] to {Amount} random cards in your deck."),
    };
}
