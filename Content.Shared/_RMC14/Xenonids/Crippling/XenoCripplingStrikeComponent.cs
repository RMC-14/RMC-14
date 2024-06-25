using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Crippling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoCripplingStrikeSystem))]
public sealed partial class XenoCripplingStrikeComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 20;

    [DataField, AutoNetworkedField]
    public TimeSpan ActiveDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedMultiplier = FixedPoint2.New(0.75);

    [DataField, AutoNetworkedField]
    public TimeSpan SlowDuration = TimeSpan.FromSeconds(5);
}
