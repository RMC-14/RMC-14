using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Admin;

[Serializable, NetSerializable]
public readonly record struct Hive(NetEntity Id, string Name);

[Serializable, NetSerializable]
public readonly record struct Squad(EntProtoId Id, bool Exists, int Members);

[Serializable, NetSerializable]
public sealed class RMCAdminEuiState(NetEntity target, List<Hive> hives, List<Squad> squads) : EuiStateBase
{
    public readonly NetEntity Target = target;
    public readonly List<Hive> Hives = hives;
    public readonly List<Squad> Squads = squads;
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
