using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.NightVision;

/// <summary>
/// Gives a wearable item an action that applies night vision settings to the wearer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNightVisionSystem))]
public sealed partial class NightVisionItemComponent : Component
{
    /// <summary>
    /// Action prototype granted while the item is worn in the required slot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? ActionId = "CMActionToggleScoutVision";

    /// <summary>
    /// Action entity currently linked to this item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    /// <summary>
    /// Entity currently receiving this item's night vision.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    /// If false, the item applies night vision while worn and cannot be action-toggled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Toggleable = true;

    /// <summary>
    /// Slot that must contain the item for its action to be available.
    /// </summary>
    // TODO RMC14 only supports one flag because inventory callers mix string slots and SlotFlags.
    [DataField, AutoNetworkedField]
    public SlotFlags SlotFlags { get; set; } = SlotFlags.EYES;

    /// <summary>
    /// Optional skills required before the item can apply night vision.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int>? Skills;

    /// <summary>
    /// Whether the wearer receives the green night vision overlay.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Green;

    /// <summary>
    /// Whether the wearer receives meson-style FoV behavior.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Mesons;

    /// <summary>
    /// Enables the experimental meson FoV override without changing older RMC meson optics.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ExperimentalMesonFov;

    /// <summary>
    /// Whether scoping should be blocked while this item is active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BlockScopes;

    /// <summary>
    /// Lets this item use its full state even when the wearer has innate half-only night vision.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreUserOnlyHalf;

    /// <summary>
    /// Night vision state applied when the item is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NightVisionState DefaultState = NightVisionState.Full;

    /// <summary>
    /// Whether the wearer had night vision before this item was enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HadNightVision;

    /// <summary>
    /// Previous wearer night vision state restored when this item is disabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NightVisionState PreviousState = NightVisionState.Off;

    /// <summary>
    /// Previous wearer night vision color restored when this item is disabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PreviousGreen;

    /// <summary>
    /// Previous wearer meson state restored when this item is disabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PreviousMesons;

    /// <summary>
    /// Previous wearer experimental meson FoV state restored when this item is disabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PreviousExperimentalMesonFov;

    /// <summary>
    /// Previous wearer scope blocking state restored when this item is disabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PreviousBlockScopes;

    /// <summary>
    /// Sound played locally when this item turns on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundOn = new SoundPathSpecifier("/Audio/_RMC14/Handling/toggle_nv1.ogg");

    /// <summary>
    /// Sound played locally when this item turns off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundOff = new SoundPathSpecifier("/Audio/_RMC14/Handling/toggle_nv2.ogg");
}
