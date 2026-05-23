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
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 沉默：1 费技能。
///   小睦：本回合中每以小墨形态打出过一张牌，立刻抽 2 张牌（一次性结算 N×2 张）。
///         升级后：额外获得 1 点能量。
///   小墨：获得 5/8 格挡，进入小睦。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Silence : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/silence.png";

    private readonly List<DynamicVar> _vars;

    public Silence() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        _vars = new List<DynamicVar>
        {
            new BlockVar(5, ValueProp.Move),     // Mo block 5/8
            new EnergyVar(1),                    // Mu 升级时给的能量
            // 实算：当前 Mu form 会抽几张（= 本回合以 Mo 形态出过的牌数 × 2）
            new LambdaVar("ActualDraws", card =>
            {
                if (card.Owner == null) return 0;
                return CombatCounters.GetMortisCardsThisTurn(card.Owner) * 2;
            }),
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    // Mo 分支「进入小睦」 → 挂 EnterMu tooltip（"转换成小睦人格"）
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { foreach (var t in FormTooltips.EnterMu()) yield return t; }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);                // Mo: 5 → 8
        // Mu 分支额外加能量靠 IsUpgraded 在 OnPlay 里判断
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            int n = CombatCounters.GetMortisCardsThisTurn(Owner) * 2;
            if (n > 0)
                await CardPileCmd.Draw(ctx, n, Owner, false);
            if (IsUpgraded)
                await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("沉默",
            "{MuSec}{MuOpen}小睦{MuClose}：本回合中[gold]小墨[/gold]每打出过一张牌，抽2张牌（抽{ActualDraws}张牌）。{IfUpgraded:show:获得{Energy:energyIcons()}。|}{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：获得{Block:diff()}点[gold]格挡[/gold]。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Silence",
            "{MuSec}{MuOpen}Mu{MuClose}: For each card played in [gold]Mo[/gold] form this turn, draw 2.\nDraw {ActualDraws}. {IfUpgraded:show:Gain {Energy:energyIcons()}.|}{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Gain {Block:diff()} [gold]Block[/gold]; [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
