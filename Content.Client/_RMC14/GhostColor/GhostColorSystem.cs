using Content.Client._RMC14.Ghost;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.GhostColor;
using Content.Shared.Ghost;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.GhostColor;

public sealed class GhostColorSystem : EntitySystem
{
    private const float BodyAlpha = 0x88 / 255f;
    private const float ClothingAlphaBoost = 0.4f;

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostColorComponent, AfterAutoHandleStateEvent>(OnGhostColorState);
        SubscribeLocalEvent<GhostColorComponent, ComponentShutdown>(OnGhostColorShutdown);
        SubscribeLocalEvent<GhostHumanoidAppearanceComponent, GhostHumanoidLayersRefreshedEvent>(OnLayersRefreshed);
    }

    private void OnGhostColorState(Entity<GhostColorComponent> ent, ref AfterAutoHandleStateEvent _)
    {
        ApplyColor(ent);
    }

    private void OnGhostColorShutdown(Entity<GhostColorComponent> ent, ref ComponentShutdown _)
    {
        if (TryComp(ent, out GhostComponent? ghost))
            ApplyColor(ent, ghost.Color);
    }

    private void OnLayersRefreshed(Entity<GhostHumanoidAppearanceComponent> ent, ref GhostHumanoidLayersRefreshedEvent _)
    {
        ApplyColor(ent);
    }

    private void ApplyColor(EntityUid uid, Color? colorOverride = null)
    {
        if (!TryComp(uid, out GhostComponent? ghost) || !TryComp(uid, out SpriteComponent? sprite))
            return;

        var ghostColor = colorOverride ?? CompOrNull<GhostColorComponent>(uid)?.Color ?? ghost.Color;

        if (!HasComp<GhostHumanoidAppearanceComponent>(uid))
        {
            sprite.Color = ghostColor.WithAlpha(BodyAlpha);
            return;
        }

        sprite.Color = ghostColor.WithAlpha(1f);

        var clothingAlpha = Math.Clamp(BodyAlpha + ClothingAlphaBoost, 0f, 1f);

        foreach (var layer in sprite.AllLayers)
        {
            layer.Color = layer.Color.WithAlpha(BodyAlpha);
        }

        if (TryComp(uid, out GhostHumanoidAppearanceVisualsComponent? ghostVisuals))
        {
            foreach (var key in ghostVisuals.BoostedLayers)
            {
                if (!sprite.LayerMapTryGet(key, out var index))
                    continue;

                sprite[index].Color = sprite[index].Color.WithAlpha(clothingAlpha);
            }
        }
    }
}
