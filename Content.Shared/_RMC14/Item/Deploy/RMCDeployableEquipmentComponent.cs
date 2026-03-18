using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Item.Deploy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCDeployableEquipmentComponent : Component
{
    /// <summary>
    ///     The current state of the deployable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DeployedState DeployedState;

    /// <summary>
    ///     The duration of the deploying DoAfter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DeployDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     The duration of the undeploying DoAfter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UndeployDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     The skill that affects the deploy and undeploy duration.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent>? DelaySkill = "RMCSkillEngineer";

    /// <summary>
    ///     The tool quality needed to undeploy the deployable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> UndeployQuality = "Pulsing";

    /// <summary>
    ///     Whether the entity should be anchored when deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AnchorOnDeploy = true;

    /// <summary>
    ///     The whitelist to check for in the <see cref="PlaceableCheckRange"/>, deployment will be blocked if any found entity matches this.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? PlaceableBlacklist;

    /// <summary>
    ///     The range of the blacklist check.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PlaceableCheckRange;

    /// <summary>
    ///     How far in front of the user the deployable should be placed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DeployDistance = 1;

    /// <summary>
    ///     The name of the fixture that should be enabled/disabled when deploying/undeploying.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? DeployFixture = "deploy";
}

[Serializable, NetSerializable]
public enum DeployedState
{
    Undeployed,
    Deployed,
}
