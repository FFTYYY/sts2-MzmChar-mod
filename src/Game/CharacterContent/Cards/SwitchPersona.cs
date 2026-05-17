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
/// 人格转换：1 费技能（升级 0 费）。
/// 切换形态 + 本回合 +2 敏捷 +2 力量（用 STS1 套路：apply 持久 +N，再挂 LoseXxxAtTurnEndPower 在回合末减回去）
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class SwitchPersona : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/switch_persona.png";

    // 初始卡（人格转换），不应被印牌/变化牌随机产生
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<DexterityPower>();
            yield return HoverTipFactory.FromPower<StrengthPower>();
        }
    }

    public SwitchPersona() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 1 → 0 费
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // 本回合 +2 dex +2 strength
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, 2, Owner.Creature, this, false);
        await PowerCmd.Apply<TempDexterityPower>(Owner.Creature, 2, Owner.Creature, this, true);
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, 2, Owner.Creature, this, false);
        await PowerCmd.Apply<TempStrengthPower>(Owner.Creature, 2, Owner.Creature, this, true);

        if (Forms.IsMortisForm(Owner))
        {
            await CombatCounters.BumpMortisCard(ctx, Owner);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("人格转换",
            "本回合获得2点[gold]敏捷[/gold]和2点[gold]力量[/gold]。\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：[gold]进入小墨[/gold]。{MuSecEnd}\n{MoSec}{MoOpen}小墨{MoClose}：[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Switch Persona",
            "This turn gain 2 [gold]Dexterity[/gold] and 2 [gold]Strength[/gold].\n" +
            "{MuSec}{MuOpen}Mu{MuClose}: [gold]Enter Mo[/gold].{MuSecEnd}\n{MoSec}{MoOpen}Mo{MoClose}: [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
