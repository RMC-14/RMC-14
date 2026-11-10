using Content.Shared._RMC14.Power;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Biomass;

public abstract class SharedRMCBiomassAnalyzerMachineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedRMCPowerSystem _power = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    protected bool IsMachinePowered(EntityUid uid)
    {
        return _power.IsPowered(uid);
    }

    public EntityUid? GetFirstQueuedOrgan(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        foreach (var job in ent.Comp.Queue)
        {
            if (job.Organ is { } organ && TryGetEntity(organ, out var organUid))
                return organUid;
        }

        return null;
    }

    protected void StartPrintingAnimation(Entity<RMCBiomassAnalyzerMachineComponent> ent, TimeSpan duration, RMCBiomassAnalyzerVisualState animation)
    {
        ent.Comp.PrintingUntil = Timing.CurTime + duration;
        ent.Comp.Animation = animation;
        ent.Comp.AnimationStart = Timing.CurTime;
        Dirty(ent);
    }

    protected void UpdateAppearance(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        var loaded = IsMachinePowered(ent) &&
                     _container.TryGetContainer(ent, ent.Comp.OrganContainerId, out var container) &&
                     container.ContainedEntities.Count > 0;

        _appearance.SetData(ent, RMCBiomassAnalyzerVisuals.OrganLoaded, loaded);
    }
}
