using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;

[Serializable, NetSerializable]
public sealed partial class BulletBoxTransferDoAfterEvent : SimpleDoAfterEvent
{
    public readonly bool ToBox;

    public BulletBoxTransferDoAfterEvent(bool toFrom)
    {
        ToBox = toFrom;
    }
}
