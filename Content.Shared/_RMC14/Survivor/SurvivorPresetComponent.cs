using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Survivor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SurvivorSystem))]
public sealed partial class SurvivorPresetComponent : Component
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
    public Dictionary<EntProtoId, (int, int)> RareItems = new();

    [DataField, AutoNetworkedField]
    public int RareItemCoefficent = 100;

    [DataField, AutoNetworkedField]
    public float PrimaryWeaponChance = 0.6f;
}
