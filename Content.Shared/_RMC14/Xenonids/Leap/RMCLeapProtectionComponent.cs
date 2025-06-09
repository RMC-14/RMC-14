using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Leap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCLeapProtectionComponent : Component
{
    /// <summary>
    ///     How long the leaping entity should be stunned for when blocked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(0);

    /// <summary>
    ///     The sound to make when a leap is blocked.
    /// </summary>
    [DataField]
    public SoundSpecifier BlockSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/bonk.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f)
    };

    /// <summary>
    ///     How long the leaping entity should be stunned for when blocked.
    ///     Only set this if the entity has inherent leap protection.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? InherentStunDuration;

    /// <summary>
    ///     The inherent sound to make when a leap is blocked.
    ///     Only used if the entity has permanent inherent leap protection.
    /// </summary>
    [DataField]
    public SoundSpecifier InherentBlockSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/bonk.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f)
    };

    /// <summary>
    ///     The component will only be granted if the item is equipped in one of these slots.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.OUTERCLOTHING;

    /// <summary>
    ///     All equipped entities that are providing this component to an entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ProtectionProviders = new();
}
