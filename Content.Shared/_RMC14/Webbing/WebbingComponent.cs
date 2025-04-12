using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._RMC14.Webbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWebbingSystem))]
public sealed partial class WebbingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Rsi? PlayerSprite = new(new ResPath("_RMC14/Objects/Clothing/Webbing/webbing.rsi"), "equipped");

    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
