using Content.Server.Ghost;
using Content.Shared._RMC14.Gibbing;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server._RMC14.Ghost;

public sealed partial class RMCGhostOnGibSystem : EntitySystem
{
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MindContainerComponent, RMCBeforeBodyGibbedEvent>(OnBeforeBodyGibbed);
    }

    private void OnBeforeBodyGibbed(Entity<MindContainerComponent> ent, ref RMCBeforeBodyGibbedEvent args)
    {
        if (!ent.Comp.GhostOnShutdown || HasComp<TransferMindOnGibComponent>(ent))
            return;

        if (!_mind.TryGetMind(ent, out var mindId, out var mind, ent.Comp) || mind.VisitingEntity != null)
            return;

        _ghost.SpawnGhost((mindId, mind), ent.Owner);
    }
}
