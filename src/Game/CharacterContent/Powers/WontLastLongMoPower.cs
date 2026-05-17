using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 不会长久的（小墨）buff。跟 WontLastLongMuPower 完全对称，区别仅 Str → Dex。详细注释见 Mu 版本。
/// </summary>
public class WontLastLongMoPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => true;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/wont_last_long_mo.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/wont_last_long_mo.png";

    public class Data { public int Threshold; }
    protected override object InitInternalData() => new Data();

    private CardModel? _appliedFromCard;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var d = GetInternalData<Data>();
        if (d != null && d.Threshold == 0) d.Threshold = (int)Amount;
        _appliedFromCard = cardSource;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Card?.Owner != Owner?.Player) return;
        if (play.Card == _appliedFromCard)
        {
            _appliedFromCard = null;
            return;
        }

        SetAmount(Amount - 1, false);
        if (Amount <= 0)
        {
            Flash();
            await PowerCmd.Apply<DexterityPower>(Owner!, -1, Owner, null, true);
            await PowerCmd.Apply<TempDexterityPower>(Owner!, -1, Owner, null, true);

            var d = GetInternalData<Data>();
            int threshold = d?.Threshold ?? 1;
            SetAmount(threshold, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("不会长久的（墨）",
            "每打出一定数量的牌，本回合失去1点[gold]敏捷[/gold]。",
            "再打出{Amount}张牌，本回合失去1点[gold]敏捷[/gold]。"),
        _ => new PowerLoc("Won't Last Long (Mo)",
            "Per a number of cards played, lose 1 [gold]Dexterity[/gold] this turn.",
            "After {Amount} more cards, lose 1 [gold]Dexterity[/gold] this turn."),
    };
}
