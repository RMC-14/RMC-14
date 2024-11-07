using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stun;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AmmoSlowedComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 SlowMultiplier = FixedPoint2.New(0.87);

    [DataField, AutoNetworkedField]
    public FixedPoint2 SuperSlowMultiplier = FixedPoint2.New(0.75);

    [DataField, AutoNetworkedField]
    public TimeSpan SuperExpireTime;

    [DataField, AutoNetworkedField]
    public TimeSpan ExpireTime;

    [DataField, AutoNetworkedField]
    public bool SuperSlowActive = true;
}
