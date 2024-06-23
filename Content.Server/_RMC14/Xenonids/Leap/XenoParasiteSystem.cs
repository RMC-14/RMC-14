using Content.Server.GameTicking;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Coordinates;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.Leap;

public sealed class XenoParasiteSystem : SharedXenoParasiteSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void ParasiteLeapHit(Entity<XenoParasiteComponent> parasite)
    {
        if (!TryComp(parasite, out ActorComponent? actor))
            return;

        var session = actor.PlayerSession;
        _gameTicker.SpawnObserver(session);
        if (session.AttachedEntity is not { } entity)
            return;

        _transform.SetCoordinates(entity, parasite.Owner.ToCoordinates());
        _transform.AttachToGridOrMap(entity);
    }
}
