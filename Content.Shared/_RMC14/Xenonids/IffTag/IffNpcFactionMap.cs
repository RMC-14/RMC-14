using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.IffTag;

// TODO RMC14: remove this lol
public static class IffNpcFactionMap
{
    public static readonly Dictionary<ProtoId<NpcFactionPrototype>, EntProtoId<IFFFactionComponent>> NpcToIff = new()
    {
        ["UNMC"] = "FactionMarine",
        ["WeYa"] = "FactionWeYa",
        ["CLF"] = "FactionCLF",
        ["TSE"] = "FactionTSE",
        ["SPP"] = "FactionSPP",
    };

    public static readonly Dictionary<EntProtoId<IFFFactionComponent>, ProtoId<NpcFactionPrototype>> IffToNpc = new()
    {
        ["FactionMarine"] = "UNMC",
        ["FactionWeYa"] = "WeYa",
        ["FactionCLF"] = "CLF",
        ["FactionTSE"] = "TSE",
        ["FactionSPP"] = "SPP",
    };
}
