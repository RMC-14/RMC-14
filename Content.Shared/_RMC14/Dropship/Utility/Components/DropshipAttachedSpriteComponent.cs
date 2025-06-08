using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DropshipUtilitySystem))]
public sealed partial class DropshipAttachedSpriteComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? Sprite;
}
