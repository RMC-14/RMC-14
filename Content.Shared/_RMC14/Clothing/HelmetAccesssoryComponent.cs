using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HelmetAccessoriesSystem))]
public sealed partial class HelmetAccessoryComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi Rsi;

    // todo add toggle sprites
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? ToggledRsi;
}
