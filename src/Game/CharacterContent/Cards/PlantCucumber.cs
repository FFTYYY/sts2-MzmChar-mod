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

/// <summary>种植黄瓜：2 费蓝色能力。回合开始按形态触发：Mu +6/8 格挡 / Mo +4/6 活力（见 PlantCucumberPower）。
/// 卡上数值用 DynamicVar 走 :diff() 染色 → 升级版数字自动绿色；power 内部由 IsUpgradedVersion field 驱动同样数值。</summary>
[Pool(typeof(MzmCharCardPool))]
public class PlantCucumber : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/cucumber.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DynamicVar("MuBlock", 6m),    // 升级 +2 → 8
        new DynamicVar("MoVigor", 4m),    // 升级 +2 → 6
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // 不挂 PlantCucumberPower 自身 hover tip：派生 MuGain/MoGain (Amount × 常量) canonical 没法对齐升级值
            yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }

    public PlantCucumber() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["MuBlock"].UpgradeValueBy(2);    // 6 → 8
        DynamicVars["MoVigor"].UpgradeValueBy(2);    // 4 → 6
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        // IsInstanced=true → 每次 apply 独立 instance；power.AfterApplied 自己从 cardSource.IsUpgraded
        // 读升级状态，无需 OnPlay 手动 set。
        await Sts2Compat.PowerApply<PlantCucumberPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("种植黄瓜",
            "回合开始时：{MuSec}{MuOpen}小睦{MuClose}获得{MuBlock:diff()}点[gold]格挡[/gold]{MuSecEnd}；{MoSec}{MoOpen}小墨{MoClose}获得{MoVigor:diff()}点[gold]活力[/gold]{MoSecEnd}。"),
        _ => new CardLoc("Plant Cucumbers",
            "Start of turn: {MuSec}{MuOpen}Mu{MuClose} gain {MuBlock:diff()} [gold]Block[/gold]{MuSecEnd}; {MoSec}{MoOpen}Mo{MoClose} gain {MoVigor:diff()} [gold]Vigor[/gold]{MoSecEnd}."),
    };
}
