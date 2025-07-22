using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Deploy;

/// <summary>
/// Marks an entity as a deployed child of an RMCDeployableComponent. Stores a reference to the original deployable entity and the setup index.
/// Used for tracking, redeployment, and collapse logic of deployed entities (e.g., tent parts, fortification elements).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCDeploySystem), Other = AccessPermissions.Read)]
public sealed partial class RMCDeployedEntityComponent : Component, ISerializationHooks
{
    /// <summary>
    /// The original entity that initiated the deploy. Used to link this deployed entity back to its source deployable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid OriginalEntity;

    /// <summary>
    /// The index of the setup in DeploySetups that spawned this entity. Used for redeployment and collapse logic.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SetupIndex;

    /// <summary>
    /// Protection flag to prevent repeated processing during deletion. Set to true when shutdown is in progress.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool InShutdown = false;

    void ISerializationHooks.AfterDeserialization()
    {
        if (OriginalEntity != EntityUid.Invalid || SetupIndex != 0 || InShutdown) // YAML protection
        {
            OriginalEntity = EntityUid.Invalid;
            SetupIndex = 0;
            InShutdown = false;
        }
    }
}