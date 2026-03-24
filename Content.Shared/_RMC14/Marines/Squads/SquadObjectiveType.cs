using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Squads;

/// <summary>
/// Types of squad objectives that can be assigned to a squad.
/// </summary>
[Serializable, NetSerializable]
public enum SquadObjectiveType : byte
{
    /// <summary>
    /// Primary objective for the squad.
    /// </summary>
    Primary = 0,

    /// <summary>
    /// Secondary objective for the squad.
    /// </summary>
    Secondary = 1
}

