using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Hive;

[Serializable, NetSerializable]
public enum XenoHarmPermission : byte
{
    Forbidden,
    RestrictedInfected,
    Allowed,
}

[Serializable, NetSerializable]
public enum XenoConstructionPermission : byte
{
    Queen,
    Leaders,
    Anyone,
}

[Serializable, NetSerializable]
public enum XenoUnnestPermission : byte
{
    Builders,
    Anyone,
}
