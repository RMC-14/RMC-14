using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCAreaPowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Apcs = new();

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> EquipmentReceivers = new();

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> LightingReceivers = new();

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> EnvironmentReceivers = new();

    [DataField, AutoNetworkedField]
    public int[] Load = new int[Enum.GetValues<RMCPowerChannel>().Length];
}
