using Content.Shared._RMC14.Scoping;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Scoping;

public sealed class ScopeSystem : SharedScopeSystem
{
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    protected override Direction? StartScoping(Entity<ScopeComponent> scope, EntityUid user)
    {
        if (base.StartScoping(scope, user) is not { } direction)
            return null;

        scope.Comp.User = user;

        if (TryComp(user, out ActorComponent? actor))
        {
            var coords = Transform(user).Coordinates;
            var offset = GetScopeOffset(scope, direction);
            scope.Comp.RelayEntity = SpawnAtPosition(null, coords.Offset(offset));
            _viewSubscriber.AddViewSubscriber(scope.Comp.RelayEntity.Value, actor.PlayerSession);
        }

        return direction;
    }

    protected override bool Unscope(Entity<ScopeComponent> scope)
    {
        var user = scope.Comp.User;
        if (!base.Unscope(scope))
            return false;

        DeleteRelay(scope, user);
        return true;
    }

    protected override void DeleteRelay(Entity<ScopeComponent> scope, EntityUid? user)
    {
        if (scope.Comp.RelayEntity is not { } relay)
            return;

        scope.Comp.RelayEntity = null;

        if (TryComp(user, out ActorComponent? actor))
            _viewSubscriber.RemoveViewSubscriber(relay, actor.PlayerSession);

        if (!TerminatingOrDeleted(relay))
            QueueDel(relay);
    }
}
