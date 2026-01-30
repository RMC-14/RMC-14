using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Leap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCGrantLeapProtectionComponent : Component
{
    /// <summary>
    ///     How long the leaping entity should be stunned for when blocked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     The sound to make when a leap is blocked.
    /// </summary>
    [DataField]
    public SoundSpecifier BlockSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/bonk.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f)
    };

    /// <summary>
    ///     The component will only be granted if the item is equipped in one of these slots.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.OUTERCLOTHING;

    /// <summary>
    ///     If the item should give leap protection while an entity is holding it in a hand.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ProtectsInHand;
}
