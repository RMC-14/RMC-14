using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

public sealed class RMCVisionDisabilitySystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blinding = default!;

    private const int NearsightedFloor = 4;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCBlindDisabilityComponent, ComponentStartup>(OnBlindStartup);
        SubscribeLocalEvent<RMCBlindDisabilityComponent, ComponentShutdown>(OnBlindShutdown);
        SubscribeLocalEvent<RMCNearsightedComponent, ComponentStartup>(OnNearsightedStartup);
        SubscribeLocalEvent<RMCNearsightedComponent, ComponentShutdown>(OnNearsightedShutdown);
    }

    private void OnBlindStartup(Entity<RMCBlindDisabilityComponent> ent, ref ComponentStartup args)
    {
        Apply(ent.Owner, blind: true);
    }

    private void OnBlindShutdown(Entity<RMCBlindDisabilityComponent> ent, ref ComponentShutdown args)
    {
        Apply(ent.Owner, blind: false);
    }

    private void OnNearsightedStartup(Entity<RMCNearsightedComponent> ent, ref ComponentStartup args)
    {
        Apply(ent.Owner, blind: HasComp<RMCBlindDisabilityComponent>(ent.Owner), nearsighted: true);
    }

    private void OnNearsightedShutdown(Entity<RMCNearsightedComponent> ent, ref ComponentShutdown args)
    {
        Apply(ent.Owner, blind: HasComp<RMCBlindDisabilityComponent>(ent.Owner), nearsighted: false);
    }

    private void Apply(EntityUid uid, bool blind, bool? nearsighted = null)
    {
        if (!TryComp<BlindableComponent>(uid, out var blindable))
            return;

        var isNearsighted = nearsighted ?? HasComp<RMCNearsightedComponent>(uid);
        var floor = blind ? blindable.MaxDamage : isNearsighted ? NearsightedFloor : 0;
        _blinding.SetMinDamage((uid, blindable), floor);
    }
}
