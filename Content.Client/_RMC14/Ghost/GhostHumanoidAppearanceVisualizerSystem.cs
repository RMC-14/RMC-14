using System.Linq;
using Content.Client.DisplacementMap;
using Content.Client.Humanoid;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.Webbing;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Ghost;

public sealed class GhostHumanoidAppearanceVisualizerSystem : EntitySystem
{
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly object[] TemplateLayers =
    {
        HumanoidVisualLayers.Chest,
        HumanoidVisualLayers.Head,
        HumanoidVisualLayers.Snout,
        HumanoidVisualLayers.Eyes,
        HumanoidVisualLayers.RArm,
        HumanoidVisualLayers.LArm,
        HumanoidVisualLayers.RLeg,
        HumanoidVisualLayers.LLeg,
        HumanoidVisualLayers.UndergarmentBottom,
        HumanoidVisualLayers.UndergarmentTop,
        "jumpsuit",
        HumanoidVisualLayers.LFoot,
        HumanoidVisualLayers.RFoot,
        HumanoidVisualLayers.LHand,
        HumanoidVisualLayers.RHand,
        HumanoidVisualLayers.Handcuffs,
        "gloves",
        "shoes",
        "id",
        "ears",
        "eyes",
        "belt",
        "outerClothing",
        WebbingVisualLayers.Base,
        "back",
        HumanoidVisualLayers.FacialHair,
        "neck",
        HumanoidVisualLayers.Hair,
        HumanoidVisualLayers.HeadSide,
        HumanoidVisualLayers.HeadTop,
        HumanoidVisualLayers.Tail,
        "mask",
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostHumanoidAppearanceComponent, ComponentStartup>(OnAppearanceStartup);
        SubscribeLocalEvent<GhostHumanoidAppearanceComponent, AfterAutoHandleStateEvent>(OnAppearanceState);
        SubscribeLocalEvent<GhostHumanoidAppearanceComponent, ComponentShutdown>(OnAppearanceShutdown);
    }

    private void OnAppearanceStartup(Entity<GhostHumanoidAppearanceComponent> ent, ref ComponentStartup args)
    {
        RefreshLayers(ent);
    }

    private void OnAppearanceState(Entity<GhostHumanoidAppearanceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshLayers(ent);
    }

    private void RefreshLayers(Entity<GhostHumanoidAppearanceComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var visuals = EnsureComp<GhostHumanoidAppearanceVisualsComponent>(ent);
        Entity<SpriteComponent?> spriteEnt = (ent.Owner, sprite);
        EnsureTemplateLayers((ent.Owner, sprite));
        ClearLayers((ent.Owner, sprite, visuals));

        if (sprite.AllLayers.Any())
            sprite[0].Visible = false;

        _humanoid.RefreshAppearance(ent.Owner, ent.Comp.Appearance, sprite);

        var insertionIndices = new Dictionary<string, int>();
        for (var i = 0; i < ent.Comp.Layers.Count; i++)
        {
            var snapshot = ent.Comp.Layers[i];
            var renderKey = $"ghosthum-{i}-{snapshot.Key}";
            var index = ResolveLayerIndex((ent.Owner, sprite), snapshot, renderKey, insertionIndices);
            _sprite.LayerSetData((ent.Owner, sprite), index, snapshot.Layer);
            visuals.RenderedLayers.Add(renderKey);

            if (snapshot.BoostedAlpha)
                visuals.BoostedLayers.Add(renderKey);

            if (snapshot.Displacement != null &&
                _displacement.TryAddDisplacement(snapshot.Displacement, (ent.Owner, sprite), index, renderKey, out var displacementKey))
            {
                visuals.RenderedLayers.Add(displacementKey);
                if (snapshot.BoostedAlpha)
                    visuals.BoostedLayers.Add(displacementKey);

                if (snapshot.BookmarkKey != null &&
                    _sprite.LayerMapTryGet((ent.Owner, sprite), displacementKey, out var displacementIndex, false))
                {
                    insertionIndices[snapshot.BookmarkKey] = displacementIndex;
                }
            }
        }
    }

    private int ResolveLayerIndex(
        Entity<SpriteComponent> sprite,
        GhostHumanoidLayerSnapshot snapshot,
        string renderKey,
        Dictionary<string, int> insertionIndices)
    {
        Entity<SpriteComponent?> spriteEnt = (sprite.Owner, sprite.Comp);
        if (snapshot.BookmarkKey == null ||
            !_sprite.LayerMapTryGet(spriteEnt, snapshot.BookmarkKey, out var bookmarkIndex, false))
        {
            return _sprite.LayerMapReserve(spriteEnt, renderKey);
        }

        var index = insertionIndices.TryGetValue(snapshot.BookmarkKey, out var currentIndex)
            ? currentIndex + 1
            : bookmarkIndex + 1;

        _sprite.AddBlankLayer(sprite, index);
        _sprite.LayerMapRemove(spriteEnt, renderKey);
        _sprite.LayerMapSet(spriteEnt, renderKey, index);
        insertionIndices[snapshot.BookmarkKey] = index;
        return index;
    }

    private void EnsureTemplateLayers(Entity<SpriteComponent> ent)
    {
        Entity<SpriteComponent?> spriteEnt = (ent.Owner, ent.Comp);
        foreach (var key in TemplateLayers)
        {
            var index = key switch
            {
                Enum enumKey => _sprite.LayerMapReserve(spriteEnt, enumKey),
                string stringKey => _sprite.LayerMapReserve(spriteEnt, stringKey),
                _ => throw new ArgumentOutOfRangeException(nameof(key)),
            };

            ent.Comp[index].Visible = false;
        }
    }

    private void OnAppearanceShutdown(Entity<GhostHumanoidAppearanceComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite) ||
            !TryComp(ent, out GhostHumanoidAppearanceVisualsComponent? visuals))
        {
            return;
        }

        ClearLayers((ent.Owner, sprite, visuals));

        if (sprite.AllLayers.Any())
            sprite[0].Visible = true;

        RemCompDeferred<GhostHumanoidAppearanceVisualsComponent>(ent);
    }

    private void ClearLayers(Entity<SpriteComponent, GhostHumanoidAppearanceVisualsComponent> ent)
    {
        foreach (var key in ent.Comp2.RenderedLayers)
        {
            _sprite.RemoveLayer((ent.Owner, ent.Comp1), key, false);
        }

        ent.Comp2.RenderedLayers.Clear();
        ent.Comp2.BoostedLayers.Clear();
    }
}
