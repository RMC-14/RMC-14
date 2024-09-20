using Content.Shared._RMC14.TacticalMap;
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
[Virtual]
public class RMCAdminEuiState(
    List<Hive> hives,
    List<Squad> squads,
    List<Xeno> xenos,
    int marines,
    List<(Guid Id, string Actor, int Round)> tacticalMapHistory,
    (Guid Id, List<TacticalMapLine> Lines, string Actor, int RoundId) tacticalMapLines
) : EuiStateBase
{
    public readonly List<Hive> Hives = hives;
    public readonly List<Squad> Squads = squads;
    public readonly List<Xeno> Xenos = xenos;
    public readonly int Marines = marines;
    public readonly List<(Guid Id, string Actor, int Round)> TacticalMapHistory = tacticalMapHistory;
    public readonly (Guid Id, List<TacticalMapLine> Lines, string Actor, int RoundId) TacticalMapLines = tacticalMapLines;
}

[Serializable, NetSerializable]
public sealed class RMCAdminEuiTargetState(
    List<Hive> hives,
    List<Squad> squads,
    List<Xeno> xenos,
    int marines,
    List<(Guid Id, string Actor, int Round)> tacticalMapHistory,
    (Guid Id, List<TacticalMapLine> Lines, string Actor, int RoundId) tacticalMapLines,
    List<(string Name, bool Present)> specialistSkills,
    int points,
    Dictionary<string, int> extraPoints
) : RMCAdminEuiState(hives, squads, xenos, marines, tacticalMapHistory, tacticalMapLines)
{
    public readonly List<(string Name, bool Present)> SpecialistSkills = specialistSkills;
    public readonly int Points = points;
    public readonly Dictionary<string, int> ExtraPoints = extraPoints;
}

[Serializable, NetSerializable]
public sealed class RMCAdminSetVendorPointsMsg(int points) : EuiMessageBase
{
    public readonly int Points = points;
}

[Serializable, NetSerializable]
public sealed class RMCAdminSetSpecialistVendorPointsMsg(int points) : EuiMessageBase
{
    public readonly int Points = points;
}

[Serializable, NetSerializable]
public sealed class RMCAdminAddSpecSkillMsg(string component) : EuiMessageBase
{
    public readonly string Component = component;
}

[Serializable, NetSerializable]
public sealed class RMCAdminRemoveSpecSkillMsg(string component) : EuiMessageBase
{
    public readonly string Component = component;
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
public sealed class RMCAdminRefresh : EuiMessageBase;

[Serializable, NetSerializable]
public sealed class RMCAdminRequestTacticalMapHistory(Guid id) : EuiMessageBase
{
    public readonly Guid Id = id;
}
