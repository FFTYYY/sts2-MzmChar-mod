using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 全都不会弹！：1 费金色能力卡。
///   小睦：获得 4/6 点力量，敏捷 -1
///   小墨：获得 4/6 点敏捷，力量 -1
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class CantPlayAtAll : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/cant_play.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new PowerVar<StrengthPower>(4),
        new PowerVar<DexterityPower>(4),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
        }
    }

    public CantPlayAtAll() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthPower"].UpgradeValueBy(2);
        DynamicVars["DexterityPower"].UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        if (Forms.IsMortisForm(Owner))
        {
            await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner.Creature,
                DynamicVars["DexterityPower"].BaseValue, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature, -1, Owner.Creature, this, false);
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature,
                DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner.Creature, -1, Owner.Creature, this, false);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("全都不会弹！",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{StrengthPower:diff()}点[gold]力量[/gold]和-1[gold]敏捷[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：获得{DexterityPower:diff()}点[gold]敏捷[/gold]和-1[gold]力量[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Can't Play Anything!",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {StrengthPower:diff()} [gold]Strength[/gold]; -1 [gold]Dexterity[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Gain {DexterityPower:diff()} [gold]Dexterity[/gold]; -1 [gold]Strength[/gold].{MoSecEnd}"),
    };
}
