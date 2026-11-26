using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapTrackedComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool TrackDead;

    [DataField, AutoNetworkedField]
    public List<ProtoId<NpcFactionPrototype>> Factions = new();
}
