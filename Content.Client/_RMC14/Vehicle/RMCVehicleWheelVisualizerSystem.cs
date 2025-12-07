using Content.Shared._RMC14.Vehicle;
using Content.Shared.Movement.Components;
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

        var enumerator = EntityManager.AllEntityQueryEnumerator<SpriteMovementComponent, RMCVehicleWheelSlotsComponent, SpriteComponent>();
        while (enumerator.MoveNext(out var uid, out var movement, out _, out var sprite))
        {
            UpdateWheelVisuals(uid, movement, sprite);
        }
    }

    private void UpdateWheelVisuals(EntityUid uid, SpriteMovementComponent? movement = null, SpriteComponent? sprite = null)
    {
        if (sprite == null && !TryComp<SpriteComponent>(uid, out sprite))
            return;

        if (!SpriteSystem.LayerMapTryGet((uid, sprite!), RMCVehicleWheelLayers.Wheels, out var layer, true))
        {
            return;
        }

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

        movement ??= TryComp<SpriteMovementComponent>(uid, out var move) ? move : null;
        var isMoving = movement?.IsMoving == true;
        const string wheelsState = "wheels_0";
        var targetState = wheelsState;

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

    private void UpdateWheelVisuals(EntityUid uid)
    {
        UpdateWheelVisuals(uid, null, null);
    }
}
