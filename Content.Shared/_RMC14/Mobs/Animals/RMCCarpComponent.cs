using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Mobs.Animals;

[RegisterComponent]
public sealed partial class RMCCarpComponent : Component
{
    [DataField]
    public float TargetSearchRange = 7f;

    [DataField]
    public TimeSpan GnashCooldownMin = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan GnashCooldownMax = TimeSpan.FromSeconds(12);

    [DataField]
    public float KnockdownChance = 0.15f;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextGnashAt;
}
