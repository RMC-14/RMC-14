using System.Numerics;
using Content.Server.Movement.Systems;
using Content.Shared._CM14.Scoping;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._CM14.Scoping;

public sealed class ScopeSystem : SharedScopeSystem
{
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ContentEyeSystem _contentEye = default!;
    [Dependency] private readonly EyeSystem _eye = default!;

    protected override void StartScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
        var xform = Transform(user);

        const float smallestViewpointSize = 15;

        var cardinalVector = xform.LocalRotation.GetCardinalDir().ToVec();
        var targetOffset = cardinalVector * ((smallestViewpointSize * scopeComponent.Zoom - 1) / 2);

        _contentEye.SetZoom(user, Vector2.One * scopeComponent.Zoom, true);

        if (TryComp(user, out ActorComponent? actorComp))
        {
            // add cardinal vector, until better pvs handling is introduced here
            var loaderId = Spawn(scopeComponent.PvsLoaderProto, _transformSystem.GetMapCoordinates(xform).Offset(targetOffset));
            scopeComponent.PvsLoader = loaderId;
            _viewSubscriber.AddViewSubscriber(loaderId, actorComp.PlayerSession);
            _eye.SetTarget(user, loaderId);
        }
    }

    protected override void StopScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
        _contentEye.ResetZoom(user);
        _eye.SetTarget(user, null);

        Del(scopeComponent.PvsLoader);
        scopeComponent.PvsLoader = null;
    }
}
