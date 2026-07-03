using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

/// <summary>
/// Applied to support hardpoints that modify onboard weapons.
/// </summary>
[RegisterComponent]
public sealed partial class VehicleWeaponSupportAttachmentComponent : Component
{
    [DataField]
    public FixedPoint2 AccuracyMultiplier = 1;

    [DataField]
    public float FireRateMultiplier = 1f;
}

/// <summary>
/// Aggregated modifiers applied on the vehicle when weapon support attachments are installed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HardpointSystem))]
public sealed partial class VehicleWeaponSupportModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 AccuracyMultiplier = 1;

    [DataField, AutoNetworkedField]
    public float FireRateMultiplier = 1f;
}

/// <summary>
/// Applied to support hardpoints that modify vehicle speed.
/// </summary>
[RegisterComponent]
public sealed partial class VehicleSpeedModifierAttachmentComponent : Component
{
    [DataField]
    public float SpeedMultiplier = 1f;
}

/// <summary>
/// Aggregated speed modifier applied on the vehicle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HardpointSystem))]
public sealed partial class VehicleSpeedModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1f;
}

/// <summary>
/// Applied to hardpoints that modify vehicle acceleration.
/// </summary>
[RegisterComponent]
public sealed partial class VehicleAccelerationModifierAttachmentComponent : Component
{
    [DataField]
    public float AccelerationMultiplier = 1f;
}

/// <summary>
/// Aggregated acceleration modifier applied on the vehicle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HardpointSystem))]
public sealed partial class VehicleAccelerationModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public float AccelerationMultiplier = 1f;
}

/// <summary>
/// Applied to support hardpoints that increase gunner view.
/// </summary>
[RegisterComponent]
public sealed partial class VehicleGunnerViewAttachmentComponent : Component
{
    [DataField]
    public float PvsScale = 0.35f;

    [DataField]
    public float CursorMaxOffset;

    [DataField]
    public float CursorOffsetSpeed = 0.5f;

    [DataField]
    public float CursorPvsIncrease;
}

/// <summary>
/// Aggregated view modifier applied on the vehicle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HardpointSystem))]
public sealed partial class VehicleGunnerViewComponent : Component
{
    [DataField, AutoNetworkedField]
    public float PvsScale;

    [DataField, AutoNetworkedField]
    public float CursorMaxOffset;

    [DataField, AutoNetworkedField]
    public float CursorOffsetSpeed = 0.5f;

    [DataField, AutoNetworkedField]
    public float CursorPvsIncrease;
}

/// <summary>
/// Added to gunners to increase their view while operating a vehicle with a view module installed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleWeaponsSystem), typeof(VehicleGunnerViewSystem))]
public sealed partial class VehicleGunnerViewUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public float PvsScale;

    [DataField, AutoNetworkedField]
    public float CursorMaxOffset;

    [DataField, AutoNetworkedField]
    public float CursorOffsetSpeed = 0.5f;

    [DataField, AutoNetworkedField]
    public float CursorPvsIncrease;
}
