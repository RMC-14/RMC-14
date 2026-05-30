using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public enum TacticalMapUserUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class TacticalMapBuiState(string mapName, Dictionary<SquadObjectiveType, string>? squadObjectives = null) : BoundUserInterfaceState
{
    public readonly string MapName = mapName;
    public readonly Dictionary<SquadObjectiveType, string>? SquadObjectives = squadObjectives;
}
