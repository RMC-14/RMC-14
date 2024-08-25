using System.Numerics;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Mobs;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Marines;

public sealed class MarineOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly MarineSystem _marine;
    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;

    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public MarineOverlay()
    {
        IoCManager.InjectDependencies(this);

        _marine = _entity.System<MarineSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<TransformSystem>();

        _shader = _prototype.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.HasComponent<CMGhostMarineHudComponent>(_players.LocalEntity) &&
            !_entity.HasComponent<MarineComponent>(_players.LocalEntity))
        {
            return;
        }

        var handle = args.WorldHandle;

        var eyeRot = args.Viewport.Eye?.Rotation ?? default;

        var xformQuery = _entity.GetEntityQuery<TransformComponent>();
        var scaleMatrix = Matrix3x2.CreateScale(new Vector2(1, 1));
        var rotationMatrix = Matrix3Helpers.CreateRotation(-eyeRot);

        handle.UseShader(_shader);

        var query = _entity.AllEntityQueryEnumerator<MarineComponent, StatusIconComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var status, out var sprite, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            var bounds = status.Bounds ?? sprite.Bounds;

            var worldPos = _transform.GetWorldPosition(xform, xformQuery);

            if (!bounds.Translated(worldPos).Intersects(args.WorldAABB))
                continue;

            var icon = _marine.GetMarineIcon(uid);
            if (icon.Icon == null)
                continue;

            var worldMatrix = Matrix3x2.CreateTranslation(worldPos);
            var scaledWorld = Matrix3x2.Multiply(scaleMatrix, worldMatrix);
            var matrix = Matrix3x2.Multiply(rotationMatrix, scaledWorld);
            handle.SetTransform(matrix);

            var texture = _sprite.Frame0(icon.Icon);

            var yOffset = 0.1f + (bounds.Height + sprite.Offset.Y) / 2f - (float) texture.Height / EyeManager.PixelsPerMeter;
            var xOffset = 0.1f + (bounds.Width + sprite.Offset.X) / 2f - (float) texture.Width / EyeManager.PixelsPerMeter;

            var position = new Vector2(xOffset, yOffset);
            if (icon.Icon != null && icon.Background != null)
            {
                var background = _sprite.Frame0(icon.Background);
                handle.DrawTexture(background, position, icon.BackgroundColor);
            }

            handle.DrawTexture(texture, position);
        }

        handle.UseShader(null);
    }
}
