using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Catalyze;

public sealed class XenoDespoilerCatalyzeFlagSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public bool IsEmpowered(Entity<XenoDespoilerComponent> ent)
    {
        return ent.Comp.NextAbilityEmpowered && _timing.CurTime <= ent.Comp.EmpowerExpiresAt;
    }

    public bool TakeEmpowerment(Entity<XenoDespoilerComponent> ent)
    {
        var wasActive = IsEmpowered(ent);

        if (ent.Comp.NextAbilityEmpowered)
        {
            ent.Comp.NextAbilityEmpowered = false;
            Dirty(ent);
        }

        return wasActive;
    }
}
