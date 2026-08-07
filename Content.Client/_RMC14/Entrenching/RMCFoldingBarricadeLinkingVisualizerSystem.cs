using Content.Shared._RMC14.Entrenching;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using static Robust.Client.Graphics.RSI;

namespace Content.Client._RMC14.Entrenching;

public sealed class RMCFoldingBarricadeLinkingVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly (RMCFoldingBarricadeLinkingVisuals Visual,
        RMCFoldingBarricadeLinkingVisualLayers Layer,
        Direction Direction)[] ConnectionLayers =
    [
        (RMCFoldingBarricadeLinkingVisuals.North, RMCFoldingBarricadeLinkingVisualLayers.North, Direction.North),
        (RMCFoldingBarricadeLinkingVisuals.South, RMCFoldingBarricadeLinkingVisualLayers.South, Direction.South),
        (RMCFoldingBarricadeLinkingVisuals.East, RMCFoldingBarricadeLinkingVisualLayers.East, Direction.East),
        (RMCFoldingBarricadeLinkingVisuals.West, RMCFoldingBarricadeLinkingVisualLayers.West, Direction.West),
    ];

    private Direction _lastEyeDirection = Direction.Invalid;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCFoldingBarricadeLinkingComponent, AppearanceChangeEvent>(
            OnAppearanceChange,
            after: [typeof(GenericVisualizerSystem)]);
    }

    public override void FrameUpdate(float frameTime)
    {
        var eyeDirection = GetEyeDirection();
        if (eyeDirection == _lastEyeDirection)
            return;

        _lastEyeDirection = eyeDirection;

        var query = EntityQueryEnumerator<RMCFoldingBarricadeLinkingComponent, AppearanceComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var linking, out var appearance, out var sprite))
        {
            UpdateVisuals((uid, sprite), linking, appearance);
        }
    }

    private void OnAppearanceChange(Entity<RMCFoldingBarricadeLinkingComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateVisuals((ent.Owner, args.Sprite), ent.Comp, args.Component);
    }

    private void UpdateVisuals(
        Entity<SpriteComponent> ent,
        RMCFoldingBarricadeLinkingComponent linking,
        AppearanceComponent? appearance)
    {
        var eyeRotation = _eye.CurrentEye.Rotation;
        var sprite = ent.AsNullable();

        foreach (var (visual, layer, direction) in ConnectionLayers)
        {
            if (!_sprite.LayerMapTryGet(sprite, layer, out var layerIndex, false))
                continue;

            if (!_appearance.TryGetData<RMCFoldingBarricadeLinkingVisualState>(
                    ent.Owner,
                    visual,
                    out var state,
                    appearance) ||
                state == RMCFoldingBarricadeLinkingVisualState.None)
            {
                _sprite.LayerSetVisible(sprite, layerIndex, false);
                continue;
            }

            // Connection states are keyed by screen-facing side, while linking data is stored in world directions.
            var screenDirection = (direction.ToAngle() + eyeRotation).GetCardinalDir();
            if (!linking.TryGetConnectionState(screenDirection, state, out var stateId))
            {
                _sprite.LayerSetVisible(sprite, layerIndex, false);
                continue;
            }

            _sprite.LayerSetRsiState(sprite, layerIndex, new StateId(stateId));
            _sprite.LayerSetVisible(sprite, layerIndex, true);
        }
    }

    private Direction GetEyeDirection()
    {
        return _eye.CurrentEye.Rotation.GetCardinalDir();
    }
}
