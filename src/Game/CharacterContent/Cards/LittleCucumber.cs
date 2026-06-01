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
/// 小黄瓜：1 费蓝色技能。消耗。
///   小睦：回复 7/10 生命，失去 2 力量。
///   小墨：失去 4/2 生命，获得 2 力量 + 2/3 能量。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class LittleCucumber : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/little_cucumber.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DynamicVar("MuHeal", 7m),
        new DynamicVar("MuStrLoss", 2m),
        new DynamicVar("MoHpLoss", 4m),    // 升级 -2 (4 → 2)
        new DynamicVar("MoStrGain", 2m),
        new EnergyVar(2),                   // 升级 +1 (2 → 3)
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<StrengthPower>(); }
    }

    public LittleCucumber() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["MuHeal"].UpgradeValueBy(3);     // 7 → 10
        DynamicVars["MoHpLoss"].UpgradeValueBy(-2);  // 4 → 2
        DynamicVars.Energy.UpgradeValueBy(1);        // 2 → 3
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        if (Forms.IsMortisForm(Owner))
        {
            // 自伤走 CreatureCmd.Damage，触发 AfterDamageReceived hook（百年积木等遗物依赖）。
            // ValueProp 14 = Unblockable | Unpowered | Move：忽略格挡、不吃力量/Frail、标记为非攻击。
            // clamped 保 ≥ 1 HP（"扣血"卡不致死）。
            int hpLoss = (int)DynamicVars["MoHpLoss"].BaseValue;
            int clamped = System.Math.Min(hpLoss, Owner.Creature.CurrentHp - 1);
            if (clamped > 0)
                await CreatureCmd.Damage(ctx, Owner.Creature, clamped,
                    ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature,
                DynamicVars["MoStrGain"].BaseValue, Owner.Creature, this, false);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        }
        else
        {
            await CreatureCmd.Heal(Owner.Creature, DynamicVars["MuHeal"].BaseValue, false);
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature,
                -DynamicVars["MuStrLoss"].BaseValue, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("小黄瓜",
            "{MuSec}{MuOpen}小睦{MuClose}：回复{MuHeal:diff()}点生命。失去{MuStrLoss}点[gold]力量[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：失去{MoHpLoss:diff()}点生命。获得{MoStrGain}点[gold]力量[/gold]和{Energy:energyIcons()}。{MoSecEnd}"),
        _ => new CardLoc("Little Cucumber",
            "{MuSec}{MuOpen}Mu{MuClose}: Heal {MuHeal:diff()}; lose {MuStrLoss} [gold]Strength[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Lose {MoHpLoss:diff()} HP; gain {MoStrGain} [gold]Strength[/gold] and {Energy:energyIcons()}.{MoSecEnd}"),
    };
}
