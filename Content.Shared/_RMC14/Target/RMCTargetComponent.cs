using System.Numerics;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.ShootingTarget;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCTargetSystem))]
public sealed partial class RMCTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DpsWindow = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(0.25);

    [DataField, AutoNetworkedField]
    public Vector2 PopupOffset = new(0f, 0.6f);

    [ViewVariables]
    public Queue<(TimeSpan Time, FixedPoint2 Damage)> DamageSamples = new();

    [ViewVariables]
    public FixedPoint2 WindowDamage = FixedPoint2.Zero;

    [ViewVariables]
    public FixedPoint2 TotalDamage = FixedPoint2.Zero;

    [ViewVariables]
    public TimeSpan LastPopupAt = TimeSpan.MinValue;
}
