using Content.Server.GameTicking;
using Content.Shared._CM14.Xenos.Hugger;
using Content.Shared.Coordinates;
using Robust.Shared.Player;

namespace Content.Server._CM14.Xenos.Leap;

public sealed class XenoHuggerSystem : SharedXenoHuggerSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void HuggerLeapHit(Entity<XenoHuggerComponent> hugger)
    {
        if (!TryComp(hugger, out ActorComponent? actor))
            return;

        var session = actor.PlayerSession;
        _gameTicker.SpawnObserver(session);
        if (session.AttachedEntity is not { } entity)
            return;

        _transform.SetCoordinates(entity, hugger.Owner.ToCoordinates());
        _transform.AttachToGridOrMap(entity);
    }
}
