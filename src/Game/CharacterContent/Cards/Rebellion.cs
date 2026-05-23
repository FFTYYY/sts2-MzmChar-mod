using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MzmChar.Game;

/// <summary>
/// 叛逆：1 费金色技能（消耗）。（原名「从没觉得玩乐队开心过」）
/// 把你所有的「打击」（CardTag.Strike）变化为**随机无色牌**。升级：变成升级过的随机无色牌。
///
/// Transform pattern 参考 EntropyPower.AfterPlayerTurnStart：用 CardCmd.Transform(original, replacement, style)。
/// replacement 从 ColorlessCardPool.AllCards 里随机抽。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Rebellion : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/rebellion.png";

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    public Rebellion() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { /* upgrade 改的是给 colorless replacement 调 Upgrade */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        // 收集所有 piles 里 tag 含 Strike 的卡
        var strikes = new List<CardModel>();
        foreach (var pile in new[] { PileType.Hand, PileType.Discard, PileType.Draw })
        {
            var p = pile.GetPile(Owner);
            if (p == null) continue;
            foreach (var c in p.Cards.ToList())
                if (c.Tags != null && c.Tags.Contains(CardTag.Strike))
                    strikes.Add(c);
        }

        var colorlessPool = ModelDb.CardPool<ColorlessCardPool>();
        var allColorless = colorlessPool?.AllCards;
        var rng = Owner.RunState?.Rng?.CombatCardSelection;

        // 按单机 / 联机模式过滤掉对方模式专属卡。等价于 vanilla CardFactory.FilterForPlayerCount
        // (internal 不可见，inline 同样逻辑)：
        //   单机 (Players.Count <= 1) → 排除 MultiplayerOnly
        //   联机 (>1)                → 排除 SingleplayerOnly
        // 同时排除 CanBeGeneratedInCombat=false 的卡（如其他 mod 的测试卡 / 初始卡 / 先古卡）。
        // 这跟 InnerNoise / NeverHappyInBand 同款 filter。
        bool isMultiplayer = Owner.RunState != null && Owner.RunState.Players.Count > 1;
        var allowed = allColorless?.Where(c =>
            c.CanBeGeneratedInCombat
            && (isMultiplayer
                ? c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly
                : c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly)).ToList();

        if (rng != null && allowed != null && allowed.Count > 0)
        {
            foreach (var original in strikes)
            {
                var template = rng.NextItem(allowed);
                if (template == null) continue;
                // 拿一个 fresh instance（canonical 不能直接进 pile —— 见坑 #4）
                var replacement = Owner.Creature.CombatState!.CreateCard(template, Owner);
                if (IsUpgraded) CardCmd.Upgrade(replacement, CardPreviewStyle.None);
                await CardCmd.Transform(original, replacement, CardPreviewStyle.MessyLayout);
            }
        }

    }

    // 注：
    //  1) 不要手写「消耗」—— Exhaust keyword 框架自动渲染
    //  2) 升级文本切换走 SmartFormat 条件 {IfUpgraded:show:upText|baseText} ——
    //     ILocalizationProvider 只在注册时调一次（canonical IsUpgraded=false），所以
    //     C# 端 IsUpgraded 三元式不能切升级文本。MzmCharBaseCard.AddExtraArgsToDescription
    //     已经把 IfUpgraded 注入到 description 变量字典
    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("叛逆",
            "把你所有的[gold]打击[/gold]变化为{IfUpgraded:show:升级过的|}随机[gold]无色牌[/gold]。"),
        _ => new CardLoc("Rebellion",
            "Transform all your [gold]Strike[/gold] cards into {IfUpgraded:show:upgraded |}random [gold]Colorless[/gold] cards."),
    };
}
