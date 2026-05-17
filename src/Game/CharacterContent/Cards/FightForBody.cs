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
/// 争夺身体：2 费攻击（升级后 1 费）。
///   小睦：本回合每额外抽过 1 张牌，获得 4 点格挡。进入小墨。
///   小墨：本回合每额外抽过 1 张牌，造成 5 点伤害。进入小睦。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class FightForBody : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/fight_for_body.png";

    private readonly List<DynamicVar> _vars;

    // Mu 只加格挡 → 不需要选目标；Mo 攻击需要选目标
    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    public FightForBody() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        _vars = new List<DynamicVar>
        {
            new DynamicVar("MuBlockPerDraw", 4m),
            new DynamicVar("MoDmgPerDraw", 5m),
            // 实时算 —— 两个 LambdaVar 都不门控当前形态，两边各自显示
            new LambdaVar("MuActual", card =>
            {
                if (card.Owner == null) return 0;
                int n = CombatCounters.ExtraDrawsThisTurn[card.Owner];
                return n * (int)card.DynamicVars["MuBlockPerDraw"].BaseValue;
            }, LambdaVar.ModifierKind.Block),
            new LambdaVar("MoActual", card =>
            {
                if (card.Owner == null) return 0;
                int n = CombatCounters.ExtraDrawsThisTurn[card.Owner];
                return n * (int)card.DynamicVars["MoDmgPerDraw"].BaseValue;
            }, LambdaVar.ModifierKind.Damage),
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 2 → 1 费
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int extraDraws = CombatCounters.ExtraDrawsThisTurn[Owner];

        if (Forms.IsMortisForm(Owner))
        {
            int dmg = extraDraws * (int)DynamicVars["MoDmgPerDraw"].BaseValue;
            // 不 guard dmg > 0 —— extraDraws=0 时仍要让力量加成生效（framework 内部加 Str）
            if (play.Target != null)
            {
                await DamageCmd.Attack(dmg).FromCard(this).Targeting(play.Target).Execute(ctx);
                CombatCounters.StruckByMortisThisTurn[play.Target]++;
            }
            await CombatCounters.BumpMortisCard(ctx, Owner);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            int block = extraDraws * (int)DynamicVars["MuBlockPerDraw"].BaseValue;
            // 不 guard block > 0 —— framework 对 ValueProp.Move 自动加 Dex
            await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, play);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("争夺身体",
            "本回合每额外抽过一张牌，{MuSec}{MuOpen}小睦{MuClose}获得{MuBlockPerDraw}点[gold]格挡[/gold]（{MuActual:diff()}格挡）{MuSecEnd}；{MoSec}{MoOpen}小墨{MoClose}造成{MoDmgPerDraw}点伤害（{MoActual:diff()}伤害）{MoSecEnd}。\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：[gold]进入小墨[/gold]{MuSecEnd}；{MoSec}{MoOpen}小墨{MoClose}：[gold]进入小睦[/gold]{MoSecEnd}。"),
        _ => new CardLoc("Fight for Body",
            "Per extra draw this turn, {MuSec}{MuOpen}Mu{MuClose} gains {MuBlockPerDraw} [gold]Block[/gold] (total {MuActual:diff()} Block){MuSecEnd}; {MoSec}{MoOpen}Mo{MoClose} deals {MoDmgPerDraw} damage (total {MoActual:diff()} damage){MoSecEnd}.\n" +
            "{MuSec}{MuOpen}Mu{MuClose}: [gold]Enter Mo[/gold]{MuSecEnd}; {MoSec}{MoOpen}Mo{MoClose}: [gold]Enter Mu[/gold]{MoSecEnd}."),
    };
}
