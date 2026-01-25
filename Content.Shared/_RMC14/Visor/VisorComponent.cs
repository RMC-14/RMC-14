using Content.Shared._RMC14.Marines.Skills;
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

    [DataField, AutoNetworkedField]
    public SkillWhitelist? SkillsRequired;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi OnIcon;
}

public enum VisorVisualLayers
{
    Base
}
