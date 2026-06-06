using Content.Shared._RMC14.Xenonids;
using Content.Shared.Mobs.Components;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

public static class XenoDespoilerVictims
{
    public static bool IsValidVictim(IEntityManager entities, EntityUid victim, EntityUid caster)
    {
        if (victim == caster)
            return false;
        if (!entities.HasComponent<MobStateComponent>(victim))
            return false;
        if (entities.HasComponent<XenoComponent>(victim))
            return false;
        return true;
    }
}
