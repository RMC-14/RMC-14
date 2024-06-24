using Content.Shared.Actions;

namespace Content.Shared._RMC14.Xenonids.Egg;

public sealed partial class XenoGrowOvipositorActionEvent : InstantActionEvent
{
    [DataField]
    public int AttachPlasmaCost = 700;

    [DataField]
    public TimeSpan AttachDoAfter = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan DetachDoAfter = TimeSpan.FromSeconds(5);
}
