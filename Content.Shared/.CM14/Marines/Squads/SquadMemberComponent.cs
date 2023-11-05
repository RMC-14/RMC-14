using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.CM14.Marines.Squads;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem))]
public sealed partial class SquadMemberComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Squad;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier Background;
}
