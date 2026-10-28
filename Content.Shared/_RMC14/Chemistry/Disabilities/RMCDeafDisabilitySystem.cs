using Content.Shared._RMC14.Deafness;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

public sealed class RMCDeafDisabilitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCDeafDisabilityComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCDeafDisabilityComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<RMCDeafDisabilityComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<DeafComponent>(ent.Owner);
    }

    private void OnShutdown(Entity<RMCDeafDisabilityComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<DeafComponent>(ent.Owner);
    }
}
