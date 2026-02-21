using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

/// <summary>
/// Applied to support hardpoints that modify onboard weapons.
/// </summary>
[RegisterComponent]
public sealed partial class RMCVehicleWeaponSupportAttachmentComponent : Component
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
[Access(typeof(RMCHardpointSystem))]
public sealed partial class RMCVehicleWeaponSupportModifierComponent : Component
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
public sealed partial class RMCVehicleSpeedModifierAttachmentComponent : Component
{
    [DataField]
    public float SpeedMultiplier = 1f;
}

/// <summary>
/// Aggregated speed modifier applied on the vehicle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCHardpointSystem))]
public sealed partial class RMCVehicleSpeedModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1f;
}

/// <summary>
/// Applied to support hardpoints that increase gunner view.
/// </summary>
[RegisterComponent]
public sealed partial class RMCVehicleGunnerViewAttachmentComponent : Component
{
    [DataField]
    public float PvsScale = 0.35f;
}

/// <summary>
/// Aggregated view modifier applied on the vehicle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCHardpointSystem))]
public sealed partial class RMCVehicleGunnerViewComponent : Component
{
    [DataField, AutoNetworkedField]
    public float PvsScale;
}

/// <summary>
/// Added to gunners to increase their view while operating a vehicle with a view module installed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleWeaponsSystem), typeof(RMCVehicleGunnerViewSystem))]
public sealed partial class RMCVehicleGunnerViewUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public float PvsScale;
}
