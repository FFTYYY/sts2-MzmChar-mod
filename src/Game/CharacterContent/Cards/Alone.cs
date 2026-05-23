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
/// 独处：1 费金色技能（rare）。
///   小睦：获得 10/15 格挡。自己每有 3 层敏捷，就额外获得 1 力量。
///   小墨：造成 10/15 伤害。自己每有 3 层力量，就额外获得 1 敏捷。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Alone : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/alone.png";

    private readonly List<DynamicVar> _vars;

    public Alone() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
        _vars = new List<DynamicVar>
        {
            new DamageVar(10, ValueProp.Move),                  // Mo damage
            new BlockVar("MuBlock", 10m, ValueProp.Move),       // Mu block (custom name)
            // Mu 实算：bonus strength = floor(dex / 3)
            new LambdaVar("MuStrBonus", card =>
            {
                if (card.Owner?.Creature == null) return 0;
                var dp = card.Owner.Creature.GetPower<DexterityPower>();
                int d = dp != null ? (int)dp.Amount : 0;
                return d / 3;
            }),
            // Mo 实算：bonus dex = floor(strength / 3)
            new LambdaVar("MoDexBonus", card =>
            {
                if (card.Owner?.Creature == null) return 0;
                var sp = card.Owner.Creature.GetPower<StrengthPower>();
                int s = sp != null ? (int)sp.Amount : 0;
                return s / 3;
            }),
        };
    }

    // Mu 只对自己加格挡 + 加力量 → 不需要选目标
    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<DexterityPower>();
            yield return HoverTipFactory.FromPower<StrengthPower>();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5);              // Mo 10 → 15
        DynamicVars["MuBlock"].UpgradeValueBy(5);          // Mu 10 → 15
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).Execute(ctx);
            }
            var sp = Owner.Creature.GetPower<StrengthPower>();
            int bonus = sp != null ? (int)sp.Amount / 3 : 0;
            if (bonus > 0)
                await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner.Creature, bonus, Owner.Creature, this, false);
        }
        else
        {
            await PlayCast();
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars["MuBlock"].BaseValue, ValueProp.Move, play, false);
            var dp = Owner.Creature.GetPower<DexterityPower>();
            int bonus = dp != null ? (int)dp.Amount / 3 : 0;
            if (bonus > 0)
                await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature, bonus, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("独处",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{MuBlock:diff()}[gold]格挡[/gold]。自己每有3[gold]敏捷[/gold]，就获得1[gold]力量[/gold]（{MuStrBonus}力量）。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}伤害。自己每有3[gold]力量[/gold]，就获得1[gold]敏捷[/gold]（{MoDexBonus}敏捷）。{MoSecEnd}"),
        _ => new CardLoc("Alone",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {MuBlock:diff()} [gold]Block[/gold]. Per 3 [gold]Dexterity[/gold] you have, gain 1 [gold]Strength[/gold] ({MuStrBonus}).{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage. Per 3 [gold]Strength[/gold] you have, gain 1 [gold]Dexterity[/gold] ({MoDexBonus}).{MoSecEnd}"),
    };
}
