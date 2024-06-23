using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Requisitions.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRequisitionsSystem))]
public sealed partial class RequisitionsRailingComponent : Component
{
    [DataField, AutoNetworkedField]
    public RequisitionsRailingMode Mode;

    [DataField, AutoNetworkedField]
    public string LoweredState = "lowered";

    [DataField, AutoNetworkedField]
    public string RaisedState = "raised";

    [DataField, AutoNetworkedField]
    public string LoweringState = "lowering";

    [DataField, AutoNetworkedField]
    public string RaisingState = "raising";

    [DataField, AutoNetworkedField]
    public TimeSpan RailingRaiseDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public string Fixture = "fix1";

    public object? LowerAnimation;

    public object? RaiseAnimation;
}

[Serializable, NetSerializable]
public enum RequisitionsRailingLayers
{
    Base
}

[Serializable, NetSerializable]
public enum RequisitionsRailingMode
{
    Lowered,
    Raised,
    Lowering,
    Raising
}
