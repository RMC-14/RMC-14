using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Squads;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem))]
public sealed partial class AssignSquadComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
