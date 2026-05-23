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
/// 熬夜禁止：1 费白色技能。消耗（升级去除消耗）。
///   小睦：自己每有 3 点力量，就额外获得 1 点力量。
///   小墨：下 1 次攻击造成伤害时，活力不消失。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class NoStayingUp : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/no_staying_up.png";

    private readonly List<DynamicVar> _vars;

    public NoStayingUp() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        _vars = new List<DynamicVar>
        {
            new PowerVar<VigorPreservePower>(1),
            new LambdaVar("MuStrBonus", card =>
            {
                if (card.Owner?.Creature == null) return 0;
                var sp = card.Owner.Creature.GetPower<StrengthPower>();
                int s = sp != null ? (int)sp.Amount : 0;
                return s / 3;
            }),
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<VigorPreservePower>();
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);  // 升级去除消耗
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        if (Forms.IsMortisForm(Owner))
        {
            await Sts2Compat.PowerApply<VigorPreservePower>(ctx, Owner.Creature,
                DynamicVars["VigorPreservePower"].BaseValue, Owner.Creature, this, false);
        }
        else
        {
            var sp = Owner.Creature.GetPower<StrengthPower>();
            int bonus = sp != null ? (int)sp.Amount / 3 : 0;
            if (bonus > 0)
                await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature, bonus, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("熬夜禁止",
            "{MuSec}{MuOpen}小睦{MuClose}：自身每有3点[gold]力量[/gold]，就额外获得1点[gold]力量[/gold]（{MuStrBonus}力量）。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：下{VigorPreservePower}次攻击造成伤害时，[gold]活力[/gold]不会消失。{MoSecEnd}"),
        _ => new CardLoc("No Staying Up",
            "{MuSec}{MuOpen}Mu{MuClose}: Per 3 [gold]Strength[/gold] you have, gain 1 [gold]Strength[/gold] ({MuStrBonus}).{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: For your next {VigorPreservePower} attacks, [gold]Vigor[/gold] is not consumed.{MoSecEnd}"),
    };
}
