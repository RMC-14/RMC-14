using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleWeaponsSystem))]
public sealed partial class RMCVehicleWeaponsComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Operator;

    [DataField, AutoNetworkedField]
    public EntityUid? SelectedWeapon;

    [NonSerialized]
    public Dictionary<string, EntityUid> HardpointOperators = new();

    [NonSerialized]
    public Dictionary<EntityUid, string> OperatorSelections = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleWeaponsSystem))]
public sealed partial class VehicleWeaponsOperatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Vehicle;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleWeaponsSystem))]
public sealed partial class VehicleWeaponsSeatComponent : Component
{
    [DataField, AutoNetworkedField]
    public SkillWhitelist Skills = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleWeaponsSystem), typeof(VehicleTurretSystem), typeof(VehicleTurretMuzzleSystem))]
public sealed partial class VehicleTurretComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool RotateToCursor = false;

    [DataField, AutoNetworkedField]
    public bool StabilizedRotation = false;

    [DataField, AutoNetworkedField]
    public float RotationSpeed = 0f;

    [DataField, AutoNetworkedField]
    public bool ShowOverlay = false;

    [DataField, AutoNetworkedField]
    public Vector2 PixelOffset = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public string OverlayRsi = string.Empty;

    [DataField, AutoNetworkedField]
    public string OverlayState = string.Empty;

    [DataField, AutoNetworkedField]
    public bool UseDirectionalOffsets = false;

    [DataField, AutoNetworkedField]
    public Vector2 PixelOffsetNorth = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 PixelOffsetEast = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 PixelOffsetSouth = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 PixelOffsetWest = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Angle WorldRotation = Angle.Zero;

    [DataField, AutoNetworkedField]
    public Angle TargetRotation = Angle.Zero;

    [NonSerialized]
    public EntityUid? VisualEntity;
}
