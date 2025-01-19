namespace Content.Shared._RMC14.Pointing;

public sealed class RMCPointingSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPointingComponent, RMCGetPointingArrowEvent>(OnGetPointingArrow);
    }

    private void OnGetPointingArrow(Entity<RMCPointingComponent> ent, ref RMCGetPointingArrowEvent ev)
    {
        ev.Arrow = ent.Comp.Arrow;
    }
}
