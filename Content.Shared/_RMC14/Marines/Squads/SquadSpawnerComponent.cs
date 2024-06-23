using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Squads;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem))]
public sealed partial class SquadSpawnerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Squad;

    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype>? Role;
}
