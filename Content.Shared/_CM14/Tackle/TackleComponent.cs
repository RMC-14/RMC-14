using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TackleComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Strength = 1;

    [DataField, AutoNetworkedField]
    public TimeSpan Stun = TimeSpan.FromSeconds(8);
}
