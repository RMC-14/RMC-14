using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;

[Serializable, NetSerializable]
public enum BulletBoxLayers
{
    Fill,
}

[Serializable, NetSerializable]
public enum BulletBoxVisuals
{
    Empty = 0,
    Low,
    Medium,
    High,
    Full,
}

