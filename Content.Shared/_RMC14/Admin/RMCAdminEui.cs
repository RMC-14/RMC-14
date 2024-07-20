using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Admin;

[Serializable, NetSerializable]
public readonly record struct Hive(NetEntity Id, string Name);

[Serializable, NetSerializable]
public readonly record struct Squad(EntProtoId Id, bool Exists, int Members);

[Serializable, NetSerializable]
public readonly record struct Xeno(EntProtoId Proto);

[Serializable, NetSerializable]
public sealed class RMCAdminEuiState(
    List<Hive> hives,
    List<Squad> squads,
    List<Xeno> xenos,
    int marines,
    Dictionary<string, float> marinesPerXeno
) : EuiStateBase
{
    public readonly List<Hive> Hives = hives;
    public readonly List<Squad> Squads = squads;
    public readonly List<Xeno> Xenos = xenos;
    public readonly int Marines = marines;
    public readonly Dictionary<string, float> MarinesPerXeno = marinesPerXeno;
}

[Serializable, NetSerializable]
public sealed class RMCAdminChangeHiveMsg(Hive hive) : EuiMessageBase
{
    public readonly Hive Hive = hive;
}

[Serializable, NetSerializable]
public sealed class RMCAdminCreateHiveMsg(string name) : EuiMessageBase
{
    public readonly string Name = name;
}

[Serializable, NetSerializable]
public sealed class RMCAdminTransformHumanoidMsg(string speciesId) : EuiMessageBase
{
    public readonly string SpeciesId = speciesId;
}

[Serializable, NetSerializable]
public sealed class RMCAdminTransformXenoMsg(EntProtoId xenoId) : EuiMessageBase
{
    public readonly EntProtoId XenoId = xenoId;
}

[Serializable, NetSerializable]
public sealed class RMCAdminCreateSquadMsg(EntProtoId squadId) : EuiMessageBase
{
    public readonly EntProtoId SquadId = squadId;
}

[Serializable, NetSerializable]
public sealed class RMCAdminAddToSquadMsg(EntProtoId squadId) : EuiMessageBase
{
    public readonly EntProtoId SquadId = squadId;
}

[Serializable, NetSerializable]
public sealed class RMCAdminRefresh : EuiMessageBase;
