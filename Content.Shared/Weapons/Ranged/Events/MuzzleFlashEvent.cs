using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised whenever a muzzle flash client-side entity needs to be spawned.
/// </summary>
[Serializable, NetSerializable]
public sealed class MuzzleFlashEvent : EntityEventArgs
{
    public NetEntity Uid;
    public string Prototype;
    public Vector2 Offset; // RMC14
    public Vector2 OriginOffset; // RMC14

    public Angle Angle;

    public MuzzleFlashEvent(NetEntity uid, string prototype, Angle angle, Vector2 offset = default, Vector2 originOffset  = default)
    {
        Uid = uid;
        Prototype = prototype;
        Angle = angle;
        Offset = offset;
        OriginOffset = originOffset;
    }
}
