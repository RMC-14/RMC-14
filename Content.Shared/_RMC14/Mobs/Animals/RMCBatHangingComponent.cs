using System.Numerics;
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

    [DataField]
    public Vector2 HangOffset = new(0f, 0.25f);

    [DataField]
    public float IdleRange = 2f;

    [DataField]
    public float MinimumIdleTime = 2f;

    [DataField]
    public float MaximumIdleTime = 6f;
}

[Serializable, NetSerializable]
public enum RMCBatVisuals : byte
{
    Hanging,
}
