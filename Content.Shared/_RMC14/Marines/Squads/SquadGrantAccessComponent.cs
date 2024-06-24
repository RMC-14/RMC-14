using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Squads;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SquadGrantAccessComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<AccessLevelPrototype>? AccessLevel;

    [DataField, AutoNetworkedField]
    public string? RoleName;
}
