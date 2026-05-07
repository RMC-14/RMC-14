using System.Collections.Immutable;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCActionsSystem))]
public sealed partial class RMCActionOrderComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Id;

    [DataField, AutoNetworkedField]
    public ImmutableArray<EntProtoId>? Order;
}
