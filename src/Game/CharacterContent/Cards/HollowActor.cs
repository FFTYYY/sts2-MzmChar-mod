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
/// 空心演员：X 费蓝色技能。获得 X 层[gold]转换人格[/gold]。
/// X 费实现参考 vanilla Whirlwind（HasEnergyCostX => true, ResolveEnergyXValue()）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class HollowActor : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/hollow_actor.png";

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<TransformPersonaPower>(); }
    }

    protected override void AddExtraArgsToDescription(MegaCrit.Sts2.Core.Localization.LocString description)
    {
        base.AddExtraArgsToDescription(description);
        // X 预览 = 当前可用能量。canonical（卡库）时 Owner getter 直接抛 → 必须先短路
        int x = IsCanonical ? 0 : (Owner?.PlayerCombatState?.Energy ?? 0);
        int amt = IsUpgraded ? (x + 1) : x;
        description.Add("XValue", (decimal)amt);
    }

    public HollowActor() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade() { /* 升级让 X+1 层（OnPlay + 描述里都 IsUpgraded gated） */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        int x = ResolveEnergyXValue();
        int amount = IsUpgraded ? (x + 1) : x;
        if (amount > 0)
            await Sts2Compat.PowerApply<TransformPersonaPower>(ctx, Owner.Creature, amount, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("空心演员",
            "获得{IfUpgraded:show:X+1|X}层[gold]转换人格[/gold]。（获得{XValue}层[gold]转换人格[/gold]）"),
        _ => new CardLoc("Hollow Actor",
            "Gain {IfUpgraded:show:X+1|X} [gold]Transform Persona[/gold]. (Gain {XValue} [gold]Transform Persona[/gold])"),
    };
}
