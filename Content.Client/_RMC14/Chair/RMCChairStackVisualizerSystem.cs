using System.Numerics;
using Content.Shared._RMC14.Chair;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._RMC14.Chair;

public sealed class RMCChairStackVisualizerSystem : EntitySystem
{
    private const float Pixel = 1f / 32f;
    private const string LayerPrefix = "rmc-chair-stack-";
    private static readonly ResPath ChairRsi = new("_RMC14/Structures/Furniture/folding_chair.rsi");

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, int> _visualizedCounts = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCChairStackComponent, AppearanceChangeEvent>(OnAppearanceChange,
            after: [typeof(GenericVisualizerSystem)]);
        SubscribeLocalEvent<RMCChairStackComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<RMCChairStackComponent, ComponentRemove>(OnRemove);
    }

    private void OnAppearanceChange(Entity<RMCChairStackComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateVisuals((ent.Owner, args.Sprite), args.Component);
    }

    private void OnMove(Entity<RMCChairStackComponent> ent, ref MoveEvent args)
    {
        if (args.NewRotation == args.OldRotation ||
            !TryComp(ent, out SpriteComponent? sprite) ||
            !TryComp(ent, out AppearanceComponent? appearance))
        {
            return;
        }

        UpdateVisuals((ent.Owner, sprite), appearance);
    }

    private void OnRemove(Entity<RMCChairStackComponent> ent, ref ComponentRemove args)
    {
        _visualizedCounts.Remove(ent);
    }

    private void UpdateVisuals(Entity<SpriteComponent> ent, AppearanceComponent appearance)
    {
        _appearance.TryGetData<int>(ent, RMCChairStackVisuals.Count, out var count, appearance);
        var sprite = ent.AsNullable();

        var previousCount = _visualizedCounts.GetValueOrDefault(ent);
        for (var i = previousCount; i > 0; i--)
        {
            var key = GetLayerKey(i);
            if (_sprite.LayerMapTryGet(sprite, key, out _, false))
                _sprite.RemoveLayer(sprite, key);
        }

        _visualizedCounts[ent] = count;
        _sprite.SetDrawDepth(sprite, count > 0 ? (int) DrawDepth.OverMobs : (int) DrawDepth.Objects);

        if (count == 0)
            return;

        var direction = _transform.GetWorldRotation(ent).GetCardinalDir();
        var offset = Vector2.Zero;
        var step = direction switch
        {
            Direction.East => new Vector2(Pixel, 3 * Pixel),
            Direction.West => new Vector2(-Pixel, 3 * Pixel),
            _ => new Vector2(0, 2 * Pixel),
        };

        var netId = GetNetEntity(ent).Id;
        for (var i = 1; i <= count; i++)
        {
            offset += step;
            if (count > 8)
            {
                var wobble = ((netId + (uint) i * 1103515245U) & 1) == 0 ? -Pixel : Pixel;
                offset.X += wobble;
            }

            var layer = _sprite.AddLayer(sprite, new SpriteSpecifier.Rsi(ChairRsi, "chair"));
            _sprite.LayerMapSet(sprite, GetLayerKey(i), layer);
            _sprite.LayerSetOffset(sprite, layer, offset);
        }
    }

    private static string GetLayerKey(int index)
    {
        return $"{LayerPrefix}{index}";
    }
}
