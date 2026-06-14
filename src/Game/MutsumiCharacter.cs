using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Patches.UI;  // RelicIconData (Yummy Cookie 自定义图标)
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 所有 Custom* / *Sfx path 在本类显式 override。
///   - [OURS] 指向 pack/MzmChar/ 下的资源
///   - [BORROW: ironclad] 指向 vanilla Ironclad 资源（占位）
/// </summary>
public class MutsumiCharacter : CustomCharacterModel
{
    /// <summary>所有借用资源都从这个角色派生。改成 "necrobinder" / "silent" 等一键换占位风格。</summary>
    private const string BorrowFrom = "ironclad";

    // ===== 必填：核心数值 =====
    // 主题色：深 sage 绿（呼应小睦的灰绿色头发，比头发再深一点）
    private static readonly Color ThemeColor = new(0.30f, 0.55f, 0.40f);

    public override Color NameColor => ThemeColor;
    public override Color MapDrawingColor => ThemeColor;   // 地图上画路径的颜色
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 75;

    // 0 = 跳过 BaseLib CustomAnimationPatch.WaitCustomAnim 的 Cmd.Wait，避免多人同死时
    // 并发 wait 窗口造成 combat-end sync 卡死。视觉上 AutoReturnToIdlePatch 让 die anim 停在
    // 当前帧不切回 idle。
    public override float DeathAnimTime => 0f;

    // 池子用我们自己的（角色身份核心，不能借）
    public override CardPoolModel CardPool => ModelDb.CardPool<MzmCharCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MzmCharRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MzmCharPotionPool>();

    // 起始牌组：4×打击 + 4×防御 + 1×人格切换 + 1×睦头人出击！ = 10 张
    // (额外的「表人格」「里人格」由初始遗物「第二人格」在战斗开始时加进手牌)
    public override IEnumerable<CardModel> StartingDeck =>
        Enumerable.Repeat<CardModel>(ModelDb.Card<MuStrike>(), 4)
            .Concat(Enumerable.Repeat<CardModel>(ModelDb.Card<MuDefend>(), 4))
            .Append<CardModel>(ModelDb.Card<SwitchPersona>())
            .Append<CardModel>(ModelDb.Card<MutsumiCharge>());

    public override IReadOnlyList<RelicModel> StartingRelics =>
        new RelicModel[] { ModelDb.Relic<SecondPersonaRelic>() };

    // ===== [OURS] 我们自己出的视觉资源 ===== //
    public override string? CustomCharacterSelectIconPath => "res://MzmChar/characters/select.png";
    public override string? CustomCharacterSelectLockedIconPath => "res://MzmChar/characters/select.png";
    public override string? CustomCharacterSelectBg => "res://MzmChar/scenes/char_select_bg.tscn";
    // CustomIconTexturePath 在 vanilla 多个 TextureRect 用，不同 .tscn stretch 不同。
    // 用 ≤128 尺寸的 button_small.png 兼容；顶部 HUD 走 character_icon.tscn 引高清 button.png。
    public override string? CustomIconTexturePath => "res://MzmChar/characters/button_small.png";
    public override string? CustomIconOutlineTexturePath => "res://MzmChar/characters/button.png";

    // ===== 战斗中角色视觉 —— 用我们的 visuals.tscn (含 BaseLib 要求的 7 个子节点) =====
    // 想换成 Spine 动画：把 visuals.tscn 里的 Visuals 节点从 Sprite2D 改成 SpineSprite + skeleton_data_res
    public override string? CustomVisualPath => "res://MzmChar/scenes/visuals.tscn";

    // ===== [BORROW: ironclad] 动画 / 商店 / 休息处 / SFX ===== //
    // 卡片拖尾 vfx
    public override string? CustomTrailPath => SceneHelper.GetScenePath("vfx/card_trail_" + BorrowFrom);
    // 顶部信息栏的整个头像 Control —— 用我们的 character_icon.tscn（TextureRect 包 button.png）
    public override string? CustomIconPath => "res://MzmChar/scenes/character_icon.tscn";
    // 能量计数器：路径借 Ironclad 的场景结构，但用 CustomEnergyCounter struct 把图层换成我们自己的 energy_big.png
    // BaseLib 的 EnergyCounterPatch 会拦截 NEnergyCounter.Create 用我们的 pathFunc 替换图层
    public override string? CustomEnergyCounterPath => SceneHelper.GetScenePath("combat/energy_counters/" + BorrowFrom + "_energy_counter");
    public override CustomEnergyCounter? CustomEnergyCounter => new(
        pathFunc: layer => "res://MzmChar/characters/energy_big.png",
        outlineColor: ThemeColor,
        burstColor: Colors.White);
    // 休息处坐姿 -- 用我们自己的 rest_site.tscn (Sprite2D 显示 rest_site_portrait.png)
    public override string? CustomRestSiteAnimPath => "res://MzmChar/scenes/rest_site.tscn";
    // 商店站姿 -- 用我们自己的 merchant.tscn (Sprite2D 显示 merchant_portrait.png)
    public override string? CustomMerchantAnimPath => "res://MzmChar/scenes/merchant.tscn";
    // 地图上的玩家标记
    public override string? CustomMapMarkerPath => ImageHelper.GetImagePath("packed/map/icons/map_marker_" + BorrowFrom + ".png");
    // 选角转场材质 —— 自做的 shader 是这次白屏的最可疑嫌犯，借 Ironclad 的稳
    public override string? CustomCharacterSelectTransitionPath => "res://materials/transitions/" + BorrowFrom + "_transition_mat.tres";
    // Yummy Cookie 遗物的小睦定制饼干造型（BaseLib 在角色注册时自动绑定 RelicImageOverridePatch）
    // 单 PNG 三用：Big/Packed/Outline 同图，跟我们其他遗物图标的做法一致
    public override RelicIconData? CustomYummyCookie => new RelicIconData(
        BigIconPath:           "res://MzmChar/relics/yummy_cookie_mzm.png",
        PackedIconPath:        "res://MzmChar/relics/yummy_cookie_mzm.png",
        PackedIconOutlinePath: "res://MzmChar/relics/yummy_cookie_mzm.png");

    // 多人模式手势贴图
    public override string? CustomArmPointingTexturePath => "res://MzmChar/characters/hand_point.png";
    public override string? CustomArmRockTexturePath => "res://MzmChar/characters/hand_rock.png";
    public override string? CustomArmPaperTexturePath => "res://MzmChar/characters/hand_paper.png";
    public override string? CustomArmScissorsTexturePath => "res://MzmChar/characters/hand_scissors.png";

    // SFX —— 这些是 CharacterModel 直接虚属性（不带 "Custom" 前缀），返回 FMOD event 路径
    public override string CharacterSelectSfx => $"event:/sfx/characters/{BorrowFrom}/{BorrowFrom}_select";
    public override string CharacterTransitionSfx => $"event:/sfx/ui/wipe_{BorrowFrom}";
    public override string? CustomAttackSfx => $"event:/sfx/characters/{BorrowFrom}/{BorrowFrom}_attack";
    public override string? CustomCastSfx => $"event:/sfx/characters/{BorrowFrom}/{BorrowFrom}_cast";
    public override string? CustomDeathSfx => $"event:/sfx/characters/{BorrowFrom}/{BorrowFrom}_die";

    // Architect 攻击 vfx 列表（借 Ironclad 的常见效果）
    public override List<string> GetArchitectAttackVfx() => new()
    {
        "vfx/vfx_attack_blunt",
        "vfx/vfx_heavy_blunt",
        "vfx/vfx_attack_slash",
        "vfx/vfx_bloody_impact",
        "vfx/vfx_rock_shatter",
    };

    // ===== 本地化（中英双语 switch） =====
    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CharacterLoc(
            Title: "若叶睦",
            TitleObject: "若叶睦",
            Description: "一个寡言的少女，在某支乐队里担任节奏吉他。\n不知道为什么，出现在了尖塔的门口。",
            PronounObject: "她",
            PronounSubject: "她",
            PronounPossessive: "她的",
            PossessiveAdjective: "她的",
            AromaPrinciple: "……搞不懂。",
            EndTurnPingAlive: "……嗯。",
            EndTurnPingDead: "……（说不出话）",
            EventDeathPrevention: "……还没到结束的时候。",
            GoldMonologue: "金币……会用得上的吧。",
            CardsModifierTitle: "若叶睦的卡牌",
            CardsModifierDescription: "若叶睦的卡牌现在会出现在奖励和商店中。"),
        _ => new CharacterLoc(
            Title: "Wakaba Mutsumi",
            TitleObject: "Wakaba Mutsumi",
            Description: "A quiet girl who plays rhythm guitar in a band.\nFor some reason, she ended up at the door of the spire.",
            PronounObject: "her",
            PronounSubject: "she",
            PronounPossessive: "hers",
            PossessiveAdjective: "her",
            AromaPrinciple: "...don't get it.",
            EndTurnPingAlive: "...mm.",
            EndTurnPingDead: "...(can't speak)",
            EventDeathPrevention: "...not yet. The set isn't finished.",
            GoldMonologue: "Gold... it'll come in handy.",
            CardsModifierTitle: "Wakaba Mutsumi's Cards",
            CardsModifierDescription: "Wakaba Mutsumi's cards will now appear in rewards and shops."),
    };
}
