using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[Serializable, NetSerializable]
public enum XenoEvolutionUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class XenoEvolveBuiState : BoundUserInterfaceState
{
    public readonly bool LackingOvipositor;
    public readonly Dictionary<string, int> RaffleCandidates;
    public readonly HashSet<int> ContestedTiers;
    public readonly List<string> LeapfrogTargets;
    public readonly bool PhaseAActive;

    public XenoEvolveBuiState(
        bool lackingOvipositor,
        Dictionary<string, int> raffleCandidates,
        HashSet<int> contestedTiers,
        List<string> leapfrogTargets,
        bool phaseAActive)
    {
        LackingOvipositor = lackingOvipositor;
        RaffleCandidates = raffleCandidates;
        ContestedTiers = contestedTiers;
        LeapfrogTargets = leapfrogTargets;
        PhaseAActive = phaseAActive;
    }
}

[Serializable, NetSerializable]
public sealed class XenoEvolveBuiMsg(EntProtoId choice) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Choice = choice;
}

[Serializable, NetSerializable]
public sealed class XenoJoinRaffleBuiMsg(EntProtoId choice) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Choice = choice;
}

[Serializable, NetSerializable]
public sealed class XenoLeaveRaffleBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class XenoStrainBuiMsg(EntProtoId choice) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Choice = choice;
}
