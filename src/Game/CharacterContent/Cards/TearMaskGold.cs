using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 撕下面具（金）：1 费金色技能。
///   小睦：获得 5/8 格挡 × 2 次（不切形态）
///   小墨：获得 2/3 费 → 进入小睦
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class TearMaskGold : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/tear_mask_gold.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar(5, ValueProp.Move),
        new DynamicVar("Hits", 2m),
        new EnergyVar(2),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { foreach (var t in FormTooltips.EnterMu()) yield return t; }
    }

    public TearMaskGold() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);   // 5 → 8
        DynamicVars.Energy.UpgradeValueBy(1);  // 2 → 3
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            // Mo: 获得能量 + 进入小睦
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            await CombatCounters.BumpMortisCard(ctx, Owner);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            // Mu: 获得 5 格挡 × 2 次 (两次单独 GainBlock，保留"两次行为"的语义)；不切形态
            int hits = (int)DynamicVars["Hits"].BaseValue;
            for (int i = 0; i < hits; i++)
            {
                await CreatureCmd.GainBlock(Owner.Creature,
                    DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
            }
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("撕下面具",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]{Hits}次。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：获得{Energy:energyIcons()}。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Tear Off Mask",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold] {Hits} times.{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Gain {Energy:energyIcons()}. [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
