using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._CM14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableToggleableSystem))]
public sealed partial class AttachableToggleableComponent : Component
{
    [ViewVariables]
    public bool Active = false;

    [DataField, AutoNetworkedField]
    public float DoAfter;

    [DataField, AutoNetworkedField]
    public bool NeedHand = true;

    [DataField, AutoNetworkedField]
    public bool BreakOnMove = true;

    [DataField, AutoNetworkedField]
    public bool SupercedeHolder = false;

    [DataField, AutoNetworkedField]
    public bool AttachedOnly = false;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/_CM14/Attachable/attachment_activate.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeactivateSound = new SoundPathSpecifier("/Audio/_CM14/Attachable/attachment_deactivate.ogg");

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public string ActionId = "CMActionToggleAttachable";

    [DataField, AutoNetworkedField]
    public string ActionName = "Toggle Attachable";

    [DataField, AutoNetworkedField]
    public string ActionDesc = "Toggle an attachable. If you're seeing this, someone forgot to set the description properly.";

    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("_CM14/Objects/Weapons/Guns/Attachments/rail.rsi"), "flashlight");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? IconActive;

    [ViewVariables]
    public bool Attached = false;
}
