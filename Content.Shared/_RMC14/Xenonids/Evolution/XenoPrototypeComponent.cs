using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoPrototypeComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? TargetPrototypeId;

    /// <summary>
    /// Specifically NOT networked. If this is different from the target prototype id, that means
    /// the xeno change prototype logic has to run to change it over on the client.
    /// </summary>
    public EntProtoId? CurrentPrototypeId;
}
