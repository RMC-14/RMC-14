using Content.Client.Atmos.Components;
using Content.Shared._RMC14.Atmos; // RMC14
using Content.Shared.Atmos;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// This handles the display of fire effects on flammable entities.
/// </summary>
public sealed class FireVisualizerSystem : VisualizerSystem<FireVisualsComponent>
{
    [Dependency] private readonly PointLightSystem _lights = default!;

    // RMC14 start
    private EntityQuery<RMCFireColorComponent> _fireColorQuery;
    // RMC14 end

    public override void Initialize()
    {
        base.Initialize();

        // RMC14 start
        _fireColorQuery = GetEntityQuery<RMCFireColorComponent>();
        // RMC14 end

        SubscribeLocalEvent<FireVisualsComponent, ComponentInit>(OnComponentInit);
        // RMC14 start
        SubscribeLocalEvent<FireVisualsComponent, ComponentStartup>(OnComponentStartup);
        // RMC14 end
        SubscribeLocalEvent<FireVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(EntityUid uid, FireVisualsComponent component, ComponentShutdown args)
    {
        if (component.LightEntity != null)
        {
            Del(component.LightEntity.Value);
            component.LightEntity = null;
        }

        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            SpriteSystem.LayerMapTryGet((uid, sprite), FireVisualLayers.Fire, out var layer, false))
        {
            SpriteSystem.RemoveLayer((uid, sprite), layer);
        }
    }

    private void OnComponentInit(EntityUid uid, FireVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        SpriteSystem.LayerMapReserve((uid, sprite), FireVisualLayers.Fire);
        SpriteSystem.LayerSetVisible((uid, sprite), FireVisualLayers.Fire, false);
        sprite.LayerSetShader(FireVisualLayers.Fire, "unshaded");
        if (component.Sprite != null)
            SpriteSystem.LayerSetRsi((uid, sprite), FireVisualLayers.Fire, new ResPath(component.Sprite));
    }

    // RMC14 start
    // Delay the initial update until startup so client-side light children are not attached to an initializing parent.
    private void OnComponentStartup(EntityUid uid, FireVisualsComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        UpdateAppearance(uid, component, sprite, appearance);
    }
    // RMC14 end

    protected override void OnAppearanceChange(EntityUid uid, FireVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateAppearance(uid, component, args.Sprite, args.Component);
    }

    private void UpdateAppearance(EntityUid uid, FireVisualsComponent component, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!SpriteSystem.LayerMapTryGet((uid, sprite), FireVisualLayers.Fire, out var index, false))
            return;

        AppearanceSystem.TryGetData<bool>(uid, FireVisuals.OnFire, out var onFire, appearance);
        AppearanceSystem.TryGetData<float>(uid, FireVisuals.FireStacks, out var fireStacks, appearance);
        SpriteSystem.LayerSetVisible((uid, sprite), index, onFire);

        if (!onFire)
        {
            if (component.LightEntity != null)
            {
                Del(component.LightEntity.Value);
                component.LightEntity = null;
            }

            return;
        }

        if (fireStacks > component.FireStackAlternateState && !string.IsNullOrEmpty(component.AlternateState))
            SpriteSystem.LayerSetRsiState((uid, sprite), index, component.AlternateState);
        else
            SpriteSystem.LayerSetRsiState((uid, sprite), index, component.NormalState);

        // RMC14 start
        var fireColor = component.LightColor;
        if (_fireColorQuery.TryComp(uid, out var fireColorComp))
        {
            fireColor = fireColorComp.Color;
            SpriteSystem.LayerSetColor((uid, sprite), index, fireColor);
        }
        // RMC14 end

        // RMC14 start
        if (!MetaData(uid).EntityInitialized)
            return;
        // RMC14 end

        component.LightEntity ??= Spawn(null, new EntityCoordinates(uid, default));
        var light = EnsureComp<PointLightComponent>(component.LightEntity.Value);

        // RMC14 start
        _lights.SetColor(component.LightEntity.Value, fireColor, light);
        // RMC14 end

        // light needs a minimum radius to be visible at all, hence the + 1.5f
        _lights.SetRadius(component.LightEntity.Value, Math.Clamp(1.5f + component.LightRadiusPerStack * fireStacks, 0f, component.MaxLightRadius), light);
        _lights.SetEnergy(component.LightEntity.Value, Math.Clamp(1 + component.LightEnergyPerStack * fireStacks, 0f, component.MaxLightEnergy), light);

        // TODO flickering animation? Or just add a noise mask to the light? But that requires an engine PR.
    }
}

public enum FireVisualLayers : byte
{
    Fire
}
