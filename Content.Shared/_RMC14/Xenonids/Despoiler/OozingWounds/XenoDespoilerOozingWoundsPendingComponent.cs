using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Despoiler.OozingWounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerOozingWoundsPendingComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<XenoDespoilerOozingWoundsPendingTile> Pending = new();
}

[Serializable, NetSerializable]
public record struct XenoDespoilerOozingWoundsPendingTile(TimeSpan SpawnAt, NetCoordinates Tile, EntProtoId SprayProto, EntProtoId PuddleProto, float PuddleChance);
