using Content.Client.DisplacementMap;
using Content.Shared._RMC14.Ghost;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Ghost;

public sealed class GhostHumanoidAppearanceVisualizerSystem : EntitySystem
{
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

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
        ClearLayers((ent.Owner, sprite, visuals));

        for (var i = 0; i < ent.Comp.Layers.Count; i++)
        {
            var snapshot = ent.Comp.Layers[i];
            var renderKey = $"ghosthum-{i}-{snapshot.Key}";
            var index = _sprite.LayerMapReserve((ent.Owner, sprite), renderKey);
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
            }
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
