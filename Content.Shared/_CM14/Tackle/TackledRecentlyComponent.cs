using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TackledRecentlyComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Current;

    [DataField, AutoNetworkedField]
    public TimeSpan LastTackled;
}
