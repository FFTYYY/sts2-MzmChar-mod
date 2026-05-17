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
/// 霸凌着你：0 费蓝色技能。联机专用。
/// 令一名其他玩家获得 2 费、抽 1/2 张牌、失去 3 点生命、获得 2 层易伤。
/// 用 TargetType.AnyAlly：play.Target 是被点的队友 Creature → .Player 拿到对应 Player 实例。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class BullyingYou : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/bullying_you_test.png";

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    private readonly List<DynamicVar> _vars = new()
    {
        new EnergyVar(2),
        new CardsVar(1),
        new DynamicVar("HpLoss", 3m),
        new PowerVar<VulnerablePower>(2),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<VulnerablePower>(); }
    }

    public BullyingYou() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly) { }

    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1); /* Cards 1 → 2 */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        if (play.Target == null) return;
        // AnyAlly 包含 self（IsValidTarget 只查 Side） —— spec 是"其他玩家"，soft-block 自瞄
        if (play.Target == Owner.Creature) return;
        var allyPlayer = play.Target.Player;
        if (allyPlayer == null) return;  // 防 pet creature（PetOwner 而非 Player）

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, allyPlayer);
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, allyPlayer, false);

        // 失去生命：直接 set HP，钳制 ≥ 1（不杀死队友）
        int hpLoss = (int)DynamicVars["HpLoss"].BaseValue;
        var newHp = play.Target.CurrentHp - hpLoss;
        if (newHp < 1) newHp = 1;
        await CreatureCmd.SetCurrentHp(play.Target, newHp);

        await PowerCmd.Apply<VulnerablePower>(play.Target,
            DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, false);

        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("霸凌着你",
            "令一名其他玩家获得{Energy:energyIcons()}，抽{Cards:diff()}张牌，失去{HpLoss}点生命，并获得{VulnerablePower}层[gold]易伤[/gold]。"),
        _ => new CardLoc("Bullying You",
            "Target another ally: they gain {Energy:energyIcons()}, draw {Cards:diff()}, lose {HpLoss} HP, and gain {VulnerablePower} [gold]Vulnerable[/gold]."),
    };
}
