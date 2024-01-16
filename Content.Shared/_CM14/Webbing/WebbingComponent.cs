using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._CM14.Webbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWebbingSystem))]
public sealed partial class WebbingComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? PlayerSprite = new(new ResPath("_CM14/Objects/Clothing/webbing.rsi"), "equipped");
}
