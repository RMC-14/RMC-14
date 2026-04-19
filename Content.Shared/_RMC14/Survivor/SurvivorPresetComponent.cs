using Content.Shared._RMC14.EntityPreset;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Survivor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SurvivorSystem))]
public sealed partial class SurvivorPresetComponent : Component, IRMCRandomizedPreset
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, List<EntProtoId>> RandomStartingGear = new();

    [DataField, AutoNetworkedField]
    public List<List<EntProtoId>> RandomOutfits = new();

    [DataField, AutoNetworkedField]
    public List<List<EntProtoId>> RandomGear = new();

    [DataField, AutoNetworkedField]
    public List<List<EntProtoId>> RandomWeapon = new();

    [DataField, AutoNetworkedField]
    public List<List<EntProtoId>> PrimaryWeapons = new();

    [DataField, AutoNetworkedField]
    public List<List<List<EntProtoId>>> RandomGearOther = new();

    [DataField, AutoNetworkedField]
    public bool TryEquipRandomOtherGear = true;

    [DataField, AutoNetworkedField]
    public bool TryRandomOutfitsInhand;

    [DataField, AutoNetworkedField]
    public bool TryEquipRandomWeapon;

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, (int, int)> RareItems = new();

    [DataField, AutoNetworkedField]
    public int RareItemCoefficient = 100;

    [DataField, AutoNetworkedField]
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
