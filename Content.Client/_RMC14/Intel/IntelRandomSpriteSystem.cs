using Content.Shared._RMC14.Intel;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client._RMC14.Intel;

public sealed class IntelRandomSpriteSystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _resCache = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IntelRandomSpriteComponent, GetInhandVisualsEvent>(OnGetInhandVisuals);
    }

    private void OnGetInhandVisuals(EntityUid uid, IntelRandomSpriteComponent component, GetInhandVisualsEvent args)
    {
        if (!TryComp<RandomSpriteComponent>(uid, out var randomSprite))
            return;

        if (!randomSprite.Selected.TryGetValue("base", out var baseState))
        {
            // if randomSprite hasn't initialized yet, use fallback
            ProvideFallbackInhandVisual(uid, args);
            return;
        }

        var variant = baseState.State;

        if (!TryComp<ItemComponent>(uid, out var item))
            return;

        if (item.RsiPath == null)
            return;

        var rsiPath = SpriteSpecifierSerializer.TextureRoot / item.RsiPath;
        var rsi = _resCache.GetResource<RSIResource>(rsiPath).RSI;

        if (rsi == null)
            return;

        var inhandState = $"{variant}-inhand-{args.Location.ToString().ToLowerInvariant()}";

        if (!rsi.TryGetState(inhandState, out var _))
        {
            ProvideFallbackInhandVisual(uid, args);
            return;
        }

        var layer = new PrototypeLayerData
        {
            RsiPath = rsi.Path.ToString(),
            State = inhandState,
            MapKeys = new() { inhandState }
        };

        args.Layers.Add((inhandState, layer));
    }

    private void ProvideFallbackInhandVisual(EntityUid uid, GetInhandVisualsEvent args)
    {
        if (!TryComp<ItemComponent>(uid, out var item))
            return;

        if (item.RsiPath == null)
            return;

        var rsiPath = SpriteSpecifierSerializer.TextureRoot / item.RsiPath;
        var rsi = _resCache.GetResource<RSIResource>(rsiPath).RSI;

        if (rsi == null)
            return;

        var fallbackState = $"folder-inhand-{args.Location.ToString().ToLowerInvariant()}";

        if (!rsi.TryGetState(fallbackState, out var _))
            return;

        var layer = new PrototypeLayerData
        {
            RsiPath = rsi.Path.ToString(),
            State = fallbackState,
            MapKeys = new() { fallbackState }
        };

        args.Layers.Add((fallbackState, layer));
    }
}
