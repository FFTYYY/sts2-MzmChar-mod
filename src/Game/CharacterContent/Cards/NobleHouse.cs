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
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 名门：1 费蓝色能力。回合开始时，每有 1 层演艺热情，获得 3 点格挡。升级：加 Innate（数值/费用不变）。
/// 多张本卡可叠加：power Amount 累加（3+3=6 per passion）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class NobleHouse : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/noble_house.png";

    // 用 plain DynamicVar 不是 BlockVar —— 能力卡数值固定字面值，不应吃敏捷加成
    // （BlockVar 会让 :diff() PreviewValue 跑 Hook.ModifyBlock 叠 dex/frail 等 modifier）
    private readonly List<DynamicVar> _vars = new()
    {
        new DynamicVar("Block", 3m),    // 固定 3，升级只降费，不变数值
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<NobleHousePower>();
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
        }
    }

    public NobleHouse() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);   // 升级加固有；费用 / 数值不变
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<NobleHousePower>(ctx, Owner.Creature, DynamicVars["Block"].BaseValue, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("名门",
            "回合开始时，你每有1点[gold]演艺热情[/gold]，就获得{Block:diff()}点[gold]格挡[/gold]。"),
        _ => new CardLoc("Noble House",
            "At turn start, gain {Block:diff()} [gold]Block[/gold] for each stack of [gold]Performance Passion[/gold]."),
    };
}
