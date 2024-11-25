using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sprite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCSpriteSystem))]
public sealed partial class SpriteColorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;
}
