using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Deploy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCDeploySystem), Other = AccessPermissions.Read)]
public sealed partial class RMCSharedDeployedEntityComponent : Component, ISerializationHooks
{
    // The original entity that initiated the deploy
    [DataField, AutoNetworkedField]
    public EntityUid OriginalEntity;

    // The index of the setup in DeploySetups that spawned this entity
    [DataField, AutoNetworkedField]
    public int SetupIndex;

    // Флаг для защиты от повторной обработки при удалении
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