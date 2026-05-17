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
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 幻想人格：2 费蓝色技能。消耗（无虚无）。升级：去掉消耗 + 改为 1 费。
/// 移除所有「回合结束失去敏捷」debuff。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class PhantomPersona : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/phantom_persona.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<DexterityPower>();
            yield return HoverTipFactory.FromPower<TempDexterityPower>();
        }
    }

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    public PhantomPersona() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);    // 升级去消耗
        EnergyCost.UpgradeBy(-1);              // 升级减 1 费 → 1 费
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        // 合并后 TempDexterityPower 的语义：Amount > 0 = "回合结束失去敏捷"（要被本卡移除）；
        // Amount < 0 = "回合结束恢复敏捷"（Distort 的 Mu 路径用的，不能误删）
        var pw = Owner.Creature.GetPower<TempDexterityPower>();
        if (pw != null && pw.Amount > 0)
            await PowerCmd.Remove<TempDexterityPower>(Owner.Creature);

        if (Forms.IsMortisForm(Owner))
            await CombatCounters.BumpMortisCard(ctx, Owner);
        else
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("幻想人格",
            "移除所有[gold]若叶睦的临时敏捷[/gold]。"),
        _ => new CardLoc("Phantom Persona",
            "Remove all \"lose [gold]Dexterity[/gold] at end of turn\" debuffs."),
    };
}
