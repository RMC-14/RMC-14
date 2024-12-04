using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Heal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoHealSystem))]
public sealed partial class XenoHealComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? HealEffect = "RMCEffectHealQueen";

    [DataField, AutoNetworkedField]
    public int Radius = 4;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Percentage = 0.3;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenHeals = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextHeal;
}
