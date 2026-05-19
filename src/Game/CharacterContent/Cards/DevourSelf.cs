using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 吞噬自己：1 费技能（蓝色），稀有。
///   小睦：获得 当前敏捷*1.5（升级 *2）的活力。进入小墨。
///   小墨：造成 当前敏捷*2（升级 *3）的伤害。进入小睦。
///
/// 倍数用 DynamicVar 走 :diff() 染色 → 升级版倍数自动绿色。
/// 显示用 LambdaVar 实时按 player 的 Dex stack 算出来的预测值。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class DevourSelf : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/devour_self.png";

    private readonly List<DynamicVar> _vars;

    // Mu 给自己 vigor + 切形态 → 不需要选目标
    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    public DevourSelf() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        _vars = new List<DynamicVar>
        {
            new DynamicVar("MuMult", 2m),     // 升级 +1 → 3
            new DynamicVar("MoMult", 3m),     // 升级 +1 → 4
            // Mu 实算：vigor = floor(dex * mult)
            new LambdaVar("MuVigor", card =>
            {
                int dex = GetCurrentDex(card);
                decimal mult = card.DynamicVars["MuMult"].BaseValue;
                return (int)Math.Floor(dex * mult);
            }),
            // Mo 实算：damage = dex * mult，需要走 Hook.ModifyDamage 应用 Strength/Vulnerable
            new LambdaVar("MoDmg", card =>
            {
                int dex = GetCurrentDex(card);
                decimal mult = card.DynamicVars["MoMult"].BaseValue;
                return (int)(dex * mult);
            }, LambdaVar.ModifierKind.Damage),
        };
    }

    private static int GetCurrentDex(CardModel card)
    {
        if (card.Owner?.Creature == null) return 0;
        var dexPower = card.Owner.Creature.GetPower<DexterityPower>();
        return dexPower != null ? (int)dexPower.Amount : 0;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<DexterityPower>();
            yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MuMult"].UpgradeValueBy(1m);     // 2 → 3
        DynamicVars["MoMult"].UpgradeValueBy(1m);     // 3 → 4
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int dex = GetCurrentDex(this);

        if (Forms.IsMortisForm(Owner))
        {
            decimal moMult = DynamicVars["MoMult"].BaseValue;
            int dmg = (int)(dex * moMult);
            // 不 guard dmg > 0 —— dex=0 时仍要让力量加成生效（framework 内部加 Str）
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
            decimal mult = DynamicVars["MuMult"].BaseValue;
            int vigor = (int)Math.Floor(dex * mult);
            if (vigor > 0)
                await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature, vigor, Owner.Creature, this, false);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("吞噬自己",
            "{MuSec}{MuOpen}小睦{MuClose}：获得当前[gold]敏捷[/gold]{MuMult:diff()}倍的[gold]活力[/gold]（{MuVigor}活力）。[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成当前[gold]敏捷[/gold]{MoMult:diff()}倍的伤害（{MoDmg:diff()}伤害）。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Devour Self",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain [gold]Vigor[/gold] equal to {MuMult:diff()}× your current [gold]Dexterity[/gold] ({MuVigor}). [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal damage equal to {MoMult:diff()}× your current [gold]Dexterity[/gold] ({MoDmg:diff()}). [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
