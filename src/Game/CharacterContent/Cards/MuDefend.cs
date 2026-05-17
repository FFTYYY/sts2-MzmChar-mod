using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 防御：1 费基础技能。双形态。
///   小睦：6/9 格挡
///   小墨：本回合获得 3/5 格挡 + 1/2 敏捷
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MuDefend : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/defend.png";

    // 初始卡，不应被印牌/变化牌随机产生
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar(6, ValueProp.Move),                    // Mu block (default name "Block")
        new BlockVar("MoBlock", 3m, ValueProp.Move),         // Mo block (custom name)
        new DynamicVar("TempDex", 1m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardTag> _tags = new() { CardTag.Defend };
    protected override HashSet<CardTag> CanonicalTags => _tags;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<DexterityPower>(); }
    }

    public MuDefend() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);               // Mu: 6 → 9
        DynamicVars["MoBlock"].UpgradeValueBy(2);          // Mo: 3 → 5
        DynamicVars["TempDex"].UpgradeValueBy(1);          // 1 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            await PlayCast();
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars["MoBlock"].BaseValue, ValueProp.Move, play, false);
            var dex = DynamicVars["TempDex"].BaseValue;
            await PowerCmd.Apply<DexterityPower>(Owner.Creature, dex, Owner.Creature, this, false);
            await PowerCmd.Apply<TempDexterityPower>(Owner.Creature, dex, Owner.Creature, this, true);
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("防御",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：获得{MoBlock:diff()}点[gold]格挡[/gold]。本回合获得{TempDex:diff()}点[gold]敏捷[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Defend",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Gain {MoBlock:diff()} [gold]Block[/gold]; this turn gain {TempDex:diff()} [gold]Dexterity[/gold].{MoSecEnd}"),
    };
}
