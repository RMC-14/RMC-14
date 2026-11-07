using Content.Client._RMC14.GhostColor;
using Content.Client.Atmos.Components;
using Content.Client.Damage;
using Content.Shared._RMC14.Ghost;
using Content.Shared.FixedPoint;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Ghost;

public sealed class GhostOverlayVisualizerSystem : EntitySystem
{
    private const string FireLayerKey = "ghost-overlay-fire";
    private const string XenoWoundLayerKey = "ghost-overlay-xeno-wound";

    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly GhostColorSystem _ghostColor = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostOverlayAppearanceComponent, ComponentStartup>(OnOverlayStartup);
        SubscribeLocalEvent<GhostOverlayAppearanceComponent, AfterAutoHandleStateEvent>(OnOverlayState);
        SubscribeLocalEvent<GhostOverlayAppearanceComponent, ComponentShutdown>(OnOverlayShutdown);
        SubscribeLocalEvent<GhostOverlayAppearanceComponent, GhostAppearanceLayersRefreshedEvent>(OnLayersRefreshed);
    }

    private void OnOverlayStartup(Entity<GhostOverlayAppearanceComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent);
    }

    private void OnOverlayState(Entity<GhostOverlayAppearanceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Refresh(ent);
    }

    private void OnLayersRefreshed(Entity<GhostOverlayAppearanceComponent> ent, ref GhostAppearanceLayersRefreshedEvent args)
    {
        Refresh(ent);
    }

    private void OnOverlayShutdown(Entity<GhostOverlayAppearanceComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite) ||
            !TryComp(ent, out GhostOverlayVisualsComponent? visuals))
        {
            return;
        }

        ClearLayers((ent.Owner, sprite, visuals));
        RemCompDeferred<GhostOverlayVisualsComponent>(ent);
    }

    private void Refresh(Entity<GhostOverlayAppearanceComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var visuals = EnsureComp<GhostOverlayVisualsComponent>(ent);
        Entity<SpriteComponent, GhostOverlayVisualsComponent> overlayEnt = (ent.Owner, sprite, visuals);
        ClearLayers(overlayEnt);

        if (ent.Comp.XenoWound is { } xenoWound)
            AddXenoWoundLayer(overlayEnt, xenoWound);

        if (ent.Comp.SourcePrototype is { } protoId &&
            _prototype.TryIndex(protoId, out var prototype))
        {
            if (ent.Comp.Damage is { } damage)
                AddDamageLayers(overlayEnt, prototype, damage);

            if (ent.Comp.Fire is { } fire)
                AddFireLayer(overlayEnt, prototype, fire);
        }

        _ghostColor.ApplyColor(ent);
    }

    private void AddDamageLayers(
        Entity<SpriteComponent, GhostOverlayVisualsComponent> ent,
        EntityPrototype prototype,
        GhostDamageSnapshot damage)
    {
        if (!prototype.TryGetComponent(out DamageVisualsComponent? damageVisuals, _compFactory) ||
            !damageVisuals.Overlay ||
            damageVisuals.TargetLayers == null ||
            damageVisuals.DamageOverlayGroups == null)
        {
            return;
        }

        Entity<SpriteComponent?> spriteEnt = (ent.Owner, ent.Comp1);
        foreach (var targetLayer in damageVisuals.TargetLayers)
        {
            foreach (var (group, overlay) in damageVisuals.DamageOverlayGroups)
            {
                if (!damage.DamagePerGroup.TryGetValue(group, out var total) ||
                    GetDamageThreshold(damageVisuals, total) is not { } threshold ||
                    !_sprite.LayerMapTryGet(spriteEnt, targetLayer, out var targetIndex, false))
                {
                    continue;
                }

                var key = $"ghost-overlay-damage-{targetLayer}-{group}";
                var index = _sprite.AddLayer(
                    spriteEnt,
                    new SpriteSpecifier.Rsi(new ResPath(overlay.Sprite), $"{targetLayer}_{group}_{threshold}"),
                    targetIndex + 1);
                _sprite.LayerMapSet(spriteEnt, key, index);

                if (damage.GroupColors.TryGetValue(group, out var groupColor))
                    _sprite.LayerSetColor(spriteEnt, index, groupColor);
                else if (overlay.Color != null)
                    _sprite.LayerSetColor(spriteEnt, index, Color.FromHex(overlay.Color));

                ent.Comp2.Layers.Add(key);
            }
        }
    }

    private void AddXenoWoundLayer(Entity<SpriteComponent, GhostOverlayVisualsComponent> ent, GhostXenoWoundSnapshot wound)
    {
        Entity<SpriteComponent?> spriteEnt = (ent.Owner, ent.Comp1);
        if (!_sprite.LayerMapTryGet(spriteEnt, GhostNonHumanoidAppearanceVisualizerSystem.BaseLayerKey, out var baseIndex, false) ||
            ent.Comp1[baseIndex].ActualRsi is not { } rsi)
        {
            return;
        }

        var state = $"{wound.Prefix}_walk_{wound.States - wound.Level + 1}";
        var index = _sprite.AddLayer(spriteEnt, new SpriteSpecifier.Rsi(rsi.Path, state), baseIndex + 1);
        _sprite.LayerMapSet(spriteEnt, XenoWoundLayerKey, index);
        ent.Comp2.Layers.Add(XenoWoundLayerKey);
    }

    private void AddFireLayer(
        Entity<SpriteComponent, GhostOverlayVisualsComponent> ent,
        EntityPrototype prototype,
        GhostFireSnapshot fire)
    {
        if (!prototype.TryGetComponent(out FireVisualsComponent? fireVisuals, _compFactory) ||
            fireVisuals.Sprite == null)
        {
            return;
        }

        var state = fire.FireStacks > fireVisuals.FireStackAlternateState && !string.IsNullOrEmpty(fireVisuals.AlternateState)
            ? fireVisuals.AlternateState
            : fireVisuals.NormalState;

        if (state == null)
            return;

        Entity<SpriteComponent?> spriteEnt = (ent.Owner, ent.Comp1);
        var index = _sprite.LayerMapReserve(spriteEnt, FireLayerKey);
        _sprite.LayerSetData(spriteEnt, index, new PrototypeLayerData
        {
            RsiPath = fireVisuals.Sprite,
            State = state,
            Shader = "unshaded",
            Color = fire.Color,
            Visible = true,
        });
        ent.Comp2.Layers.Add(FireLayerKey);
    }

    private void ClearLayers(Entity<SpriteComponent, GhostOverlayVisualsComponent> ent)
    {
        foreach (var key in ent.Comp2.Layers)
        {
            _sprite.RemoveLayer((ent.Owner, ent.Comp1), key, false);
        }

        ent.Comp2.Layers.Clear();
    }

    private static FixedPoint2? GetDamageThreshold(DamageVisualsComponent damageVisuals, FixedPoint2 damage)
    {
        var scaled = damage / damageVisuals.Divisor;
        FixedPoint2? threshold = null;
        foreach (var candidate in damageVisuals.Thresholds)
        {
            if (candidate <= FixedPoint2.Zero || candidate > scaled)
                continue;

            if (threshold == null || candidate > threshold)
                threshold = candidate;
        }

        return threshold;
    }
}
