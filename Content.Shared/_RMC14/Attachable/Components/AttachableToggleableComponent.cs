using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableToggleableSystem))]
public sealed partial class AttachableToggleableComponent : Component
{
    /// <summary>
    /// Whether the attachment is currently active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = false;

    /// <summary>
    /// If set to true, the attachment will deactivate upon switching hands.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedHand = false;

    /// <summary>
    /// If set to true, the attachment will not toggle itself when its action is interrupted. Used in cases where the item toggles itself separately, like scopes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DoInterrupt = false;

    /// <summary>
    /// If set to true, the attachment will deactivate upon moving.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnMove = false;

    /// <summary>
    /// If set to true, the attachment will deactivate upon rotating to any direction other than the one it was activated in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnRotate = false;

    /// <summary>
    /// If set to true, the attachment will deactivate upon rotating 90 degrees away from the one it was activated in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnFullRotate = false;

    /// <summary>
    /// If set to true, the attachment will deactivate upon being dropped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnDrop = false;

    /// <summary>
    /// If set to true, the attachment will slow the user upon being toggled due to any source but toggling it manually.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SlowOnBreak = false;

    /// <summary>
    /// If set to true, the attachment can only be toggled when the holder is wielded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WieldedOnly = false;

    /// <summary>
    /// If set to true, the attachment can only be used when the holder is wielded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WieldedUseOnly = false;

    /// <summary>
    /// If set to true, the attachment can only be activated when someone is holding it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HeldOnlyActivate = false;

    /// <summary>
    /// Only the person holding or wearing the holder can activate this attachment.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UserOnly = false;

    [DataField, AutoNetworkedField]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(0f);

    [DataField, AutoNetworkedField]
    public float DoAfter;

    [DataField, AutoNetworkedField]
    public float? DeactivateDoAfter;

    [DataField, AutoNetworkedField]
    public bool DoAfterNeedHand = true;

    [DataField, AutoNetworkedField]
    public bool DoAfterBreakOnMove = true;

    [DataField, AutoNetworkedField]
    public AttachableInstantToggleConditions InstantToggle = AttachableInstantToggleConditions.None;

    [DataField, AutoNetworkedField]
    public float InstantToggleRange = 1f;

    /// <summary>
    /// If set to true, this attachment will block some of the holder's functionality when active and perform it instead.
    /// Used for attached weapons, like the UGL.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SupercedeHolder = false;

    /// <summary>
    /// If set to true, this attachment's functions only work when it's attached to a holder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AttachedOnly = false;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/_RMC14/Attachable/attachment_activate.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeactivateSound = new SoundPathSpecifier("/Audio/_RMC14/Attachable/attachment_deactivate.ogg");

    [DataField, AutoNetworkedField]
    public bool ShowTogglePopup = true;

    [DataField, AutoNetworkedField]
    public LocId ActivatePopupText = new LocId("attachable-popup-activate-generic");

    [DataField, AutoNetworkedField]
    public LocId DeactivatePopupText = new LocId("attachable-popup-deactivate-generic");

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public string ActionId = "CMActionToggleAttachable";

    [DataField, AutoNetworkedField]
    public string ActionName = "Toggle Attachable";

    [DataField, AutoNetworkedField]
    public string ActionDesc = "Toggle an attachable. If you're seeing this, someone forgot to set the description properly.";

    [DataField, AutoNetworkedField]
    public EntityWhitelist? ActionsToRelayWhitelist;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Objects/Weapons/Guns/Attachments/rail.rsi"), "flashlight");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? IconActive;

    [DataField, AutoNetworkedField]
    public bool Attached = false;

    [DataField, AutoNetworkedField]
    public bool ActivateOnMove = true;
}

public enum AttachableInstantToggleConditions : byte
{
    None = 0,
    Brace = 1 << 0
}
