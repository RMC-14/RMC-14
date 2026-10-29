using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Generation;

public abstract class SharedRMCResearchComputerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    protected bool TryStartPrint(Entity<RMCResearchComputerComponent> ent)
    {
        var time = _timing.CurTime;
        if (time < ent.Comp.NextPrintAt)
            return false;

        ent.Comp.NextPrintAt = time + ent.Comp.PrintCooldown;
        Dirty(ent);
        return true;
    }
}
