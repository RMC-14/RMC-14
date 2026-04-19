using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleWeaponsSystem))]
public sealed partial class VehicleWeaponsComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Operator;

    [DataField, AutoNetworkedField]
    public EntityUid? SelectedWeapon;

    [NonSerialized]
    public Dictionary<EntityUid, EntityUid> HardpointOperators = new();

    [NonSerialized]
    public Dictionary<EntityUid, EntityUid> OperatorSelections = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleWeaponsSystem))]
public sealed partial class VehicleWeaponsOperatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Vehicle;

    [DataField, AutoNetworkedField]
    public EntityUid? SelectedWeapon;

    [NonSerialized]
    public Dictionary<EntityUid, EntityUid> HardpointActions = new();

    [NonSerialized]
    public TimeSpan NextCooldownFeedbackAt = TimeSpan.Zero;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleWeaponsSystem))]
public sealed partial class VehicleWeaponsSeatComponent : Component
{
    [DataField, AutoNetworkedField]
    public SkillWhitelist Skills = new();

    [DataField]
    public bool IsPrimaryOperatorSeat = true;

    [DataField]
    public bool AllowUiSelection = true;

    [DataField]
    public bool AllowHotbarSelection = true;

    [DataField]
    public List<string> AllowedHardpointTypes = new();

    [DataField]
    public float BaseViewPvsScale;

    [DataField]
    public float BaseViewCursorMaxOffset;

    [DataField]
    public float BaseViewCursorOffsetSpeed = 0.5f;

    [DataField]
    public float BaseViewCursorPvsIncrease;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleWeaponsSystem), typeof(VehicleTurretSystem), typeof(VehicleTurretMuzzleSystem))]
public sealed partial class VehicleTurretComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool RotateToCursor = false;

    [DataField, AutoNetworkedField]
    public float FireWhileRotatingGraceDegrees = 0f;

    [DataField, AutoNetworkedField]
    public bool UseBarrelDirectionForShots = false;

    [DataField, AutoNetworkedField]
    public float MaxShotCurvatureDegrees = 0f;

    [DataField, AutoNetworkedField]
    public bool StabilizedRotation = false;

    [DataField, AutoNetworkedField]
    public float RotationSpeed = 0f;

    [DataField, AutoNetworkedField]
    public float ReverseDirectionDelay = 0.06f;

    [DataField, AutoNetworkedField]
    public float RotationInputDeadzoneDegrees = 1.5f;

    [DataField, AutoNetworkedField]
    public bool ShowOverlay = false;

    [DataField, AutoNetworkedField]
    public bool OffsetRotatesWithTurret = false;

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

    [NonSerialized]
    public Angle? PendingTargetRotation;

    [NonSerialized]
    public TimeSpan PendingTargetApplyAt = TimeSpan.Zero;

    [NonSerialized]
    public int PendingDirectionSign = 0;

    [NonSerialized]
    public int LastAppliedDirectionSign = 0;
}
