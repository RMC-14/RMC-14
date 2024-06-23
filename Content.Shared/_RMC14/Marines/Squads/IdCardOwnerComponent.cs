using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Squads;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem))]
public sealed partial class IdCardOwnerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Id;
}
