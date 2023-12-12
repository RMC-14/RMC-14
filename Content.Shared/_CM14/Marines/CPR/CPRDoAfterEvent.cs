using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Marines.CPR;

[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
