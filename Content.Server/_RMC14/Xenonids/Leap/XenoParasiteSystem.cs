using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Mind;
using Robust.Shared.Player;
using Content.Server.NPC.HTN;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Xenonids.Leap;

public sealed class XenoParasiteSystem : SharedXenoParasiteSystem
{
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly HTNSystem _htn = default!;

    private static readonly ProtoId<HTNCompoundPrototype> ActiveTask = "RMCParasiteActiveCompound";

    private static readonly ProtoId<HTNCompoundPrototype> DyingTask = "RMCParasiteDyingCompound";

    protected override void ParasiteLeapHit(Entity<XenoParasiteComponent> parasite)
    {
        if (!TryComp(parasite, out ActorComponent? actor))
            return;

        RemComp<GhostTakeoverAvailableComponent>(parasite);

        var session = actor.PlayerSession;

        Entity<MindComponent> mind;
        if (_mind.TryGetMind(session, out var mindId, out var mindComp))
            mind = (mindId, mindComp);
        else
            mind = _mind.CreateMind(session.UserId);

        _ghostSystem.SpawnGhost((mind.Owner, mind.Comp), parasite);
    }

    protected override void ChangeHTN(EntityUid parasite, ParasiteMode mode)
    {
        if (!TryComp<HTNComponent>(parasite, out var hTN))
            return;

        ProtoId<HTNCompoundPrototype>? RootTask = null;

        switch (mode)
        {
            case ParasiteMode.Active:
                RootTask = ActiveTask;
                break;
            case ParasiteMode.Dying:
                RootTask = DyingTask;
                break;
            default:
                return;
        }

        hTN.RootTask = new HTNCompoundTask { Task = RootTask.Value };
        _htn.Replan(hTN);
    }
}
