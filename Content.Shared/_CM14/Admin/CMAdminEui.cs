using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Admin;

[Serializable, NetSerializable]
public readonly record struct Hive(NetEntity Id, string Name);

[Serializable, NetSerializable]
public sealed class CMAdminEuiState : EuiStateBase
{
    public readonly NetEntity Target;
    public readonly List<Hive> Hives;

    public CMAdminEuiState(NetEntity target, List<Hive> hives)
    {
        Target = target;
        Hives = hives;
    }
}

[Serializable, NetSerializable]
public sealed class CMAdminChangeHiveMessage : EuiMessageBase
{
    public readonly Hive Hive;

    public CMAdminChangeHiveMessage(Hive hive)
    {
        Hive = hive;
    }
}

[Serializable, NetSerializable]
public sealed class CMAdminCreateHiveMessage : EuiMessageBase
{
    public readonly string Name;

    public CMAdminCreateHiveMessage(string name)
    {
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class CMAdminTransformHumanoidMessage : EuiMessageBase
{
    public readonly string SpeciesId;

    public CMAdminTransformHumanoidMessage(string speciesId)
    {
        SpeciesId = speciesId;
    }
}

[Serializable, NetSerializable]
public sealed class CMAdminTransformXenoMessage : EuiMessageBase
{
    public readonly EntProtoId XenoId;

    public CMAdminTransformXenoMessage(EntProtoId xenoId)
    {
        XenoId = xenoId;
    }
}
