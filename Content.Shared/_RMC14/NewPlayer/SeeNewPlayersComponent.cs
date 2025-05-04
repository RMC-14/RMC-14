using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.NewPlayer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeeNewPlayersComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi OneLabel = new(new ResPath("_RMC14/Interface/new_player.rsi"), "new_player_marker_1");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi TwoLabel = new(new ResPath("_RMC14/Interface/new_player.rsi"), "new_player_marker_2");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi ThreeLabel = new(new ResPath("_RMC14/Interface/new_player.rsi"), "new_player_marker_3");
}
