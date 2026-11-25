using Content.Shared._RMC14.Marines.Access;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTacticalMapSystem), typeof(IdModificationConsoleSystem))]
public sealed partial class TacticalMapAccessComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = new();
}
