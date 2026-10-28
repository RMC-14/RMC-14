using Content.Shared.Rejuvenate;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

public sealed class RMCDisabilityRejuvenateSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCNearsightedComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<RMCBlindDisabilityComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<RMCNervousComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<RMCOpiateReceptorDeficiencyComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<RMCDeafDisabilityComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<RMCMuteDisabilityComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnRejuvenate<T>(Entity<T> ent, ref RejuvenateEvent args) where T : IComponent
    {
        RemCompDeferred<T>(ent);
    }
}
