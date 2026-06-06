using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[Serializable, NetSerializable]
public sealed partial class HiveBoonActivateEvolutionEvent : HiveBoonEvent
{
    [DataField]
    public FixedPoint2 Multiplier = 2;

    [DataField]
    public bool BypassOvipositor;
}
