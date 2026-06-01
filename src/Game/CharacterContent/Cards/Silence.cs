using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 沉默：1 费技能。
///   小睦：抽 1 张牌。本回合小墨每打出过一张牌，额外抽 1 张（升级后额外抽 2 张）。
///         总抽数 = 1 + MoCards × (1 or 2)，用 LambdaVar "ActualDraws" 实算显示。
///   小墨：获得 5/8 格挡，进入小睦。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Silence : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/silence.png";

    private const int BaseDraw = 1;

    private readonly List<DynamicVar> _vars;

    public Silence() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        _vars = new List<DynamicVar>
        {
            new BlockVar(5, ValueProp.Move),       // Mo block 5/8
            new DynamicVar("PerMo", 1m),            // 每张 Mo 卡额外抽：1（升级 +1 → 2）
            // 实算总抽数 = BaseDraw + MoCards × PerMo
            // EnchantedValue=BaseDraw / PreviewValue=total → :diff() 在 total > BaseDraw 时框架自动绿色染色
            new GrowingDrawsVar(),
        };
    }

    /// <summary>BaseDraw 基线 + 本回合 Mo 出牌数 × PerMo → diff 染色：超过基线绿色显示。参 MirrorDoll.GrowingHitsVar。</summary>
    private class GrowingDrawsVar : DynamicVar
    {
        public GrowingDrawsVar() : base("ActualDraws", BaseDraw) { }
        public override void UpdateCardPreview(CardModel card, CardPreviewMode mode, Creature? target, bool runGlobalHooks)
        {
            int total = BaseDraw;
            if (card.Owner != null)
            {
                int mo = CombatCounters.GetMortisCardsThisTurn(card.Owner);
                int perMo = (int)card.DynamicVars["PerMo"].BaseValue;
                total = BaseDraw + mo * perMo;
            }
            BaseValue = total;            // 让 {ActualDraws}（不带 :diff()）也显示当前值
            EnchantedValue = BaseDraw;    // diff 基线 = 1
            PreviewValue = total;         // 实际值，比基线大→绿色
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    // Mo 分支「进入小睦」 → 挂 EnterMu tooltip
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { foreach (var t in FormTooltips.EnterMu()) yield return t; }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);                // Mo: 5 → 8
        DynamicVars["PerMo"].UpgradeValueBy(1);             // 每 Mo 抽数 1 → 2
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
            int mo = CombatCounters.GetMortisCardsThisTurn(Owner);
            int perMo = (int)DynamicVars["PerMo"].BaseValue;
            int total = BaseDraw + mo * perMo;
            if (total > 0)
                await CardPileCmd.Draw(ctx, total, Owner, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("沉默",
            "{MuSec}{MuOpen}小睦{MuClose}：抽{ActualDraws:diff()}张牌。本回合[gold]小墨[/gold]每打出过一张牌，额外抽{PerMo:diff()}张牌。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：获得{Block:diff()}点[gold]格挡[/gold]。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Silence",
            "{MuSec}{MuOpen}Mu{MuClose}: Draw {ActualDraws:diff()}. For each card played in [gold]Mo[/gold] form this turn, draw {PerMo:diff()} more.{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Gain {Block:diff()} [gold]Block[/gold]; [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
