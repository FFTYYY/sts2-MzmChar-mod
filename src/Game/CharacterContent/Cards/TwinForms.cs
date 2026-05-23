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

namespace MzmChar.Game;

/// <summary>双生形态：3 费能力（金色）。回合开始触发（见 TwinFormsPower）。升级 power 数值翻倍但费用不变。
/// 卡上数值用 DynamicVar 走 :diff() 染色 → 升级版数字自动绿色；power 内部由 IsUpgradedVersion field 驱动同样数值。</summary>
[Pool(typeof(MzmCharCardPool))]
public class TwinForms : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/twin_forms.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DynamicVar("MuDmg", 15m),       // 升级 +10 → 25
        new DynamicVar("MoDebuff", 2m),     // 升级 +2 → 4 (vuln 和 weak 共用)
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            // 不挂 TwinFormsPower 自身 hover tip：派生 Damage/Debuff (Amount × 常量) 在 canonical 没法
            // 正确显示升级值。卡描述里 {MuDmg:diff()} / {MoDebuff:diff()} 已经体现升级数值
        }
    }

    public TwinForms() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["MuDmg"].UpgradeValueBy(10);     // 15 → 25
        DynamicVars["MoDebuff"].UpgradeValueBy(2);   // 2 → 4
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        // IsInstanced=true → 每次 apply 独立 instance；power.AfterApplied 自己从 cardSource.IsUpgraded
        // 读升级状态，无需 OnPlay 手动 set。
        await Sts2Compat.PowerApply<TwinFormsPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("双生形态",
            "回合开始时：{MuSec}{MuOpen}小睦{MuClose}对随机敌人造成{MuDmg:diff()}点伤害，[gold]进入小墨[/gold]{MuSecEnd}；{MoSec}{MoOpen}小墨{MoClose}对随机敌人施加{MoDebuff:diff()}层[gold]虚弱[/gold]和{MoDebuff:diff()}层[gold]易伤[/gold]，[gold]进入小睦[/gold]{MoSecEnd}。"),
        _ => new CardLoc("Twin Forms",
            "Start of turn: {MuSec}{MuOpen}Mu{MuClose} deal {MuDmg:diff()} damage to a random enemy, [gold]Enter Mo[/gold]{MuSecEnd}; {MoSec}{MoOpen}Mo{MoClose} apply {MoDebuff:diff()} [gold]Weak[/gold] and {MoDebuff:diff()} [gold]Vulnerable[/gold] to a random enemy, [gold]Enter Mu[/gold]{MoSecEnd}."),
    };
}
