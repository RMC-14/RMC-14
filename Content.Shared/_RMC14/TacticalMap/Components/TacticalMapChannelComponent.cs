using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapChannelComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<NpcFactionPrototype> Faction;

    [DataField, AutoNetworkedField]
    public TacticalMapChannelScope Scope = TacticalMapChannelScope.Faction;

    [DataField, AutoNetworkedField]
    public HashSet<string> SquadGroups = new();

    [DataField, AutoNetworkedField]
    public bool SnapshotOnSubmit;

    [DataField, AutoNetworkedField]
    public TimeSpan? Cooldown;

    [DataField, AutoNetworkedField]
    public int SortOrder;

    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    [DataField, AutoNetworkedField]
    public EntityUid? Map;

    [DataField, AutoNetworkedField]
    public NetEntity? OwnerId;

    [ViewVariables]
    public EntityUid? LiveFeed;
}

[Serializable, NetSerializable]
public enum TacticalMapChannelScope : byte
{
    Faction,
    Hive,
    Squad,
}
