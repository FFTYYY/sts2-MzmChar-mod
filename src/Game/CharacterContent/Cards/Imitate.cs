using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 模仿：2/1 费蓝色攻击。
///   小睦：获得 12 点格挡，给目标 2 层虚弱。
///   小墨：如果目标的意图是攻击，造成其意图攻击值的伤害，否则造成 1 点伤害。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Imitate : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/imitate.png";

    // canonical-safe LambdaVar：卡组大全里 base.UpdateCardPreview 访问 card.Owner 抛
    // CanonicalModelException → BaseValue 留在 base(name, 0) 的 0 → 显示 0。
    // 本地子类：ctor 把三个值默认成 1；try/catch 兜住 canonical 异常；最后跟 1 取 max
    // 保证显示不低于 1（lambda 在「无攻击意图」分支返回 1，跟这个 floor 语义一致）
    private class MoDmgVar : LambdaVar
    {
        public MoDmgVar(string name, System.Func<CardModel, Creature?, decimal> calc)
            : base(name, calc, ModifierKind.Damage)
        {
            BaseValue = 1m;
            EnchantedValue = 1m;
            PreviewValue = 1m;
        }
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            try { base.UpdateCardPreview(card, previewMode, target, runGlobalHooks); }
            catch { /* canonical card → 保持 ctor 默认的 1 */ }
            if (BaseValue < 1m) BaseValue = 1m;
            if (PreviewValue < 1m) PreviewValue = 1m;
            if (EnchantedValue < 1m) EnchantedValue = 1m;
        }
    }

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar("MuBlock", 12m, ValueProp.Move),
        new PowerVar<WeakPower>(2),
        // Mo 阶段 1：怪打到出牌玩家头上的单次伤害（吃怪 strength/weak/back attack + 我方 vulnerable）
        // ModifierKind.Damage 在外层补阶段 2：再跑一次 Hook.ModifyDamage(dealer=我, target=怪)
        // 套上我方 strength/vigor + 怪的 vulnerable —— 跟 OnPlay 实际造成的伤害一致
        new MoDmgVar("MoDmg", (card, t) =>
            ComputeMimickedIntentDamage(card.Owner?.Creature, t, card)),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    // AttackIntent.GetTotalDamage 内部走 LocalContext.GetMe，每个 client 算「打到本机玩家头上」
    // 的伤害 → 联机两 client 算出不同 dmg → desync。手算 Hook.ModifyDamage 显式传出牌玩家作
    // target，全 client 一致。详 notes/baselib_and_game_apis.md
    private static int ComputeMimickedIntentDamage(Creature? ourCreature, Creature? target, CardModel sourceCard)
    {
        if (ourCreature == null || target?.IsMonster != true || target.Monster == null) return 1;
        var attackIntent = target.Monster.NextMove?.Intents?.OfType<AttackIntent>().FirstOrDefault();
        if (attackIntent == null) return 1;
        var combatState = target.CombatState;
        var runState = combatState?.RunState;
        if (combatState == null || runState == null) return 1;

        if (attackIntent.DamageCalc == null) return 1;
        decimal rawSingle = attackIntent.DamageCalc();
        // v0.108 加了 CardPlay 参；helper 同时被 display lambda + OnPlay 调用，统一传 null
        // （vanilla Thrash.OnPlay 也是传 null，IL-verified，见 report_57 §4.3）
        decimal modifiedSingle = Sts2Compat.ModifyDamageCompat(
            runState, combatState,
            target: ourCreature, dealer: target,
            damage: rawSingle, props: ValueProp.Move, cardSource: sourceCard, cardPlay: null,
            ModifyDamageHookType.All, CardPreviewMode.None, out _);

        // SingleAttackIntent.GetTotalDamage 不乘 Repeats；MultiAttackIntent 才乘（IL-verified）
        int multiplier = attackIntent is MultiAttackIntent ? System.Math.Max(1, attackIntent.Repeats) : 1;
        int total = (int)(modifiedSingle * multiplier);
        return System.Math.Max(1, total);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<WeakPower>(); }
    }

    public Imitate() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);  // 2 → 1
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            // 阶段 1：手算「怪打到我头上的伤害」。DamageCmd.Attack 内部跑阶段 2 套我方 strength/vigor + 怪的 vulnerable
            int dmg = ComputeMimickedIntentDamage(Owner.Creature, play.Target, this);
            if (play.Target != null)
                await DamageCmd.Attack(dmg).FromCardCompat(this, play).Targeting(play.Target).Execute(ctx);
        }
        else
        {
            await PlayCast();
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars["MuBlock"].BaseValue, ValueProp.Move, play, false);
            if (play.Target != null)
                await Sts2Compat.PowerApply<WeakPower>(ctx, play.Target,
                    DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("模仿",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{MuBlock:diff()}点[gold]格挡[/gold]。施加{WeakPower:diff()}层[gold]虚弱[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{MoDmg:diff()}点伤害。若目标有攻击意图，则造成等同于目标攻击意图的伤害。{MoSecEnd}"),
        _ => new CardLoc("Imitate",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {MuBlock:diff()} [gold]Block[/gold]; apply {WeakPower:diff()} [gold]Weak[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {MoDmg:diff()} damage. If the target has an attack intent, deal damage equal to the target's attack intent.{MoSecEnd}"),
    };
}
