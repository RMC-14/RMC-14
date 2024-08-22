using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TackleComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Strength = 1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Threshold = 4;

    [DataField, AutoNetworkedField]
    public TimeSpan Stun = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public float Chance = 0.35f;
}
