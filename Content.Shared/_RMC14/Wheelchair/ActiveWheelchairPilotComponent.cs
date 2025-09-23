using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Wheelchair;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WheelchairSystem))]
public sealed partial class ActiveWheelchairPilotComponent : Component
{
    [DataField]
    public EntityUid? BellActionEntity;
}