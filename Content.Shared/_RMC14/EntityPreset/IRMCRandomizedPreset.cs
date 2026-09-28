using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.EntityPreset;

public interface IRMCRandomizedPreset
{
    Dictionary<string, List<EntProtoId>> RandomStartingGear { get; }
    List<List<EntProtoId>> RandomOutfits { get; }
    List<List<EntProtoId>> RandomGear { get; }
    List<List<EntProtoId>> RandomWeapon { get; }
    List<List<EntProtoId>> PrimaryWeapons { get; }
    List<List<List<EntProtoId>>> RandomGearOther { get; }
    bool TryEquipRandomOtherGear { get; }
    bool TryRandomOutfitsInhand { get; }
    bool TryEquipRandomWeapon { get; }
    Dictionary<EntProtoId, (int, int)> RareItems { get; }
    int RareItemCoefficient { get; }
    float PrimaryWeaponChance { get; }
}
