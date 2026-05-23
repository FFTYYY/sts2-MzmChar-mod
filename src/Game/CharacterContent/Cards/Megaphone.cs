using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 传话筒：2 费能力（升级 1 费）（蓝色）。
/// 出牌时根据当前形态加对应版本 power（小睦版回合开始多抽 1，小墨版回合开始一张 0 费）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Megaphone : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/megaphone.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<MegaphoneMutsumiPower>();
            yield return HoverTipFactory.FromPower<MegaphoneMortisPower>();
        }
    }

    public Megaphone() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        if (Forms.IsMortisForm(Owner))
        {
            await Sts2Compat.PowerApply<MegaphoneMortisPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        }
        else
        {
            await Sts2Compat.PowerApply<MegaphoneMutsumiPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("传话筒",
            "{MuSec}{MuOpen}小睦{MuClose}：回合开始时，额外抽1张牌。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：回合开始时，手牌中随机一张牌本回合耗能变为0。{MoSecEnd}"),
        _ => new CardLoc("Megaphone",
            "{MuSec}{MuOpen}Mu{MuClose}: Start of turn, draw 1 extra.{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Start of turn, a random hand card costs 0 energy this turn.{MoSecEnd}"),
    };
}
