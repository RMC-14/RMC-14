using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerOozingWoundsPendingComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<XenoDespoilerOozingWoundsPendingTile> Pending = new();
}

[Serializable, NetSerializable]
public struct XenoDespoilerOozingWoundsPendingTile
{
    public TimeSpan SpawnAt;
    public EntityCoordinates Tile;
    public EntProtoId SprayProto;
    public EntProtoId PuddleProto;
    public float PuddleChance;
}
