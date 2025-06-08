using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel;

[Serializable, NetSerializable]
public sealed partial class IntelSubmitDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public NetEntity Intel;

    [DataField]
    public int Amount;
}
