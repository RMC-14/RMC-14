using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Hive;

[Serializable, NetSerializable]
public enum HiveAllianceUIKey : byte
{
    Key
}

public static class HiveAlliableFactions
{
    public static readonly ProtoId<NpcFactionPrototype>[] All =
    [
        "UNMC",
        "WeYa",
        "CLF",
        "TSE",
        "HEFA",
        "SPP",
        "Halcyon",
        "RoyalMarines",
        "Bureau",
        "Civilian",
    ];
}

[Serializable, NetSerializable]
public sealed class HiveAllianceSetFactionMsg(ProtoId<NpcFactionPrototype> faction, bool allied) : BoundUserInterfaceMessage
{
    public readonly ProtoId<NpcFactionPrototype> Faction = faction;
    public readonly bool Allied = allied;
}

[Serializable, NetSerializable]
public sealed class HiveAllianceSetHiveMsg(NetEntity hive, bool allied) : BoundUserInterfaceMessage
{
    public readonly NetEntity Hive = hive;
    public readonly bool Allied = allied;
}
