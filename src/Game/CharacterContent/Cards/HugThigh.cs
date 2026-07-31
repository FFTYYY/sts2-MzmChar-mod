using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace MzmChar.Game;

/// <summary>
/// 抱住大腿：2 费蓝色能力卡。联机专用。
///   选择一名其他玩家：其失去所有力量 / 敏捷 / 活力（含负数层，对称转移），你获得 2 倍数值。
///   在其弃牌堆（升级后：抽牌堆随机位置）加入一张这张牌的复制品。
///
/// 能力牌打出后自身移出战斗，无归堆问题；给队友的是新生成的复制品（CreateCard from canonical
/// + 补升级），不是原卡 —— 移动原卡进别人牌堆的方案（TheBall 式 GetResultLocationForCardPlay
/// 重定向）实测会额外在自己弃牌堆留一份，太容易出同步 bug，已弃用（git history 有实现）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class HugThigh : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/hug_thigh.png";

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
            yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }

    public HugThigh() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.AnyAlly) { }

    protected override void OnUpgrade()
    {
        // 升级只改复制品去向（弃牌堆 → 抽牌堆随机），loc 文本用 IfUpgraded 切换
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        if (play.Target == null) return;
        // AnyAlly 包含 self（IsValidTarget 只查 Side）—— spec 是"队友"，soft-block 自瞄（同 BullyingYou）
        if (play.Target == Owner.Creature) return;
        var allyPlayer = play.Target.Player;
        if (allyPlayer == null) return;  // 防 pet creature

        // 飞卡特效：仅当能力牌自带动画被成功跳过时才播（否则两套动画打架）。
        // helper 单独成类隔离 JIT —— 老版本缺 NCardFlyVfx 等类型时异常在这里被吞，只丢视觉
        if (HugThighPowerFlyVfxSkipPatch.Active)
        {
            try { HugThighFlyVfx.Play(this, play.Target, allyPlayer); }
            catch (System.Exception e) { ModEntry.Log($"[HugThigh] fly vfx skipped: {e.Message}"); }
        }

        await StealPower<StrengthPower>(ctx, play.Target);
        await StealPower<DexterityPower>(ctx, play.Target);
        await StealPower<VigorPower>(ctx, play.Target);

        // 复制品进队友牌堆（升级状态跟随本卡）
        var combatState = allyPlayer.Creature?.CombatState;
        if (combatState == null) return;
        var copy = combatState.CreateCard(ModelDb.Card<HugThigh>(), allyPlayer);
        if (copy == null) return;
        if (IsUpgraded && copy.IsUpgradable)
            CardCmd.Upgrade(copy, CardPreviewStyle.None);
        var result = await Sts2Compat.AddGeneratedCardToCombat(
            copy,
            IsUpgraded ? PileType.Draw : PileType.Discard,
            allyPlayer,
            IsUpgraded ? CardPilePosition.Random : CardPilePosition.Top);
        CardCmd.PreviewCardPileAdd(result, 1.5f, CardPreviewStyle.HorizontalLayout);
    }

    /// <summary>队友失去全部 T（含负数层，对称转移），自己获得 2 倍。</summary>
    private async Task StealPower<T>(PlayerChoiceContext ctx, Creature target)
        where T : PowerModel, new()
    {
        var pw = target.GetPower<T>();
        if (pw == null || pw.Amount == 0) return;
        var n = pw.Amount;
        await PowerCmd.Remove<T>(target);
        await Sts2Compat.PowerApply<T>(ctx, Owner.Creature, n * 2, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("抱住大腿",
            "令一名其他玩家失去所有[gold]力量[/gold]、[gold]敏捷[/gold]和[gold]活力[/gold]，你获得2倍数值的对应状态。\n在这名玩家的{IfUpgraded:show:抽牌堆|弃牌堆}中加入一张这张牌的复制品。"),
        _ => new CardLoc("Carry Me",
            "Target another ally: they lose ALL [gold]Strength[/gold], [gold]Dexterity[/gold] and [gold]Vigor[/gold]; you gain twice those amounts. Add a copy of this card to their {IfUpgraded:show:draw pile|discard pile}."),
    };
}

/// <summary>
/// 跳过 HugThigh 的能力牌出牌动画（PlayPowerCardFlyVfx：升起+消散），给飞向队友的
/// NCardFlyVfx 让位。按 __instance 类型判定 → 各客户端确定性一致地跳过其中的同步
/// CustomScaledWait，不影响 action 流。
/// 与 HugThighFlyVfx.Available 绑死：飞卡重载不存在（stable v0.107）时不跳过 ——
/// 保留 vanilla 能力牌动画，避免"两头都没动画"。
/// </summary>
[HarmonyPatch]
internal static class HugThighPowerFlyVfxSkipPatch
{
    internal static readonly MethodBase? Target =
        AccessTools.Method(typeof(CardModel), "PlayPowerCardFlyVfx");

    internal static bool Active => Target != null && HugThighFlyVfx.Available;

    private static bool Prepare() => Active;
    private static MethodBase TargetMethod() => Target!;

    [HarmonyPrefix]
    private static bool Prefix(CardModel __instance, ref Task __result)
    {
        if (__instance is not HugThigh) return true;
        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>
/// 「卡牌飞入目标队友」纯本地视觉 —— 全部 vanilla 原生零件，零手算坐标：
///   1. 建卡节点 = 复刻 PlayPowerCardFlyVfx（IL-verified）：NCard.Create → 挂 CombatVfxContainer
///      → GetTargetPosition(Play) 原生解析出牌位（每客户端各自的布局）→ UpdateVisuals(Play, Normal)
///   2. 飞向 creature = 复刻 GiveToAnotherPlayer（IL-verified）：Reparent 到目标 creature 的
///      VfxContainer → NCardFlyVfx.Create(node, target, 接收方 Character.TrailPath) 挂在卡节点下，
///      _Ready 自动播放，卡节点退树时 VFX 自清理
/// 版本兼容：`Create(NCard, Creature, string)` 是 beta 专有重载（stable v0.107.1 只有
/// `(NCard, PileType, bool, string)` 版，CS7036 实证）→ 反射探测 + Invoke，缺失时 Available=false，
/// 上面的 skip patch 一并不装，stable 回退 vanilla 能力牌动画。其余引用的类型/方法两版都有（编译实证）。
/// </summary>
internal static class HugThighFlyVfx
{
    private static readonly MethodInfo? _createToCreature =
        typeof(NCardFlyVfx).GetMethod("Create", new[] { typeof(NCard), typeof(Creature), typeof(string) });

    internal static bool Available => _createToCreature != null;

    internal static void Play(HugThigh card, Creature target, Player allyPlayer)
    {
        if (_createToCreature == null) return;
        var room = NCombatRoom.Instance;
        if (room == null) return;
        var container = room.CombatVfxContainer;
        if (container == null || !GodotObject.IsInstanceValid(container)) return;

        var node = NCard.FindOnTable(card, null);
        if (node == null || !GodotObject.IsInstanceValid(node))
        {
            node = NCard.Create(card, ModelVisibility.Visible);
            if (node == null) return;
            GodotTreeExtensions.AddChildSafely(container, node);
            node.GlobalPosition = PileTypeExtensions.GetTargetPosition(PileType.Play, node);
            node.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);
        }

        var targetContainer = target.GetVfxContainer();
        if (targetContainer == null || !GodotObject.IsInstanceValid(targetContainer)) return;
        node.Reparent(targetContainer);

        var trail = allyPlayer.Character?.TrailPath ?? "";
        var vfx = _createToCreature.Invoke(null, new object[] { node, target, trail }) as Node;
        // vfx 必须挂 creature 的 VfxContainer（IL [534-540]：AddChildSafely(loc1=容器, vfx)），
        // 不能挂卡节点 —— _Ready 里拖尾 NCardTrailVfx 挂到 vfx.GetParent()，父节点若会动
        // 拖尾坐标系就跟着卡漂移
        if (vfx != null)
            GodotTreeExtensions.AddChildSafely(targetContainer, vfx);
    }
}
