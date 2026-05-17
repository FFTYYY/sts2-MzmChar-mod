using BaseLib.Abstracts;

namespace MzmChar.Game;

/// <summary>
/// 角色专属遗物池。带 [Pool(typeof(MzmCharRelicPool))] 的遗物会自动加进来。
/// </summary>
public class MzmCharRelicPool : CustomRelicPoolModel
{
    public override bool IsShared => false;
    public override string? BigEnergyIconPath  => "res://MzmChar/characters/energy_big.png";
    public override string? TextEnergyIconPath => "res://MzmChar/characters/energy_text.png";
}
