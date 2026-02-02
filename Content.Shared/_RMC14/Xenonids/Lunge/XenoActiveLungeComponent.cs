using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Xenonids.Lunge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoLungeSystem))]
public sealed partial class XenoActiveLungeComponent : Component
{
    [DataField, AutoNetworkedField]
    public MapCoordinates Origin;

    [DataField, AutoNetworkedField]
    public Vector2 Charge;

    [DataField, AutoNetworkedField]
    public EntityUid Target;

    [DataField, AutoNetworkedField]
    public MapCoordinates TargetCoordinates;

    [DataField, AutoNetworkedField]
    public float Range;

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime;
}
