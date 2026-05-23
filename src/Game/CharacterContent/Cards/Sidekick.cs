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
/// 小跟班：1 费蓝色攻击。
///   小墨：造成 8/12 点伤害
///   小睦：为至多 2/3 张手牌添加「虚无」
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Sidekick : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/sidekick.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(8, ValueProp.Move),
        new DynamicVar("MuCount", 2m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    public Sidekick() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);     // 8 → 12
        DynamicVars["MuCount"].UpgradeValueBy(1); // 2 → 3
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).Execute(ctx);
            }
        }
        else
        {
            // 参考 vanilla SCULPTING_STRIKE 的 IL：CardSelectCmd.FromHand + CardCmd.ApplyKeyword
            // 用 Cmd 而不是裸 AddKeyword 以保证多人同步 / 动画 / undo
            int count = (int)DynamicVars["MuCount"].BaseValue;
            var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, count);
            var selected = await CardSelectCmd.FromHand(
                ctx, Owner, prefs,
                c => c != this && !c.Keywords.Contains(CardKeyword.Ethereal),
                this);
            foreach (var c in selected)
                CardCmd.ApplyKeyword(c, new[] { CardKeyword.Ethereal });
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("小跟班",
            "{MuSec}{MuOpen}小睦{MuClose}：为至多{MuCount:diff()}张手牌添加[gold]虚无[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。{MoSecEnd}",
            ("selectionScreenPrompt", "选最多{MuCount}张手牌添加虚无")),
        _ => new CardLoc("Sidekick",
            "{MuSec}{MuOpen}Mu{MuClose}: Add [gold]Ethereal[/gold] to up to {MuCount:diff()} hand cards.{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage.{MoSecEnd}",
            ("selectionScreenPrompt", "Choose up to {MuCount} cards to add Ethereal")),
    };
}
