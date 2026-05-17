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
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 做鬼脸：0 费白色攻击。造成 7/10 伤害，[gold]进入小墨[/gold]。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class FunnyFace : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/funny_face.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(7, ValueProp.Move),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { foreach (var t in FormTooltips.EnterMo()) yield return t; }
    }

    public FunnyFace() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);  // 7 → 10
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this).Targeting(play.Target).Execute(ctx);
            if (Forms.IsMortisForm(Owner))
                CombatCounters.StruckByMortisThisTurn[play.Target]++;
        }
        if (Forms.IsMortisForm(Owner))
        {
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("做鬼脸",
            "造成{Damage:diff()}点伤害。[gold]进入小墨[/gold]。"),
        _ => new CardLoc("Funny Face",
            "Deal {Damage:diff()} damage; [gold]Enter Mo[/gold]."),
    };
}
