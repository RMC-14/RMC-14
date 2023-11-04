using Robust.Shared.GameStates;

namespace Content.Shared.CM14.Marines.Squads;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem))]
public sealed partial class SquadMemberComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Squad;
}
