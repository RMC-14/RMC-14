using Content.Client._RMC14.Ghost;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.GhostColor;
using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.GhostColor;

public sealed class GhostColorSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private float _bodyAlpha;
    private float _clothingAlphaBoost;

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostColorComponent, AfterAutoHandleStateEvent>(OnGhostColorState);
        SubscribeLocalEvent<GhostColorComponent, ComponentShutdown>(OnGhostColorShutdown);
        SubscribeLocalEvent<GhostHumanoidAppearanceComponent, GhostAppearanceLayersRefreshedEvent>(OnLayersRefreshed);

        Subs.CVar(_config, RMCCVars.RMCGhostBodyAlpha, v => _bodyAlpha = v, true);
        Subs.CVar(_config, RMCCVars.RMCGhostClothingAlphaBoost, v => _clothingAlphaBoost = v, true);
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

    private void OnLayersRefreshed(Entity<GhostHumanoidAppearanceComponent> ent, ref GhostAppearanceLayersRefreshedEvent _)
    {
        ApplyColor(ent);
    }

    public void ApplyColor(EntityUid uid, Color? colorOverride = null)
    {
        if (!TryComp(uid, out GhostComponent? ghost) || !TryComp(uid, out SpriteComponent? sprite))
            return;

        Entity<SpriteComponent?> spriteEnt = (uid, sprite);
        var ghostColor = colorOverride ?? CompOrNull<GhostColorComponent>(uid)?.Color ?? ghost.Color;

        if (!HasComp<GhostHumanoidAppearanceComponent>(uid))
        {
            _sprite.SetColor(spriteEnt, ghostColor.WithAlpha(_bodyAlpha));
            return;
        }

        _sprite.SetColor(spriteEnt, ghostColor.WithAlpha(1f));

        var clothingAlpha = Math.Clamp(_bodyAlpha + _clothingAlphaBoost, 0f, 1f);

        var index = 0;
        foreach (var layer in sprite.AllLayers)
        {
            _sprite.LayerSetColor(spriteEnt, index, layer.Color.WithAlpha(_bodyAlpha));
            index++;
        }

        if (TryComp(uid, out GhostHumanoidAppearanceVisualsComponent? ghostVisuals))
        {
            foreach (var key in ghostVisuals.BoostedLayers)
            {
                if (!_sprite.LayerMapTryGet(spriteEnt, key, out var layerIndex, false))
                    continue;

                _sprite.LayerSetColor(spriteEnt, layerIndex, sprite[layerIndex].Color.WithAlpha(clothingAlpha));
            }
        }
    }
}
