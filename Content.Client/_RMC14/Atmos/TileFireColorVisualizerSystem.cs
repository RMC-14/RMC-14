using Content.Client.Atmos.Components;
using Content.Client.Atmos.EntitySystems;
using Content.Shared._RMC14.Atmos;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Atmos;

public sealed class TileFireColorVisualizerSystem : VisualizerSystem<TileFireComponent>
{
    [Dependency] private readonly FireVisualizerSystem _fireVisualizer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCFireColorComponent, AfterAutoHandleStateEvent>(OnFireColorState);
    }

    private void OnFireColorState(Entity<RMCFireColorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var uid = ent.Owner;
        if (HasComp<TileFireComponent>(uid))
        {
            ApplyColor(uid, ent.Comp.Color);
            return;
        }

        if (TryComp<FireVisualsComponent>(uid, out var fireVisuals) &&
            TryComp<SpriteComponent>(uid, out var sprite) &&
            TryComp(uid, out AppearanceComponent? appearance))
        {
            _fireVisualizer.UpdateAppearance(uid, fireVisuals, sprite, appearance);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, TileFireComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<RMCFireColorComponent>(uid, out var fireColor))
            return;

        ApplyColor(uid, fireColor.Color, args.Sprite);
    }

    private void ApplyColor(EntityUid uid, Color color, SpriteComponent? sprite = null)
    {
        if (sprite == null && !TryComp(uid, out sprite))
            return;

        if (!SpriteSystem.LayerMapTryGet((uid, sprite), "base", out var index, false))
            return;

        SpriteSystem.LayerSetColor((uid, sprite), index, color);
    }
}
