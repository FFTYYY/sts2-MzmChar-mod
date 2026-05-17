using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 里人格：0 费技能。Retain。
/// 进入小墨 + 应用「本回合结束时格挡不消失」。升级后额外获得 5 格挡。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class BackPersona : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/back_persona.png";

    // 升级时给 5 格挡（Mu 形态语义不在意，纯粹是上升级后的加成）
    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar(0, ValueProp.Move),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Retain };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    // 初始牌：禁止被「发现」类效果抽到（Token rarity 不被 FilterForCombat 默认排除）
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.EnterMo()) yield return t;
            yield return HoverTipFactory.FromPower<BlockRetainTurnPower>();
        }
    }

    public BackPersona() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5);               // 0 → 5 格挡
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
            await CombatCounters.BumpMortisCard(ctx, Owner);
        else
            await CombatCounters.BumpMutsumiCard(ctx, Owner);

        await Forms.EnterMortis(Owner, this, ctx);
        await PowerCmd.Apply<BlockRetainTurnPower>(Owner.Creature, 1, Owner.Creature, this, false);

        var blockAmount = DynamicVars.Block.BaseValue;
        if (blockAmount > 0)
            await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, play, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("里人格",
            "[gold]进入小墨[/gold]。下回合开始时，[gold]格挡[/gold]不会消失。{IfUpgraded:show:获得{Block:diff()}点[gold]格挡[/gold]。|}"),
        _ => new CardLoc("Back Persona",
            "Enter [gold]Mo[/gold]. Your [gold]Block[/gold] is not removed at start of next turn. {IfUpgraded:show:Gain {Block:diff()} [gold]Block[/gold].|}"),
    };
}
