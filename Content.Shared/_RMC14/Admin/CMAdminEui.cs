using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Admin;

[Serializable, NetSerializable]
public readonly record struct Hive(NetEntity Id, string Name);

[Serializable, NetSerializable]
public sealed class CMAdminEuiState(NetEntity target, List<Hive> hives) : EuiStateBase
{
    public readonly NetEntity Target = target;
    public readonly List<Hive> Hives = hives;
}

[Serializable, NetSerializable]
public sealed class CMAdminChangeHiveMsg(Hive hive) : EuiMessageBase
{
    public readonly Hive Hive = hive;
}

[Serializable, NetSerializable]
public sealed class CMAdminCreateHiveMsg(string name) : EuiMessageBase
{
    public readonly string Name = name;
}

[Serializable, NetSerializable]
public sealed class CMAdminTransformHumanoidMsg(string speciesId) : EuiMessageBase
{
    public readonly string SpeciesId = speciesId;
}

[Serializable, NetSerializable]
public sealed class CMAdminTransformXenoMsg(EntProtoId xenoId) : EuiMessageBase
{
    public readonly EntProtoId XenoId = xenoId;
}
