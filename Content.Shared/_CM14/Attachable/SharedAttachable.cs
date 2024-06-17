using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
//using System.Math;


namespace Content.Shared._CM14.Attachable;

[Serializable, NetSerializable]
public sealed class AttachableHolderStripUserInterfaceState : BoundUserInterfaceState
{
    public Dictionary<string, string?> AttachableSlots;
    
    public AttachableHolderStripUserInterfaceState(Dictionary<string, string?> attachableSlots)
    {
        AttachableSlots = attachableSlots;
    }
}

[Serializable, NetSerializable]
public sealed class AttachableHolderChooseSlotUserInterfaceState : BoundUserInterfaceState
{
    public List<string> AttachableSlots;
    
    public AttachableHolderChooseSlotUserInterfaceState(List<string> attachableSlots)
    {
        AttachableSlots = attachableSlots;
    }
}

[Serializable, NetSerializable]
public sealed class AttachableHolderDetachMessage : BoundUserInterfaceMessage
{
    public readonly string Slot;
    
    public AttachableHolderDetachMessage(string slot)
    {
        Slot = slot;
    }
}

[Serializable, NetSerializable]
public sealed class AttachableHolderAttachToSlotMessage : BoundUserInterfaceMessage
{
    public readonly string Slot;
    
    public AttachableHolderAttachToSlotMessage(string slot)
    {
        Slot = slot;
    }
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AttachableWeaponModifierSet
{
    [DataField("shotsPerBurst")]
    public int FlatShotsPerBurst = 0;
    
    [DataField("damage")]
    public FixedPoint2 MultiplierDamage = 1.0;
    
    [DataField("recoil")]
    public float MultiplierCameraRecoilScalar = 1.0f;
    
    [DataField("angleIncrease")]
    public float MultiplierAngleIncrease = 1.0f;
    
    [DataField("angleDecay")]
    public float MultiplierAngleDecay = 1.0f;
    
    [DataField("maxAngle")]
    public float MultiplierMaxAngle = 1.0f;
    
    [DataField("minAngle")]
    public float MultiplierMinAngle = 1.0f;
    
    [DataField("fireRate")]
    public float MultiplierFireRate = 1.0f;
    
    [DataField("projectileSpeed")]
    public float MultiplierProjectileSpeed = 1.0f;
}

[Serializable, NetSerializable]
public enum AttachableHolderUiKeys : byte
{
    StripKey,
    ChooseSlotKey
}
