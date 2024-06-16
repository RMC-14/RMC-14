using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Webbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWebbingSystem))]
public sealed partial class WebbingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Rsi? PlayerSprite = new(new ResPath("_CM14/Objects/Clothing/Webbing/webbing.rsi"), "equipped");
}
