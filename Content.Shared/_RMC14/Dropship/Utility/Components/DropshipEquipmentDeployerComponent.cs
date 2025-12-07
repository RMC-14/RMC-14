using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DropshipEquipmentDeployerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? DeployPrototype = "RMCML66DNestMetal";

    [DataField, AutoNetworkedField]
    public string DeploySlotId = "dropship_deploy";

    [DataField, AutoNetworkedField]
    public NetEntity? DeployEntity;

    [DataField, AutoNetworkedField]
    public bool IsDeployable;

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
    public bool AutoDeploy;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? UtilityDeployedSprite;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? WeaponDeployedSprite;
}
