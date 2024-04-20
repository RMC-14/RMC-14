using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TackleableComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Threshold = 5;

    [DataField, AutoNetworkedField]
    public TimeSpan Expires = TimeSpan.FromSeconds(4);
}
