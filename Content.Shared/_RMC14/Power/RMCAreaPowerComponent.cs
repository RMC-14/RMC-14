using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCAreaPowerComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> Apcs = new();

    [ViewVariables]
    public HashSet<EntityUid> EquipmentReceivers = new();

    [ViewVariables]
    public HashSet<EntityUid> LightingReceivers = new();

    [ViewVariables]
    public HashSet<EntityUid> EnvironmentReceivers = new();

    [ViewVariables]
    public float[] Load = new float[Enum.GetValues<RMCPowerChannel>().Length];
}
