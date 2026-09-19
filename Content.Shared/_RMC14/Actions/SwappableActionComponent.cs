using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SwappableActionSystem))]
public sealed partial class SwappableActionComponent : Component
{
    /// <summary>
    ///     The action prototype swapped to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? SwappedActionProto;

    [DataField, AutoNetworkedField]
    public bool IsSwapped;
}
