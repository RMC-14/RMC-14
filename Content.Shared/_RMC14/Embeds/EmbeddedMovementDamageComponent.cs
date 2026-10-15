using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Embeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedForeignObjectEmbeddedSystem))]
public sealed partial class EmbeddedMovementDamageComponent : Component
{
    [DataField, AutoNetworkedField, Access(typeof(SharedForeignObjectEmbeddedSystem), Other = AccessPermissions.ReadWriteExecute)]
    public EntityCoordinates? LastPosition;

    [DataField, AutoNetworkedField, Access(typeof(SharedForeignObjectEmbeddedSystem), Other = AccessPermissions.ReadWriteExecute)]
    public float DistanceMoved;

    [DataField]
    public float DistanceThreshold = 1f;

    [DataField]
    public float DamagePerEmbedded = 0.5f;

    [DataField]
    public int MovementWarningCounter;
}
