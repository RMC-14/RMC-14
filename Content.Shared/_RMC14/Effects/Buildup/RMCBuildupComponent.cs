using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Effects.Buildup;

[Access(typeof(RMCBuildupSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCBuildupComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<RMCBuildupPrototype>, RMCBuildupState> States = new();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class RMCBuildupState
{
    [DataField]
    public int Current;

    [DataField]
    public TimeSpan? NextDecayAt;
}
