using System.Numerics;
using Content.Shared.CM14.Marines;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.CM14.Marines;

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

    private readonly List<SpriteSpecifier> _icons = new();

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
        if (!_entity.HasComponent<MarineComponent>(_players.LocalEntity))
        {
            return;
        }

        var handle = args.WorldHandle;

        var eyeRot = args.Viewport.Eye?.Rotation ?? default;

        var xformQuery = _entity.GetEntityQuery<TransformComponent>();
        var scaleMatrix = Matrix3.CreateScale(new Vector2(1, 1));
        var rotationMatrix = Matrix3.CreateRotation(-eyeRot);

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

            _icons.Clear();
            _marine.GetMarineIcons(uid, _icons);
            if (_icons.Count == 0)
                continue;

            var worldMatrix = Matrix3.CreateTranslation(worldPos);
            Matrix3.Multiply(scaleMatrix, worldMatrix, out var scaledWorld);
            Matrix3.Multiply(rotationMatrix, scaledWorld, out var matty);
            handle.SetTransform(matty);

            _icons.Sort();

            foreach (var proto in _icons)
            {
                var texture = _sprite.Frame0(proto);

                var yOffset = (bounds.Height + sprite.Offset.Y) / 2f;
                var xOffset = (bounds.Width + sprite.Offset.X) / 2f - (float) texture.Width / EyeManager.PixelsPerMeter;

                var position = new Vector2(xOffset, yOffset);
                handle.DrawTexture(texture, position);
            }
        }

        handle.UseShader(null);
    }
}
