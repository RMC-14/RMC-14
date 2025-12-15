using System;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;

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

        if (AppearanceSystem.TryGetData(uid, RMCVehicleWheelVisuals.WheelCount, out int count))
            hasWheels = count > 0;
        else if (AppearanceSystem.TryGetData(uid, RMCVehicleWheelVisuals.HasAllWheels, out bool present))
            hasWheels = present;

        if (!hasWheels)
        {
            _sprite.LayerSetVisible((uid, sprite), layer, false);
            return;
        }

        var isMoving = false;
        if (TryComp<GridVehicleMoverComponent>(uid, out var mover))
            isMoving = Math.Abs(mover.CurrentSpeed) > 0.01f;

        var destroyed = false;
        var opacity = 1f;

        if (TryComp<RMCHardpointIntegrityComponent>(uid, out var integrity))
        {
            var max = integrity.MaxIntegrity > 0f ? integrity.MaxIntegrity : 1f;
            opacity = integrity.Integrity / max;

            if (opacity < 0.15f)
                opacity = 0.15f;
            else if (opacity > 1f)
                opacity = 1f;

            destroyed = integrity.Integrity <= 0f;
        }

        var targetState = destroyed ? "wheels_1" : "wheels_0";

        if (sprite.LayerGetState(layer) != targetState)
        {
            sprite.LayerSetState(layer, targetState);

            if (isMoving && !destroyed)
                _sprite.LayerSetAnimationTime((uid, sprite), layer, 0f);
        }

        _sprite.LayerSetAutoAnimated((uid, sprite), layer, isMoving && !destroyed);
        _sprite.LayerSetColor((uid, sprite), layer, sprite.Color.WithAlpha(opacity));
        _sprite.LayerSetVisible((uid, sprite), layer, true);
    }
}
