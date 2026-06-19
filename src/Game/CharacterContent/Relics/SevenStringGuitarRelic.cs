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
/// 粉色电吉他（罕见）：每当你获得力量或敏捷时，同时获得 1 点活力。
/// Hook 走 AfterPowerAmountChanged：第 4 参 amount 是本次 delta 增量（IL-verified），
/// 直接 amount &gt; 0 即可判断"是 gain 不是 lose"。
/// 注意 TempStrengthPower / TempDexterityPower 是独立 power 类型，不会触发（"获得力量"语义只算永久部分）。
/// </summary>
[Pool(typeof(MzmCharRelicPool))]
public class SevenStringGuitarRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath           => "res://MzmChar/relics/seven_string_guitar.png";
    protected override string BigIconPath           => "res://MzmChar/relics/seven_string_guitar.png";
    protected override string PackedIconOutlinePath => "res://MzmChar/relics/seven_string_guitar.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
            yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner == null) return;
        if (power.Owner != Owner.Creature) return;
        if (power is not StrengthPower && power is not DexterityPower) return;
        if (amount <= 0) return;  // 只关心 gain

        Flash();
        await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature, 1, Owner.Creature, null, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new RelicLoc(
            Title:       "吉他",
            Description: "每当你获得[gold]力量[/gold]或[gold]敏捷[/gold]时，同时获得1点[gold]活力[/gold]。",
            Flavor:      "我为数不多的朋友..."),
        _ => new RelicLoc(
            Title:       "Guitar",
            Description: "Whenever you gain [gold]Strength[/gold] or [gold]Dexterity[/gold], also gain 1 [gold]Vigor[/gold].",
            Flavor:      "One of my few friends..."),
    };
}
