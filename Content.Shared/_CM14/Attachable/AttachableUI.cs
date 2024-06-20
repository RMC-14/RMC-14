using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Attachable;

[Serializable, NetSerializable]
public sealed class AttachableHolderStripUserInterfaceState(Dictionary<string, string?> attachableSlots)
    : BoundUserInterfaceState
{
    public Dictionary<string, string?> AttachableSlots = attachableSlots;
}

[Serializable, NetSerializable]
public sealed class AttachableHolderChooseSlotUserInterfaceState(List<string> attachableSlots) : BoundUserInterfaceState
{
    public List<string> AttachableSlots = attachableSlots;
}

[Serializable, NetSerializable]
public sealed class AttachableHolderDetachMessage(string slot) : BoundUserInterfaceMessage
{
    public readonly string Slot = slot;
}

[Serializable, NetSerializable]
public sealed class AttachableHolderAttachToSlotMessage(string slot) : BoundUserInterfaceMessage
{
    public readonly string Slot = slot;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AttachableWeaponMeleeModifierSet;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AttachableWeaponRangedModifierSet
{
    [DataField]
    public int ShotsPerBurst;

    [DataField]
    public FixedPoint2 DamageFlat = FixedPoint2.Zero;

    [DataField]
    public float RecoilFlat;

    [DataField]
    public double AngleIncrease = 1.0;

    [DataField]
    public double AngleDecay = 1.0;

    [DataField]
    public double MaxAngle = 1.0;

    [DataField]
    public double MinAngle = 1.0;

    [DataField]
    public float FireRate = 1.0f;

    [DataField]
    public float ProjectileSpeedFlat = 0;

    [DataField]
    public float ProjectileSpeedMultiplier = 1.0f;
}

[Serializable, NetSerializable]
public enum AttachmentUI : byte
{
    StripKey,
    ChooseSlotKey,
}
