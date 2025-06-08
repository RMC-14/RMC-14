using Robust.Shared.Audio;

namespace Content.Shared._RMC14.Xenonids.Lunge;

[RegisterComponent, AutoGenerateComponentState()]
public sealed partial class RMCLungeProtectionComponent : Component
{
    /// <summary>
    ///     How long the lunging entity should be stunned for when blocked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     The sound to make when a leap is blocked.
    /// </summary>
    [DataField]
    public SoundSpecifier BlockSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/bonk.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f),
    };

    /// <summary>
    ///     If this is true the entity is protected from leaps from all directions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FullProtection = true;
}
