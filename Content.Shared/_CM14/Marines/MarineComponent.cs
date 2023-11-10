using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.CM14.Marines;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMarineSystem))]
public sealed partial class MarineComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Icon;
}
