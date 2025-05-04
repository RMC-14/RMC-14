using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Survivor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SurvivorSystem))]
public sealed partial class SurvivorPresetComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<List<EntProtoId>> RandomGear = new();

    [DataField, AutoNetworkedField]
    public List<List<EntProtoId>> RandomWeapon = new();

    [DataField, AutoNetworkedField]
    public List<List<EntProtoId>> PrimaryWeapons = new();

    [DataField, AutoNetworkedField]
    public List<List<List<EntProtoId>>> RandomGearOther = new();

    [DataField, AutoNetworkedField]
    public float PrimaryWeaponChance = 0.6f;
}
