using System;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Vehicle;

public sealed class RMCVehicleWheelVisualizerSystem : VisualizerSystem<RMCVehicleWheelSlotsComponent>
{
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
        if (sprite == null && !TryComp<SpriteComponent>(uid, out sprite))
            return;

        // Check if the wheels layer exists
        if (!SpriteSystem.LayerMapTryGet((uid, sprite!), RMCVehicleWheelLayers.Wheels, out var layer, true))
        {
            return;
        }

        // Check if vehicle has wheels
        var hasWheels = true;
        if (AppearanceSystem.TryGetData(uid, RMCVehicleWheelVisuals.WheelCount, out int count))
            hasWheels = count > 0;
        else if (AppearanceSystem.TryGetData(uid, RMCVehicleWheelVisuals.HasAllWheels, out bool present))
            hasWheels = present;

        if (!hasWheels)
        {
            SpriteSystem.LayerSetVisible((uid, sprite!), layer, false);
            return;
        }

        var isMoving = false;
        if (TryComp<GridVehicleMoverComponent>(uid, out var gridMover))
            isMoving = MathF.Abs(gridMover.CurrentSpeed) > 0.01f;

        const string wheelsMoving = "wheels_0";
        const string wheelsStationary = "wheels_0";

        var targetState = isMoving ? wheelsMoving : wheelsStationary;
        var currentState = sprite!.LayerGetState(layer);

        if (currentState != targetState)
        {
            sprite.LayerSetState(layer, targetState);

            if (isMoving)
                SpriteSystem.LayerSetAnimationTime((uid, sprite), layer, 0f);
        }

        SpriteSystem.LayerSetAutoAnimated((uid, sprite), layer, isMoving);
        SpriteSystem.LayerSetVisible((uid, sprite), layer, true);
    }
}
