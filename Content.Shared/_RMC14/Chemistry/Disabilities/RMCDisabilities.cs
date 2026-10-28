using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

public static class RMCDisabilities
{
    public static void ClearAll(IEntityManager entities, EntityUid uid)
    {
        entities.RemoveComponent<RMCNearsightedComponent>(uid);
        entities.RemoveComponent<RMCBlindDisabilityComponent>(uid);
        entities.RemoveComponent<RMCNervousComponent>(uid);
        entities.RemoveComponent<RMCOpiateReceptorDeficiencyComponent>(uid);
        entities.RemoveComponent<RMCDeafDisabilityComponent>(uid);
        entities.RemoveComponent<RMCMuteDisabilityComponent>(uid);
    }
}
