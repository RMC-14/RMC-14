using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CM14.Marines.Squads;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SquadSystem))]
public sealed partial class SquadTeamComponent : Component
{
    [DataField(required: true)]
    public Color Color;

    [DataField(required: true)]
    public SpriteSpecifier Background;

    [DataField]
    public ProtoId<AccessLevelPrototype> AccessLevel;
}
