using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Rangefinder;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RangefinderSystem))]
public sealed partial class ActiveLaserDesignatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityCoordinates Origin;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public float BreakRange = 0.5f;
}
