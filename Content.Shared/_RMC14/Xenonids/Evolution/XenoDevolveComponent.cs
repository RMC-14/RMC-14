using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoEvolutionSystem))]
public sealed partial class XenoDevolveComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId[] DevolvesTo = Array.Empty<EntProtoId>();

    [DataField, AutoNetworkedField]
    public bool CanBeDevolvedByOther = true;
}
