using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapObjectivesComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<SquadObjectiveType, string> Objectives = new();
}
