using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;
using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared._RMC14.Deploy;

/// <summary>
/// Allows an entity to be deployed into multiple child entities (setups), supports area checks, redeployment, and collapsing back into the original entity.
/// Used for deployable objects like tents, fortifications, etc. Handles all logic for area validation, tool requirements, and storage of the original entity during deployment.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCDeploySystem), Other = AccessPermissions.Read)]
public sealed partial class RMCDeployableComponent : Component, ISerializationHooks
{
    /// <summary>
    /// DoAfter time (seconds)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DeployTime = 10f;

    /// <summary>
    /// DoAfter time (seconds)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CollapseTime = 10f;

    /// <summary>
    /// The shape of the deploy area. The origin for the shape is the center of the nearest tile under the player.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public PhysShapeAabb DeployArea = new();

    /// <summary>
    /// List of objects to spawn.
    /// In the entity of the first DeploySetup in the list marked as ReactiveParental (or just the first in the list if there are no ReactiveParental),
    /// the entity that deployed all setups will be stored until collapse or destruction.
    /// If no setup is marked as ReactiveParental, by default the first in the list will be considered ReactiveParental.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<RMCDeploySetup> DeploySetups = new();

    /// <summary>
    /// Whether to check if the area is blocked
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AreaBlockedCheck = true;

    /// <summary>
    /// Whether to check the planet surface
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FailIfNotSurface = true;

    /// <summary>
    /// The prototype ID of the tool used for collapsing. If not specified, collapsing will not be possible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? CollapseToolPrototype;

    /// <summary>
    /// The current person deploying this entity (regardless of success).
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentDeployUser;

    /// <summary>
    /// Sound played after a successful deployment.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeploySound = new SoundPathSpecifier("/Audio/Items/shovel_dig.ogg");

    /// <summary>
    /// Sound played after a successful collapse.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? CollapseSound = new SoundPathSpecifier("/Audio/Items/shovel_dig.ogg");

    void ISerializationHooks.AfterDeserialization()
    {
        if (DeploySetups == null || DeploySetups.Count == 0)
            return;
        int parentalIndex = -1;
        for (int i = 0; i < DeploySetups.Count; i++)
        {
            if (DeploySetups[i].Mode == RMCDeploySetupMode.ReactiveParental)
            {
                parentalIndex = i;
                break;
            }
        }
        if (parentalIndex == -1)
        {
            // If there is no ReactiveParentalSetup, make the first one
            DeploySetups[0].Mode = RMCDeploySetupMode.ReactiveParental;
            DeploySetups[0].StorageOriginalEntity = true;
        }
        else
        {
            // The first ReactiveParentalSetup gets StorageOriginalEntity
            DeploySetups[parentalIndex].StorageOriginalEntity = true;
        }
    }
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RMCDeploySetup : ISerializationHooks
{
    /// <summary>
    /// The prototype of the entity to spawn
    /// </summary>
    [DataField(required: true)] public EntProtoId Prototype;

    /// <summary>
    /// The setup mode, determining the reaction of the deployed entity to various events
    /// </summary>
    [DataField]
    public RMCDeploySetupMode Mode = RMCDeploySetupMode.Default;

    /// <summary>
    /// If true, this setup will never be redeployed and collapsed
    /// </summary>
    [DataField] public bool NeverRedeployableSetup = false;

    /// <summary>
    /// Service flag for determining in which setup the original entity will be stored until collapse or destruction.
    /// Not for YAML! Only for runtime use.
    /// </summary>
    [DataField] public bool StorageOriginalEntity = false;

    /// <summary>
    /// Offset relative to the center of the deploy area
    /// </summary>
    [DataField] public Vector2 Offset = Vector2.Zero;

    /// <summary>
    /// Rotation angle (degrees)
    /// </summary>
    [DataField] public float Angle = 0f;

    /// <summary>
    /// Whether to anchor at the center of the nearest tile
    /// </summary>
    [DataField] public bool Anchor = true;


    void ISerializationHooks.AfterDeserialization()
    {
        if (StorageOriginalEntity) // YAML protection
            StorageOriginalEntity = false;
    }
}


[Serializable, NetSerializable]
public enum RMCDeploySetupMode
{
    /// <summary>
    /// Entities from Default setup do not react to deletion of entities deployed from ReactiveParental setups
    /// and cannot be a source of collapse or a storage for the original entity.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Entities from this setup will react to deletion of all entities deployed from setups marked as ReactiveParental and will also be deleted.
    /// </summary>
    Reactive = 1,

    /// <summary>
    /// Marks the setup as one of the conditional "parents" for all setups not marked as ReactiveParental.
    /// Required for collapse logic and for reacting to deletion of entities from setups marked as ReactiveParental.
    /// </summary>
    ReactiveParental = 2
}
