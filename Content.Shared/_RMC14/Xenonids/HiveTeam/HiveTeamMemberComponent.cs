using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.HiveTeam;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HiveTeamMemberComponent : Component
{
    [DataField, AutoNetworkedField]
    public int TeamNumber;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi Icon = new(new ResPath("/Textures/_RMC14/Interface/marine_hud.rsi"), "hudsquad_ft1");

    [DataField, AutoNetworkedField]
    public Color IconColor = Color.FromHex("#7B2FBE");
}
