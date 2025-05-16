using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Squads;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem))]
public sealed partial class SquadMemberComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Squad;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier Background;

    [DataField(required: true), AutoNetworkedField]
    public Color BackgroundColor;

    [DataField, AutoNetworkedField]
    public List<SquadArmorLayers> BlacklistedSquadArmor = new();
}
