using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 电话（稀有）：
///   每当进入小墨时，本回合获得 2 点力量（Str + TempStr 组合）。
///   每当进入小睦时，本回合获得 2 点敏捷（Dex + TempDex 组合）。
/// 监听 AfterPowerAmountChanged: MutsumiFormPower / MortisFormPower 在 owner 自己身上 0→正。
/// Forms.EnterMortis 先 PowerCmd.Remove&lt;MutsumiFormPower&gt;（IL-verified：走
/// `&lt;Remove&gt;d__8.MoveNext` → RemoveInternal + AfterRemoved，不触发本 hook）再
/// Sts2Compat.PowerApply&lt;MortisFormPower&gt;（走 PowerCmd.Apply.MoveNext，触发本 hook，
/// 第 4 参 amount 是 delta 增量，0→1 时 amount==1, power.Amount==1）。
/// 所以判 0→正 的写法 = `amount &gt; 0 &amp;&amp; power.Amount == amount`（即 oldAmount == 0）。
/// "本回合获得" 用 Str + TempStr 组合（参考 Acting.cs），回合结束等量扣回。
/// </summary>
[Pool(typeof(MzmCharRelicPool))]
public class SlipperPhoneRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override string PackedIconPath           => "res://MzmChar/relics/slipper_phone.png";
    protected override string BigIconPath           => "res://MzmChar/relics/slipper_phone.png";
    protected override string PackedIconOutlinePath => "res://MzmChar/relics/slipper_phone.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
            foreach (var t in FormTooltips.BothEnter()) yield return t;
        }
    }

    // stable: AfterPowerAmountChanged(PowerModel, decimal, Creature, CardModel)
    // beta:   AfterPowerAmountChanged(PlayerChoiceContext, PowerModel, decimal, Creature?, CardModel?)
#if BETA
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
    public override async Task AfterPowerAmountChanged(
        PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
    {
        if (Owner == null) return;
        if (power.Owner != Owner.Creature) return;
        if (amount <= 0) return;                  // 只关心 power 被"应用/增加"
        if (power.Amount != amount) return;       // 等价 oldAmount == 0（新值 == 本次 delta）
        var c = Owner.Creature;
#if !BETA
        PlayerChoiceContext? ctx = null;          // stable 无 ctx 参 → 局部 null 传 Sts2Compat
#endif

        if (power is MortisFormPower)
        {
            Flash();
            await Sts2Compat.PowerApply<StrengthPower>(ctx, c, 2, c, null, false);
            await Sts2Compat.PowerApply<TempStrengthPower>(ctx, c, 2, c, null, true);
        }
        else if (power is MutsumiFormPower)
        {
            Flash();
            await Sts2Compat.PowerApply<DexterityPower>(ctx, c, 2, c, null, false);
            await Sts2Compat.PowerApply<TempDexterityPower>(ctx, c, 2, c, null, true);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new RelicLoc(
            Title:       "电话",
            Description: "每当你[gold]进入小墨[/gold]时，本回合获得2点[gold]力量[/gold]；每当你[gold]进入小睦[/gold]时，本回合获得2点[gold]敏捷[/gold]。",
            Flavor:      "实际上是芭蕾舞鞋。"),
        _ => new RelicLoc(
            Title:       "Phone",
            Description: "Whenever you [gold]Enter Mo[/gold], gain 2 [gold]Strength[/gold] this turn. Whenever you [gold]Enter Mu[/gold], gain 2 [gold]Dexterity[/gold] this turn.",
            Flavor:      "Actually a ballet shoe."),
    };
}
