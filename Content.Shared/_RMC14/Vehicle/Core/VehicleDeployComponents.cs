using System;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleDeploySystem), typeof(VehicleWeaponsSystem))]
public sealed partial class VehicleDeployableComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Deployed;

    [DataField, AutoNetworkedField]
    public bool Deploying;

    [DataField, AutoNetworkedField]
    public bool DeployingTo;

    [DataField, AutoNetworkedField]
    public TimeSpan DeployTime = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan UndeployTime = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public bool AutoTurretEnabled = true;

    [DataField, AutoNetworkedField]
    public float AutoTargetRange = 20f;

    [DataField, AutoNetworkedField]
    public float AutoTargetCooldown = 0.2f;

    [DataField]
    public EntityUid? Deployer;

    [DataField]
    public TimeSpan NextAutoTargetTime;

    [DataField]
    public EntityUid? TargetingDeployer;

    [DataField]
    public EntityUid? AutoTarget;

    [DataField]
    public TimeSpan DeployEndTime;

    [DataField]
    public float AutoSpinSpeed = 90f;

    [DataField]
    public Angle AutoSpinWorldRotation = Angle.Zero;

    [DataField]
    public bool AutoSpinInitialized;
}

[RegisterComponent]
[Access(typeof(VehicleDeploySystem))]
public sealed partial class VehicleDeployGatedHardpointsComponent : Component
{
    [DataField]
    public List<string> BlockedHardpoints = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleDeploySystem))]
public sealed partial class VehicleDeployActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "ActionVehicleDeploy";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public EntityUid? Vehicle;
}

public sealed partial class VehicleDeployActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class VehicleDeployDoAfterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public bool Deploy;

    public override DoAfterEvent Clone()
    {
        return new VehicleDeployDoAfterEvent
        {
            Deploy = Deploy,
        };
    }
}
