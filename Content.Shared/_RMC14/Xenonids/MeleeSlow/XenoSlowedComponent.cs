using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.MeleeSlow;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoMeleeSlowSystem))]
public sealed partial class XenoSlowedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;

    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedMultiplier = 0.87f;

    [DataField, AutoNetworkedField]
    public EntityUid? Effect = null;
}
