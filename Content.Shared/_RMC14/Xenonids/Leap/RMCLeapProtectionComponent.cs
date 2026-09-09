using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Leap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCLeapProtectionComponent : Component
{
    /// <summary>
    ///     NOT actual duration
    ///     This value = CM's apply_effect(value) x 2
    ///     This value typically scaled further by RMCStatusEffectSystem.
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
    ///     NOT actual duration
    ///     This value = CM's apply_effect(value) x 2
    ///     This value typically scaled further by RMCStatusEffectSystem.
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
    ///     All equipped entities that are providing this component to an entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ProtectionProviders = new();

    /// <summary>
    ///     If this is true the entity is protected from leaps from all directions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FullProtection;
}
