using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Designer.Events;

[Serializable, NetSerializable]
public sealed partial class DesignerRemoteThickenResinDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public int PlasmaCost;

    [DataField]
    public NetEntity TargetEntity;

    [DataField]
    public float Range;

    public DesignerRemoteThickenResinDoAfterEvent(int plasmaCost, NetEntity target, float range)
    {
        PlasmaCost = plasmaCost;
        TargetEntity = target;
        Range = range;
    }
}
