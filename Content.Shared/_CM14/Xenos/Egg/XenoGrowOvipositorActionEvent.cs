using Content.Shared.Actions;

namespace Content.Shared._CM14.Xenos.Egg;

public sealed partial class XenoGrowOvipositorActionEvent : InstantActionEvent
{
    [DataField]
    public int AttachPlasmaCost = 700;

    [DataField]
    public TimeSpan AttachDoAfter = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan DetachDoAfter = TimeSpan.FromSeconds(5);
}
