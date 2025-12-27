using System;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Vehicle;

public sealed class RMCVehicleWheelVisualizerSystem : VisualizerSystem<RMCVehicleWheelSlotsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnAppearanceChange(EntityUid uid, RMCVehicleWheelSlotsComponent component, ref AppearanceChangeEvent args)
    {
        UpdateWheelVisuals(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<RMCVehicleWheelSlotsComponent, SpriteComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var sprite))
        {
            UpdateWheelVisuals(uid, sprite);
        }
    }

    private void UpdateWheelVisuals(EntityUid uid, SpriteComponent? sprite = null)
    {
        if (sprite == null && !TryComp(uid, out sprite))
            return;

        if (!_sprite.LayerMapTryGet((uid, sprite), RMCVehicleWheelLayers.Wheels, out var layer, true))
            return;

        var hasWheels = true;

        var installed = 0;
        if (AppearanceSystem.TryGetData(uid, RMCVehicleWheelVisuals.WheelCount, out int count))
            installed = count;

        if (AppearanceSystem.TryGetData(uid, RMCVehicleWheelVisuals.HasAllWheels, out bool present) && installed == 0)
            installed = present ? 1 : 0;

        hasWheels = installed > 0;

        if (!hasWheels)
        {
            _sprite.LayerSetVisible((uid, sprite), layer, false);
            return;
        }

        var isMoving = false;
        if (TryComp<GridVehicleMoverComponent>(uid, out var mover))
            isMoving = Math.Abs(mover.CurrentSpeed) > 0.01f;

        var destroyed = false;
        var brightness = 1f;
        var functional = installed;
        var averageIntegrity = 1f;

        if (AppearanceSystem.TryGetData(uid, RMCVehicleWheelVisuals.WheelFunctionalCount, out int functionalCount))
            functional = functionalCount;

        if (AppearanceSystem.TryGetData(uid, RMCVehicleWheelVisuals.WheelIntegrityFraction, out float integrityFraction))
            averageIntegrity = integrityFraction;

        var fraction = installed > 0 ? (float) functional / installed : 1f;
        if (fraction < 0f)
            fraction = 0f;
        else if (fraction > 1f)
            fraction = 1f;

        // Thoght each wheels was its own thing
        var integrityBrightness = 0.3f + 0.7f * averageIntegrity;
        var functionalBrightness = 0.3f + 0.7f * fraction;
        brightness = MathF.Min(integrityBrightness, functionalBrightness);
        destroyed = installed > 0 && functional <= 0;

        if (destroyed)
            brightness = 1f;

        var targetState = destroyed ? "wheels_1" : "wheels_0";

        if (sprite.LayerGetState(layer) != targetState)
        {
            sprite.LayerSetState(layer, targetState);

            if (isMoving && !destroyed)
                _sprite.LayerSetAnimationTime((uid, sprite), layer, 0f);
        }

        _sprite.LayerSetAutoAnimated((uid, sprite), layer, isMoving && !destroyed);
        _sprite.LayerSetColor((uid, sprite), layer, new Color(brightness, brightness, brightness, sprite.Color.A));
        _sprite.LayerSetVisible((uid, sprite), layer, true);
    }
}
