using Content.Client.IconSmoothing;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Client._CM14.IconSmoothing;

public sealed class IconSmoothRandomSystem : EntitySystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;

    private EntityQuery<RandomSpriteComponent> _randomSpriteQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();

        _randomSpriteQuery = GetEntityQuery<RandomSpriteComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();

        SubscribeLocalEvent<IconSmoothRandomComponent, IconSmoothingUpdatedEvent>(OnOverrideIconSmoothingUpdated);
    }

    private void OnOverrideIconSmoothingUpdated(Entity<IconSmoothRandomComponent> ent, ref IconSmoothingUpdatedEvent args)
    {
        if (!_randomSpriteQuery.TryGetComponent(ent, out var random) ||
            !_spriteQuery.TryGetComponent(ent, out var sprite))
            return;

        foreach (var layer in random.Selected)
        {
            int index;
            if (_reflection.TryParseEnumReference(layer.Key, out var @enum))
            {
                if (!sprite.LayerMapTryGet(@enum, out index, logError: true))
                    continue;
            }
            else if (!sprite.LayerMapTryGet(layer.Key, out index))
            {
                if (layer.Key is not { } strKey || !int.TryParse(strKey, out index))
                {
                    Log.Error($"Invalid key `{layer.Key}` for entity with random sprite {ToPrettyString(ent)}");
                    continue;
                }
            }

            var spriteName = sprite.LayerGetState(index).Name;
            if (spriteName != null && ent.Comp.Overrides.Contains(spriteName))
            {
                sprite.LayerSetState(index, layer.Value.State);
                sprite.LayerSetColor(index, layer.Value.Color ?? Color.White);
            }
        }
    }
}
