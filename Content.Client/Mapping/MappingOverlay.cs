using System.Numerics;
using Content.Shared.Coordinates.Helpers;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Content.Client.Mapping.MappingState;

namespace Content.Client.Mapping;

public sealed class MappingOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly SpriteSystem _sprite;

    // 1 off in case something else uses these colors since we use them to compare
    private static readonly Color PickColor = new(1, 255, 0);
    private static readonly Color DeleteColor = new(255, 1, 0);

    private readonly Dictionary<EntityUid, Color> _oldColors = new();

    private readonly MappingState _state;
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    // RMC14
    [Dependency] private readonly IEyeManager _eye = default!;
    // RMC14

    public MappingOverlay(MappingState state)
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entities.System<SpriteSystem>();

        _state = state;
        _shader = _prototypes.Index(UnshadedShader).Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var (id, color) in _oldColors)
        {
            if (!_entities.TryGetComponent(id, out SpriteComponent? sprite))
                continue;

            if (sprite.Color == DeleteColor || sprite.Color == PickColor)
                _sprite.SetColor((id, sprite), color);
        }

        _oldColors.Clear();

        if (_player.LocalEntity == null)
            return;

        var handle = args.WorldHandle;
        handle.UseShader(_shader);

        switch (_state.State)
        {
            case CursorState.Pick:
            {
                if (_state.GetHoveredEntity() is { } entity &&
                    _entities.TryGetComponent(entity, out SpriteComponent? sprite))
                {
                    _oldColors[entity] = sprite.Color;
                    _sprite.SetColor((entity, sprite), PickColor);
                }

                break;
            }
            case CursorState.Delete:
            {
                if (_state.GetHoveredEntity() is { } entity &&
                    _entities.TryGetComponent(entity, out SpriteComponent? sprite))
                {
                    _oldColors[entity] = sprite.Color;
                    _sprite.SetColor((entity, sprite), DeleteColor);
                }

                break;
            }
            // RMC14
            case CursorState.Grab:
                if (_state.GetGrabBox() is not { } box)
                    break;

                if (_state.DraggingGrab is { } dragging &&
                    _state.GetCursorPosition() is { } cursor)
                {
                    cursor = new MapCoordinates(_state.VectorSnapToGrid(cursor.MapId, cursor.Position, Vector2.Zero), cursor.MapId);
                    box = box.Translated(cursor.Position - dragging);
                    var bottomLeft = _state.VectorSnapToGrid(cursor.MapId, box.BottomLeft, -Vector2Helpers.Half);
                    var topRight = _state.VectorSnapToGrid(cursor.MapId, box.TopRight, -Vector2Helpers.Half);
                    box = new Box2(bottomLeft, topRight);
                }

                // TODO RMC14 rotation
                handle.DrawRect(box, Color.White.WithAlpha(0.05f));
                handle.DrawRect(box, Color.Lime, false);

                _state.SetGrabSizeLabel($"W:{box.Width} H:{box.Height}");
                break;
            // RMC14
        }

        handle.UseShader(null);
    }
}
