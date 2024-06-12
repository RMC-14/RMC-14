using System.Numerics;
using Content.Server.Movement.Systems;
using Content.Shared._CM14.Scoping;
using Content.Shared.Camera;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._CM14.Scoping;

public sealed class ScopeSystem : SharedScopeSystem
{
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ContentEyeSystem _eye = default!;

    protected override void StartScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
        var xform = Transform(user);
        if (TryComp<CameraRecoilComponent>(user, out var cameraRecoilComponent))
        {
            const float smallestViewpointSize = 15;

            var cardinalVector = xform.LocalRotation.GetCardinalDir().ToVec();
            var targetOffset = cardinalVector * ((smallestViewpointSize * scopeComponent.Zoom - 1) / 2);
            cameraRecoilComponent.BaseOffset = targetOffset;
            Dirty(user, cameraRecoilComponent);

            _eye.SetMaxZoom(user, Vector2.One * scopeComponent.Zoom);
            _eye.SetZoom(user, Vector2.One * scopeComponent.Zoom);
            if (TryComp<ActorComponent>(user, out var actorComp))
            {
                // add cardinal vector, until better pvs handling is introduced here
                var loaderId = Spawn("CMScopingChunkLoader", _transformSystem.GetMapCoordinates(xform).Offset(targetOffset + cardinalVector * 2));
                scopeComponent.PvsLoader = loaderId;
                _viewSubscriber.AddViewSubscriber(loaderId, actorComp.PlayerSession);
            }
        }
    }

    protected override void StopScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
        if (TryComp<CameraRecoilComponent>(user, out var cameraRecoilComponent))
        {
            _eye.ResetZoom(user);
            cameraRecoilComponent.BaseOffset = Vector2.Zero;
            Dirty(user, cameraRecoilComponent);

            Del(scopeComponent.PvsLoader);
            scopeComponent.PvsLoader = null;
        }
    }
}
