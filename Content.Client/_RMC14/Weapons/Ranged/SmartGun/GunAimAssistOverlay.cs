using System.Numerics;
using Content.Shared._RMC14.Weapons.Ranged.SmartGun;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Weapons.Ranged.SmartGun;

public sealed class GunAimAssistOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SharedHandsSystem _hands;
    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public GunAimAssistOverlay()
    {
        IoCManager.InjectDependencies(this);
        _hands = _entity.System<SharedHandsSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        DrawMarkers(args.WorldHandle, args.MapId);
    }

    private void DrawMarkers(DrawingHandleWorld handle, MapId mapId)
    {
        if (_player.LocalEntity is not { } player)
            return;

        foreach (var held in _hands.EnumerateHeld(player))
        {
            if (!_entity.TryGetComponent(held, out GunAimAssistComponent? assist) || !assist.Active ||
                assist.Target is not { } target || _timing.CurTime >= assist.TargetExpires ||
                !_entity.TryGetComponent(held, out WieldableComponent? wieldable) || !wieldable.Wielded ||
                !_entity.TryGetComponent(target, out TransformComponent? xform))
            {
                continue;
            }

            if (mapId != xform.MapID)
                continue;

            var texture = _sprite.Frame0(assist.TargetIcon);
            var offset = new Vector2(texture.Width, texture.Height) / (2 * EyeManager.PixelsPerMeter);
            var previousTransform = handle.GetTransform();
            handle.SetTransform(Matrix3x2.Identity);
            try
            {
                handle.DrawTexture(texture, _transform.GetWorldPosition(xform) - offset, assist.TargetColor);
            }
            finally
            {
                handle.SetTransform(previousTransform);
            }
        }
    }
}
