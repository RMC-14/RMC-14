using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mortar;

[Serializable, NetSerializable]
public sealed partial class TargetMortarDoAfterEvent : SimpleDoAfterEvent
{
    public readonly Vector2i Vector;

    public TargetMortarDoAfterEvent(Vector2i vector)
    {
        Vector = vector;
    }
}
