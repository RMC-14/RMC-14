using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._RMC14.Medal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPlaytimeMedalSystem))]
public sealed partial class PlaytimeMedalComponent : Component
{
    [DataField, AutoNetworkedField]
    public Rsi? PlayerSprite = new(new ResPath("_RMC14/Objects/Medals/bronze.rsi"), "equipped");

    [DataField, AutoNetworkedField]
    public EntityUid? User;
}
