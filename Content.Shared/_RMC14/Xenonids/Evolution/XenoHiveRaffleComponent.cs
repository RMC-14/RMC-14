using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoEvolutionSystem))]
public sealed partial class XenoHiveRaffleComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<int, bool> RaffleTiers = new()
    {
        [2] = false,
        [3] = true,
    };

    [DataField, AutoNetworkedField]
    public HashSet<int> PhaseAClosedTiers = new();

    [DataField, AutoNetworkedField]
    public Dictionary<int, List<EntProtoId>> LeapfrogTargets = new()
    {
        [3] = new() { "CMXenoRavager", "CMXenoPraetorian", "RMCXenoCrusher", "RMCXenoBoiler" },
    };
}
