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
/// 逃离：0 费白色技能。本回合获得 3/5 点敏捷，[gold]进入小睦[/gold]。
/// 单效果卡（不分形态分支）。Temp Dex 走 DexterityPower + TempDexterityPower 组合（gotcha #9）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Escape : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/escape.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DynamicVar("TempDex", 3m),   // 3 → 5 升级
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<DexterityPower>();
            foreach (var t in FormTooltips.EnterMu()) yield return t;
        }
    }

    public Escape() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["TempDex"].UpgradeValueBy(2);   // 3 → 5
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        var dex = DynamicVars["TempDex"].BaseValue;
        await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner.Creature, dex, Owner.Creature, this, false);
        await Sts2Compat.PowerApply<TempDexterityPower>(ctx, Owner.Creature, dex, Owner.Creature, this, true);

        // 记 counter 用"打出时"的形态（EnterMutsumi 前），匹配 SelfIsolate / TearMaskGold 习惯
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);

        // EnterMutsumi 内部自带 "already Mu → no-op" 守卫（Forms.cs 的 wasMortisForm 判断），
        // 所以从 Mu 形态打也安全：dex 拿到，不触发多余的人格切换计数。
        await Forms.EnterMutsumi(Owner, this, ctx);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("逃离",
            "本回合获得{TempDex:diff()}点[gold]敏捷[/gold]。[gold]进入小睦[/gold]。"),
        _ => new CardLoc("Escape",
            "Gain {TempDex:diff()} [gold]Dexterity[/gold] this turn. [gold]Enter Mu[/gold]."),
    };
}
