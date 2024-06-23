using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Requisitions.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRequisitionsSystem))]
public sealed partial class RequisitionsGearComponent : Component
{
    [DataField, AutoNetworkedField]
    public RequisitionsGearMode Mode;

    [DataField, AutoNetworkedField]
    public string StaticState = "base";

    [DataField, AutoNetworkedField]
    public string MovingState = "moving";
}

[Serializable, NetSerializable]
public enum RequisitionsGearLayers
{
    Base
}

[Serializable, NetSerializable]
public enum RequisitionsGearMode
{
    Static,
    Moving
}
