using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

public sealed class XenoDespoilerCatalyzeFlagSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public bool IsEmpowered(EntityUid uid, XenoDespoilerComponent comp)
    {
        return comp.NextAbilityEmpowered && _timing.CurTime <= comp.EmpowerExpiresAt;
    }

    public bool TakeEmpowerment(EntityUid uid, XenoDespoilerComponent comp)
    {
        var wasActive = IsEmpowered(uid, comp);

        if (comp.NextAbilityEmpowered)
        {
            comp.NextAbilityEmpowered = false;
            Dirty(uid, comp);
        }

        return wasActive;
    }
}
