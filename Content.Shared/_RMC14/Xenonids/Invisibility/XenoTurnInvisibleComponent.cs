using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Invisibility;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoInvisibilitySystem))]
public sealed partial class XenoTurnInvisibleComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = FixedPoint2.New(20);

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan FullCooldown = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public TimeSpan ToggleLockoutTime = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public float Opacity = 0.1f;

    [DataField, AutoNetworkedField]
    public float ManualRefundMultiplier = 0.9f;

    [DataField, AutoNetworkedField]
    public float RevealedRefundMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedMultiplier = FixedPoint2.New(1.15);
}
