using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.SupplyDrop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSupplyDropSystem))]
public sealed partial class SupplyDropPadComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId<SquadTeamComponent>? Squad;
}
