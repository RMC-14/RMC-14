using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(XenoSystem))]
public sealed partial class XenoRegenComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 FlatHealing = 0.5;

    [DataField, AutoNetworkedField]
    public FixedPoint2 CritHealMultiplier = 0.33;

    [DataField, AutoNetworkedField]
    public FixedPoint2 RestHealMultiplier = 1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 StandHealingMultiplier = 0.4;

    [DataField, AutoNetworkedField]
    public float MaxHealthDivisorHeal = 65;

    [DataField, AutoNetworkedField]
    public bool HealOffWeeds;

    [DataField, AutoNetworkedField]
    public TimeSpan RegenCooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextRegenTime;
}
