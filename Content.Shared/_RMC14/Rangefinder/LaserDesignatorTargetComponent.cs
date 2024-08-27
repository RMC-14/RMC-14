using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Rangefinder;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RangefinderSystem))]
public sealed partial class LaserDesignatorTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Id;

    [DataField, AutoNetworkedField]
    public EntityUid? LaserDesignator;
}
