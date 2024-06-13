using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Scoping;

[Serializable, NetSerializable]
public sealed class CMScopeToggleEvent(NetEntity user, Vector2 eyeOffset) : EntityEventArgs
{
    public NetEntity User { get; set; } = user;
    public Vector2 EyeOffset { get; set; } = eyeOffset;
}
