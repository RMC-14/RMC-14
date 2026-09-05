namespace Content.Server._RMC14.Tutorial;

public sealed partial class RMCTutorialDummySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCTutorialDummyComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<RMCTutorialDummyComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.RemoveComponents != null)
            EntityManager.RemoveComponents(ent.Owner, ent.Comp.RemoveComponents);
    }
}
