using Content.Client._RMC14.Ghost;
using Content.Shared._RMC14.GhostColor;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.GhostColor;

public sealed class GhostColorSystem : EntitySystem
{
    private const float BodyAlpha = 0x88 / 255f;
    private const float ClothingAlphaBoost = 0.2f;

    public override void Update(float frameTime)
    {
        var ghosts = EntityQueryEnumerator<GhostComponent, SpriteComponent>();
        while (ghosts.MoveNext(out var uid, out var ghost, out var sprite))
        {
            var ghostColor = CompOrNull<GhostColorComponent>(uid)?.Color ?? ghost.Color;

            if (!HasComp<HumanoidAppearanceComponent>(uid))
            {
                sprite.Color = ghostColor.WithAlpha(BodyAlpha);
                continue;
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
}
