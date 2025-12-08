using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DropshipEquipmentDeployerComponent : Component
{
    /// <summary>
    ///     The prototype to deploy.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? DeployPrototype = "RMCML66DNestMetal";

    /// <summary>
    ///     The item slot to put the spawned entity in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string DeploySlotId = "dropship_deploy";

    /// <summary>
    ///     The text shown on the button in a dropship weapons control console.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string DropShipWindowButtonText = "MG";

    /// <summary>
    ///     The entity that will be deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? DeployEntity;

    /// <summary>
    ///     Whether the deployer can deploy.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsDeployable;

    /// <summary>
    ///     Whether the stored entity is currently deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsDeployed;

    /// <summary>
    ///     Whether the deployer should automatically deploy when a dropship arrives at it's destination.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoDeploy;

    /// <summary>
    ///     Whether the deployer should automatically undeploy when a dropship goes into FTL.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoUnDeploy;

    [DataField, AutoNetworkedField]
    public Vector2i StarboardForeDeployDirection = new(1, 0);

    [DataField, AutoNetworkedField]
    public Vector2i PortForeDeployDirection = new(-1, 0);

    [DataField, AutoNetworkedField]
    public Vector2i StarboardWingDeployDirection = new(0, -1);

    [DataField, AutoNetworkedField]
    public Vector2i PortWingDeployDirection = new(0, -1);

    [DataField, AutoNetworkedField]
    public float ForeDeployRotationDegrees = 180;

    [DataField, AutoNetworkedField]
    public float PortWingDeployRotationDegrees = -90;

    [DataField, AutoNetworkedField]
    public float StarboardWingDeployRotationDegrees = 90;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? UtilityDeployedSprite;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? WeaponDeployedSprite;
}
