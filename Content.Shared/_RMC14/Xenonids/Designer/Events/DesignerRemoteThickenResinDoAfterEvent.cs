using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Designer.Events;

[Serializable, NetSerializable]
public sealed partial class DesignerRemoteThickenResinDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public int PlasmaCost;

    public DesignerRemoteThickenResinDoAfterEvent(int plasmaCost)
    {
        PlasmaCost = plasmaCost;
    }
}
