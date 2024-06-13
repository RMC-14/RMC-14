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
    [Dependency] private readonly ContentEyeSystem _contentEye = default!;
    [Dependency] private readonly EyeSystem _eye = default!;

    protected override void StartScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
        var xform = Transform(user);
        if (!HasComp<CameraRecoilComponent>(user))
            return;

        const float smallestViewpointSize = 15;

        var cardinalVector = xform.LocalRotation.GetCardinalDir().ToVec();
        var targetOffset = cardinalVector * ((smallestViewpointSize * scopeComponent.Zoom - 1) / 2);
        _eye.SetOffset(user, targetOffset);

        var scopeToggleEvent = new CMScopeToggleEvent(GetNetEntity(user), targetOffset);
        RaiseNetworkEvent(scopeToggleEvent, user);

        _contentEye.SetZoom(user, Vector2.One * scopeComponent.Zoom, true);
        if (TryComp<ActorComponent>(user, out var actorComp))
        {
            // add cardinal vector, until better pvs handling is introduced here
            var loaderId = Spawn("CMScopingChunkLoader", _transformSystem.GetMapCoordinates(xform).Offset(targetOffset + cardinalVector * 2));
            scopeComponent.PvsLoader = loaderId;
            _viewSubscriber.AddViewSubscriber(loaderId, actorComp.PlayerSession);
        }
    }

    protected override void StopScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
        if (!HasComp<CameraRecoilComponent>(user))
            return;

        _eye.SetOffset(user, Vector2.Zero);
        _contentEye.ResetZoom(user);

        Del(scopeComponent.PvsLoader);
        scopeComponent.PvsLoader = null;
    }
}
