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
public sealed partial class RMCBatHangingComponent : Component
{
    [DataField]
    public bool Hanging;

    [DataField]
    public TimeSpan CheckCooldown = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextCheckAt;

    [DataField]
    public float HangChance = 0.25f;

    [DataField]
    public float WakeChance = 0.08f;

    [DataField]
    public float DisturbanceWakeChance = 0.35f;

    [DataField]
    public float DisturbanceRange = 4f;

    [DataField]
    public bool RequireBlockedNorth = true;
}

[Serializable, NetSerializable]
public enum RMCBatVisuals : byte
{
    Hanging,
}
