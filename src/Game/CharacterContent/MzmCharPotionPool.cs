using BaseLib.Abstracts;

namespace MzmChar.Game;

/// <summary>
/// 角色专属药水池 —— 现在没有自定义药水，将来要做就加 [Pool(typeof(MzmCharPotionPool))] 的 PotionModel。
/// </summary>
public class MzmCharPotionPool : CustomPotionPoolModel
{
    public override bool IsShared => false;
    public override string? BigEnergyIconPath  => "res://MzmChar/characters/energy_big.png";
    public override string? TextEnergyIconPath => "res://MzmChar/characters/energy_text.png";
}
