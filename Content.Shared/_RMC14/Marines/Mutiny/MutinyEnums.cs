using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Mutiny;

[Serializable, NetSerializable]
public enum MutinyPhase : byte
{
    Recruiting,
    Active,
}

[Serializable, NetSerializable]
public enum MutinySide : byte
{
    Mutineer,
    Loyalist,
    NonCombatant,
}
