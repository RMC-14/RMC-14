using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.EggMorpher;

[Serializable, NetSerializable]
public sealed partial class EggMorpherEmptyDoafterEvent : SimpleDoAfterEvent
{
    [DataField]
    public bool Transfered = false;
}
