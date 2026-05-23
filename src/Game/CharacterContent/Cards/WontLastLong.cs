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
/// 不会长久的：1 费白色能力。
///   小睦：获得 40 活力。每打出 1/2 张牌，本回合失去 1 力量。进入小墨。
///   小墨：获得 3 层无实体。每打出 1/2 张牌，本回合失去 1 敏捷。进入小睦。
///
/// 注意：阈值是 1（基础）/ 2（升级），升级让触发*更慢*（"失力"是 downside）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class WontLastLong : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/wont_last_long.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new PowerVar<VigorPower>(35),
        new PowerVar<IntangiblePower>(3),
        new DynamicVar("MuPer", 1m),
        new DynamicVar("MoPer", 1m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<VigorPower>();
            yield return HoverTipFactory.FromPower<IntangiblePower>();
            yield return HoverTipFactory.FromPower<WontLastLongMuPower>();
            yield return HoverTipFactory.FromPower<WontLastLongMoPower>();
        }
    }

    public WontLastLong() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["MuPer"].UpgradeValueBy(1);  // 1 → 2
        DynamicVars["MoPer"].UpgradeValueBy(1);  // 1 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            await Sts2Compat.PowerApply<IntangiblePower>(ctx, Owner.Creature, 3, Owner.Creature, this, false);
            // WontLastLongMoPower.IsInstanced=true → 每次施加都产生一个独立的 buff 实例（仿 vanilla OrbitPower）
            await Sts2Compat.PowerApply<WontLastLongMoPower>(ctx, Owner.Creature,
                DynamicVars["MoPer"].BaseValue, Owner.Creature, this, false);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature,
                DynamicVars["VigorPower"].BaseValue, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<WontLastLongMuPower>(ctx, Owner.Creature,
                DynamicVars["MuPer"].BaseValue, Owner.Creature, this, false);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("不会长久的",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{VigorPower}点[gold]活力[/gold]。本场战斗中，每打出{MuPer:diff()}张牌，该回合失去1点[gold]力量[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：获得{IntangiblePower}层[gold]无实体[/gold]。本场战斗中，每打出{MoPer:diff()}张牌，该回合失去1点[gold]敏捷[/gold]。{MoSecEnd}\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：[gold]进入小墨[/gold]{MuSecEnd}。{MoSec}{MoOpen}小墨{MoClose}：[gold]进入小睦[/gold]{MoSecEnd}。"),
        _ => new CardLoc("Won't Last Long",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {VigorPower} [gold]Vigor[/gold]. Per {MuPer:diff()} cards played, lose 1 [gold]Strength[/gold] this turn (whole combat). [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Gain {IntangiblePower} [gold]Intangible[/gold]. Per {MoPer:diff()} cards played, lose 1 [gold]Dexterity[/gold] this turn (whole combat). [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
