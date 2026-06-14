using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MzmChar.Game;

/// <summary>
/// 形态识别 & 切换 helper。
/// 规则：仅有小墨 buff → 小墨；其余（仅小睦 / 两都没 / 两都有）→ 小睦（默认）。
/// 战斗开始保证有一个 buff（<see cref="SecondPersonaRelic"/> 处理）。
/// 切换形态时 swap <c>visuals.tscn</c> 主 Sprite2D 的 texture。
/// </summary>
public static class Forms
{
    private const string MutsumiFramesPath = "res://MzmChar/resources/mu_anime.tres";
    private const string MortisFramesPath  = "res://MzmChar/resources/mo_anime.tres";

    /// <summary>每个形态对应的 AnimatedSprite2D position/scale，从 pack/MzmChar/scenes/visuals_mo.tscn
    /// 的 _VisualsMu / _VisualsMo 节点抄过来（手调对齐过的数值）。</summary>
    private static readonly Godot.Vector2 MutsumiPosition = new(36f, -142f);
    private static readonly Godot.Vector2 MutsumiScale    = new(0.34666667f, 0.34666667f);
    private static readonly Godot.Vector2 MortisPosition  = new(30.692335f, -152.73175f);
    private static readonly Godot.Vector2 MortisScale     = new(0.330693f, 0.33069295f);

    private static SpriteFrames? _mutsumiFrames;
    private static SpriteFrames? _mortisFrames;

    private static SpriteFrames? LoadFrames(string path, ref SpriteFrames? cache)
    {
        if (cache != null) return cache;
        cache = ResourceLoader.Load<SpriteFrames>(path);
        return cache;
    }

    /// <summary>
    /// Mod 启动时调一次，把 mu / mo SpriteFrames（含背后几十 MB 的 atlas PNG）同步预加载进缓存。
    /// 不调的话第一次切到 mo 形态会卡几秒（resource 同步加载阻塞主线程）。
    /// </summary>
    public static void Preload()
    {
        LoadFrames(MutsumiFramesPath, ref _mutsumiFrames);
        LoadFrames(MortisFramesPath, ref _mortisFrames);
    }

    public static bool IsMortisForm(Player p) =>
        p.Creature.HasPower<MortisFormPower>() && !p.Creature.HasPower<MutsumiFormPower>();

    public static bool IsMutsumiForm(Player p) => !IsMortisForm(p);

    /// <summary>切换到小睦：先移除小墨 buff（如有），再加小睦 buff（如无）。算一次切换。</summary>
    public static async Task EnterMutsumi(Player p, CardModel? source, PlayerChoiceContext? ctx = null)
    {
        Diag.Trace($"Forms.EnterMutsumi[player={p.NetId}]: start hp={p.Creature.CurrentHp} src={source?.GetType().Name}");
        // 「坠入深渊」buff：阻止真正切到小睦（其他效果照常进行，但形态不切）
        if (p.Creature.HasPower<FallIntoAbyssPower>())
            { Diag.Trace($"Forms.EnterMutsumi[player={p.NetId}]: skip (FallIntoAbyssPower)"); return; }
        // 「本回合不可切换」buff
        if (p.Creature.HasPower<NoSwitchThisTurnPower>())
            { Diag.Trace($"Forms.EnterMutsumi[player={p.NetId}]: skip (NoSwitchThisTurnPower)"); return; }

        // 是否"真的"在小墨形态 —— 用 IsMortisForm XOR 检查而非 HasPower 裸检查。
        bool wasMortisForm = IsMortisForm(p);

        if (p.Creature.HasPower<MortisFormPower>())
            await PowerCmd.Remove<MortisFormPower>(p.Creature);
        if (!p.Creature.HasPower<MutsumiFormPower>())
            await Sts2Compat.PowerApply<MutsumiFormPower>(ctx, p.Creature, 1, p.Creature, source, false);

        if (wasMortisForm && ctx != null)
        {
            Diag.Trace($"Forms.EnterMutsumi[player={p.NetId}]: BumpPersonaSwitch start");
            await CombatCounters.BumpPersonaSwitch(ctx, p);
            Diag.Trace($"Forms.EnterMutsumi[player={p.NetId}]: OnPersonaSwitched start");
            await OnPersonaSwitched(p, source, ctx);
            Diag.Trace($"Forms.EnterMutsumi[player={p.NetId}]: per-switch done");
        }

        SwapVisualsToCurrentForm(p);
        Diag.Trace($"Forms.EnterMutsumi[player={p.NetId}]: end");
    }

    public static async Task EnterMortis(Player p, CardModel? source, PlayerChoiceContext? ctx = null)
    {
        Diag.Trace($"Forms.EnterMortis[player={p.NetId}]: start hp={p.Creature.CurrentHp} src={source?.GetType().Name}");
        if (p.Creature.HasPower<NoSwitchThisTurnPower>())
            { Diag.Trace($"Forms.EnterMortis[player={p.NetId}]: skip (NoSwitchThisTurnPower)"); return; }

        bool wasMutsumiForm = IsMutsumiForm(p);

        if (p.Creature.HasPower<MutsumiFormPower>())
            await PowerCmd.Remove<MutsumiFormPower>(p.Creature);
        if (!p.Creature.HasPower<MortisFormPower>())
            await Sts2Compat.PowerApply<MortisFormPower>(ctx, p.Creature, 1, p.Creature, source, false);

        if (wasMutsumiForm && ctx != null)
        {
            Diag.Trace($"Forms.EnterMortis[player={p.NetId}]: BumpPersonaSwitch start");
            await CombatCounters.BumpPersonaSwitch(ctx, p);
            Diag.Trace($"Forms.EnterMortis[player={p.NetId}]: OnPersonaSwitched start");
            await OnPersonaSwitched(p, source, ctx);
            Diag.Trace($"Forms.EnterMortis[player={p.NetId}]: per-switch done");
        }

        SwapVisualsToCurrentForm(p);
        Diag.Trace($"Forms.EnterMortis[player={p.NetId}]: end");
    }

    /// <summary>
    /// 切人格触发：检查并执行那些「每次切人格」的 buff 效果。
    /// 添加新 per-switch buff 在这里加 trigger。
    /// </summary>
    private static async Task OnPersonaSwitched(Player p, CardModel? source, PlayerChoiceContext ctx)
    {
        // ComedianPower 是 IsInstanced — 每个实例独立倒计时，要派发到所有 instances
        // .ToList() 必需：OnPersonaSwitch 内部 Apply<EnergyNextTurnPower> / ModifyAmount(this) 都会
        // 修改 creature.powers 集合；底层 IEnumerable 是 lazy + check version → 集合修改后下一次
        // MoveNext 抛 InvalidOperationException "Collection was modified"。
        foreach (var pw in p.Creature.GetPowerInstances<ComedianPower>().ToList())
            await pw.OnPersonaSwitch(ctx, source);
        if (p.Creature.HasPower<DisintegrationPower>())
        {
            var pw = p.Creature.GetPower<DisintegrationPower>();
            if (pw != null) await pw.OnPersonaSwitch(ctx, source);
        }
        // WakabaFortunePower 不再监听形态切换 —— 层数只在打出 WakabaFortune 卡时按当时 snapshot 叠加
        // （per spec：一旦打出，power 层数不再随切换继续叠加；只有再次打出卡才能叠加）
        if (p.Creature.HasPower<MortisCardPower>())
        {
            var pw = p.Creature.GetPower<MortisCardPower>();
            if (pw != null) await pw.OnPersonaSwitch(ctx, source);
        }
        if (p.Creature.HasPower<ThousandthPersonaPower>())
        {
            var pw = p.Creature.GetPower<ThousandthPersonaPower>();
            if (pw != null) await pw.OnPersonaSwitch(ctx, source);
        }
        if (p.Creature.HasPower<AddictionPower>())
        {
            var pw = p.Creature.GetPower<AddictionPower>();
            if (pw != null) await pw.OnPersonaSwitch(ctx, source);
        }
    }

    /// <summary>
    /// 根据当前形态把 NCreatureVisuals 的主 AnimatedSprite2D 换成对应 SpriteFrames。
    /// 不在战斗 / body 不是 AnimatedSprite2D / SpriteFrames 资源加载失败 → silent return。
    /// 切完强制 Play("idle") 重启动画，避免新 frames 不含旧 animation 名时停在空白。
    /// </summary>
    private static void SwapVisualsToCurrentForm(Player p)
    {
        try
        {
            var room = NCombatRoom.Instance;
            if (room == null) { Diag.Trace($"Forms.SwapVisuals[player={p.NetId}]: skip (NCombatRoom.Instance null)"); return; }
            var nc = room.GetCreatureNode(p.Creature);
            if (nc == null || nc.Visuals == null) { Diag.Trace($"Forms.SwapVisuals[player={p.NetId}]: skip (creature node/visuals null)"); return; }
            if (!GodotObject.IsInstanceValid(nc)) { Diag.Trace($"Forms.SwapVisuals[player={p.NetId}]: skip (creature node freed)"); return; }

            var body = nc.Visuals.GetCurrentBody();
            if (body is not AnimatedSprite2D anim) { Diag.Trace($"Forms.SwapVisuals[player={p.NetId}]: skip (body not AnimatedSprite2D)"); return; }
            if (!GodotObject.IsInstanceValid(anim)) { Diag.Trace($"Forms.SwapVisuals[player={p.NetId}]: skip (anim freed)"); return; }

            var frames = IsMortisForm(p)
                ? LoadFrames(MortisFramesPath, ref _mortisFrames)
                : LoadFrames(MutsumiFramesPath, ref _mutsumiFrames);
            if (frames == null) { Diag.Trace($"Forms.SwapVisuals[player={p.NetId}]: skip (frames null)"); return; }

            anim.SpriteFrames = frames;

            // 保留当前 X 符号——某些 boss（如帝王蟹）会通过设置负 Scale.X 让玩家立绘面朝左
            float xSign = anim.Scale.X >= 0 ? 1f : -1f;
            var target = IsMortisForm(p) ? MortisScale : MutsumiScale;
            anim.Position = IsMortisForm(p) ? MortisPosition : MutsumiPosition;
            anim.Scale = new Godot.Vector2(xSign * System.Math.Abs(target.X), target.Y);

            if (frames.HasAnimation("idle"))
                anim.Play("idle");
        }
        catch (System.Exception ex)
        {
            Diag.Exception($"Forms.SwapVisuals[player={p.NetId}]", ex);
        }
    }
}
