using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 移动：1 费基础技能。
///   小睦：获得 3/5 格挡，从弃牌堆选 1 张牌加入手牌
///   小墨：抽 2/3 张牌
///
/// Mu 分支 IL-verified 1:1 抄 vanilla `Hologram.OnPlay`（state machine d__7）：
///   1. CreatureCmd.GainBlock(creature, blockAmount, cardPlay, skipVfx=0)
///   2. prefs = new CardSelectorPrefs(SelectionScreenPrompt, selectCount=1)
///   3. discard = PileType.Discard.GetPile(owner)
///   4. selected = await CardSelectCmd.FromSimpleGrid(ctx, discard.Cards, owner, prefs)
///   5. picked = selected.FirstOrDefault()
///   6. if (picked != null) CardPileCmd.Add(picked, PileType.Hand=2, CardPilePosition.Bottom=1, source=null, skipVisuals=false)
///
/// 注意（gotcha #xx）：
///   - 不要传 `this` 作为 source 给 CardPileCmd.Add —— vanilla 传 null
///   - position 必须是 Bottom（vanilla 一致）
///   - **不要**加 `if (discard.Cards.Count > 0)` 检查 —— vanilla 也不加，FromSimpleGrid 处理空列表
///   - 过滤掉自己（this）防 Move 卡刚进弃牌堆又被选回手 → 无限循环 risk
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MoveCard : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/move.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar(3, ValueProp.Move),
        new CardsVar(2),  // Mo 用
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    public MoveCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);                // 3 → 5
        DynamicVars.Cards.UpgradeValueBy(1);                // 2 → 3
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner, false);
            return;
        }

        // === Mu 分支：1:1 抄 Hologram pattern ===
        await CreatureCmd.GainBlock(Owner.Creature,
            DynamicVars.Block.BaseValue, ValueProp.Move, play, false);

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, selectCount: 1);
        var discard = PileType.Discard.GetPile(Owner);

        // 过滤掉自己 —— Move 在 OnPlay 进行时可能已在 discard 里，不能让玩家选回自己
        var candidates = discard.Cards.Where(c => c != this).ToList();

        var selected = await CardSelectCmd.FromSimpleGrid(ctx, candidates, Owner, prefs);
        var picked = selected.FirstOrDefault();
        if (picked != null)
            await CardPileCmd.Add(picked, PileType.Hand, CardPilePosition.Bottom, null, false);

    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("移动",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]。从[gold]弃牌堆[/gold]选一张牌加入手牌。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：抽{Cards:diff()}张牌。{MoSecEnd}",
            ("selectionScreenPrompt", "选一张牌加入手牌")),
        _ => new CardLoc("Move",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold]; choose a card from your [gold]discard pile[/gold] to add to your hand.{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Draw {Cards:diff()}.{MoSecEnd}",
            ("selectionScreenPrompt", "Choose a card to add to your hand")),
    };
}
