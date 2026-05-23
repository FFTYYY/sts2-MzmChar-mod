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
/// 面具：1 费白色技能。获得 1 演艺热情。双形态切换。
/// （原拟名"撕下的面具"，与金卡"撕下面具"重复，重命名为「面具」）
///
/// 类名 `MuMask`（不是 `Mask`）—— vanilla `ModelIdSerializationCache.Init` sorter
/// 在 Mask 时 warn "Two AbstractModels MzmChar.Game.Mask share an ID"，根因未追到，
/// 加 Mu 前缀作 workaround（同 MuStrike / MuDefend / MuMonologue / MuBurn 等命名习惯）。
/// loc 名「面具」/「Mask」不变。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MuMask : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mask.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
        }
    }

    public MuMask() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); /* 1 → 0 费 */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Sts2Compat.PowerApply<PerformancePassionPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner))
        {
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("面具",
            "获得1点[gold]演艺热情[/gold]。\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Mask",
            "Gain 1 [gold]Performance Passion[/gold].\n" +
            "{MuSec}{MuOpen}Mu{MuClose}: [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
