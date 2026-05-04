using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using static Content.Shared.Fax.AdminFaxEuiMsg;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoEvolutionSystem))]
public sealed partial class XenoDevolveComponent : Component, IComponentDebug
{
    [DataField, AutoNetworkedField]
    public EntProtoId[] DevolvesTo = Array.Empty<EntProtoId>();

    [DataField, AutoNetworkedField]
    public bool CanBeDevolvedByOther = true;

    public string GetDebugString()
    {
        return $"""
            CanBeDevolvedByOther: {CanBeDevolvedByOther}
            DevolvesTo:
              {string.Join("\r\n  ", DevolvesTo.Order())}
            """;
    }
}
