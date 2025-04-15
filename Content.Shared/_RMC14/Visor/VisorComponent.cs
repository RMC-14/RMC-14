using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Visor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VisorSystem))]
public sealed partial class VisorComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? ToggledSprite;

    [DataField, AutoNetworkedField]
    public SlotFlags Slot = SlotFlags.HEAD;
}

public enum VisorVisualLayers
{
    Base
}
