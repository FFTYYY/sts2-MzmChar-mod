using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 华丽谢幕：1 费金色技能。如果这是最后一张手牌，对全体敌人造成 5/8 点伤害 4 次。演奏 / 虚无 / 消耗。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MuGrandFinale : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mzmchar_grand_finale.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(5, ValueProp.Move),
        new DynamicVar("Hits", 4m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new()
    {
        CardKeyword.Ethereal, CardKeyword.Exhaust, MzmCharKeywords.Perform,
    };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
            yield return HoverTipFactory.FromPower<ConcertPower>();
        }
    }

    public MuGrandFinale() : base(1, CardType.Attack, CardRarity.Rare, TargetType.Self) { }

    // 演奏会中且手中只剩这张牌时金色发光（出了它就触发全敌伤害）。模仿 vanilla GoForTheEyes
    // 的「override ShouldGlowGoldInternal + 检查 combat 状态」模式，不动 IsPlayable
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (CombatState == null || !IsInConcert()) return false;
            var hand = PileType.Hand.GetPile(Owner);
            return hand != null && hand.Cards.Count == 1 && hand.Cards.Contains(this);
        }
    }

    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3); /* 5→8 */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!IsInConcert())
        {
            await PlayCast();
            await Sts2Compat.PowerApply<PerformancePassionPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        }
        else
        {
            var hand = PileType.Hand.GetPile(Owner);
            int handCount = hand?.Cards.Count ?? 0;
            if (handCount == 0)   // OnPlay 时本卡已经移出 hand，所以 0 = 这是最后一张
            {
                var cs = Owner.Creature.CombatState;
                if (cs != null && cs.HittableEnemies.Count > 0)
                {
                    // 镜像 vanilla Silent「华丽谢幕」OnPlay（IL-verified）：
                    //   1. 蓄力 VFX：NGrandFinaleVfx 挂到 CombatVfxContainer
                    //   2. await Cmd.Wait(totalAnticipationDuration, ignoreCombatEnd: false)
                    //   3. 每次命中 VFX：NGrandFinaleImpactVfx.Create（Impact 类，跟蓄力不是同一个）
                    //   4. 每次命中音效："blunt_attack.mp3" 走 WithHitFx 第 3 参 tmpSfx
                    // NHorizontalLinesVfx._Ready 的 vanilla bug 由 NHorizontalLinesVfxReadyPatch
                    // (src/Game/VanillaVfxPatch.cs) 全局兜底
                    var anticipationVfx = NGrandFinaleVfx.Create(Owner.Creature);
                    if (anticipationVfx != null)
                    {
                        var combatRoom = NCombatRoom.Instance;
                        if (combatRoom != null)
                            combatRoom.CombatVfxContainer.AddChildSafely(anticipationVfx);
                    }
                    await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration, false);

                    int hits = (int)DynamicVars["Hits"].BaseValue;
                    await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                        .FromCard(this).TargetingAllOpponents(cs)
                        .WithHitCount(hits)
                        .WithHitVfxNode(NGrandFinaleImpactVfx.Create)
                        .WithHitFx(null, null, "blunt_attack.mp3")
                        .Execute(ctx);
                }
            }
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("华丽谢幕",
            "{ShowRealEffect:show:如果这是你最后一张手牌，对全体敌人造成{Damage:diff()}点伤害{Hits}次。|非演奏会回合，获得1点[gold]演艺热情[/gold]。}"),
        _ => new CardLoc("Grand Finale",
            "{ShowRealEffect:show:If this is your last card in hand, deal {Damage:diff()} damage to ALL enemies {Hits} times.|Outside concert, gain 1 [gold]Performance Passion[/gold].}"),
    };
}
