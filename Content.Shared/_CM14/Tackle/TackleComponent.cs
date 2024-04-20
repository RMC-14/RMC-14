using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TackleComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Strength = 1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Min = 2;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Max = 6;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Chance = 0.35;

    [DataField, AutoNetworkedField]
    public TimeSpan Stun = TimeSpan.FromSeconds(5);
}
