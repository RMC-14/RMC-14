using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Mind;
using Robust.Shared.Player;
using Content.Shared.Mind.Components;
using Robust.Shared.Containers;

namespace Content.Server._RMC14.Xenonids.Leap;

public sealed class XenoParasiteSystem : SharedXenoParasiteSystem
{
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityManager _entities = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoParasiteComponent, MindAddedMessage>(OnTakeRole);
    }

    private void OnTakeRole(Entity<XenoParasiteComponent> parasite, ref MindAddedMessage args)
    {
        var transformComp = Transform(parasite.Owner);
        var parentEnt = transformComp.ParentUid;

        // Only carriers can have parasites in them, currently
        if (_container.ContainsEntity(parentEnt, parasite.Owner))
        {
            _transform.DropNextTo(parasite.Owner, parentEnt);
        }
    }

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
}
