using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.EntityPreset;

[RegisterComponent]
[Access(typeof(EntityPresetSystem))]
public sealed partial class EntityPresetComponent : Component, IRMCRandomizedPreset
{
    [DataField]
    public Dictionary<string, List<EntProtoId>> RandomStartingGear = new();

    [DataField]
    public List<List<EntProtoId>> RandomOutfits = new();

    [DataField]
    public List<List<EntProtoId>> RandomGear = new();

    [DataField]
    public List<List<EntProtoId>> RandomWeapon = new();

    [DataField]
    public List<List<EntProtoId>> PrimaryWeapons = new();

    [DataField]
    public List<List<List<EntProtoId>>> RandomGearOther = new();

    [DataField]
    public bool TryEquipRandomOtherGear = true;

    [DataField]
    public bool TryRandomOutfitsInhand;

    [DataField]
    public bool TryEquipRandomWeapon;

    [DataField]
    public Dictionary<EntProtoId, (int, int)> RareItems = new();

    [DataField]
    public int RareItemCoefficient = 100;

    [DataField]
    public float PrimaryWeaponChance = 0.6f;

    Dictionary<string, List<EntProtoId>> IRMCRandomizedPreset.RandomStartingGear => RandomStartingGear;
    List<List<EntProtoId>> IRMCRandomizedPreset.RandomOutfits => RandomOutfits;
    List<List<EntProtoId>> IRMCRandomizedPreset.RandomGear => RandomGear;
    List<List<EntProtoId>> IRMCRandomizedPreset.RandomWeapon => RandomWeapon;
    List<List<EntProtoId>> IRMCRandomizedPreset.PrimaryWeapons => PrimaryWeapons;
    List<List<List<EntProtoId>>> IRMCRandomizedPreset.RandomGearOther => RandomGearOther;
    bool IRMCRandomizedPreset.TryEquipRandomOtherGear => TryEquipRandomOtherGear;
    bool IRMCRandomizedPreset.TryRandomOutfitsInhand => TryRandomOutfitsInhand;
    bool IRMCRandomizedPreset.TryEquipRandomWeapon => TryEquipRandomWeapon;
    Dictionary<EntProtoId, (int, int)> IRMCRandomizedPreset.RareItems => RareItems;
    int IRMCRandomizedPreset.RareItemCoefficient => RareItemCoefficient;
    float IRMCRandomizedPreset.PrimaryWeaponChance => PrimaryWeaponChance;
}
