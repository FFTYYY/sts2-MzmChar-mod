using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 人格解体：1/0 费金色能力。每切换一次人格，随机给牌组中 1 张牌添加「虚无」。
///
/// 类名 `MuDisintegration`：原版有同名卡牌 Disintegration，类名重名时 BaseLib 的 MZMCHAR- 前缀不管用（同 MuMask 前例），加 Mu 前缀。
/// 存档里的旧 ID 由 LegacyModelIdPatch 映射到新 ID。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MuDisintegration : MzmCharBaseCard
{
    public override string PortraitPath => Art("disintegration");

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<MuDisintegrationPower>(); }
    }

    public MuDisintegration() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<MuDisintegrationPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("人格解体",
            "你每切换一次人格，随机给牌组中一张牌添加[gold]虚无[/gold]。"),
        _ => new CardLoc("Disintegration",
            "Whenever you switch personas, add [gold]Ethereal[/gold] to a random card in your deck."),
    };
}
