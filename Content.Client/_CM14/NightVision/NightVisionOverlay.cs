using System.Numerics;
using Content.Shared._CM14.NightVision;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client._CM14.NightVision;

public sealed class NightVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    private readonly TransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public NightVisionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entity.System<TransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.TryGetComponent(_players.LocalEntity, out NightVisionComponent? nightVision) ||
            nightVision.State == NightVisionState.Off)
        {
            return;
        }

        var eye = args.Viewport.Eye;
        var eyeRot = eye?.Rotation ?? default;
        var zoom = Vector2.One / (args.Viewport.Eye?.Zoom ?? Vector2.One);

        var entities = _entity.EntityQueryEnumerator<MobStateComponent, SpriteComponent, TransformComponent>();
        while (entities.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            if (xform.MapID != eye?.Position.MapId)
                continue;

            var position = _eye.CoordinatesToScreen(xform.Coordinates).Position;
            if (!args.ViewportBounds.Contains((int) position.X, (int) position.Y))
                continue;

            var rotation = _transform.GetWorldRotation(xform);
            args.ScreenHandle.DrawEntity(uid, position, Vector2.One * 2 * zoom, rotation + eyeRot, Angle.Zero, null, sprite, xform, _transform);
        }
    }
}
