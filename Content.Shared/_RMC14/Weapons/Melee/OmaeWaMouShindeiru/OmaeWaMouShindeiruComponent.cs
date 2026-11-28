using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Melee.OmaeWaMouShindeiru;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OmaeWaMouShindeiruSystem))]
public sealed partial class OmaeWaMouShindeiruComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool DamageOnHit;

    [DataField, AutoNetworkedField]
    public bool BypassResistances = true;

    [DataField, AutoNetworkedField]
    public bool CanStunOnKatanaClash = true;

    /// <summary>
    /// How long to wait before the cuts happen.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan KillDelay = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// How many cuts are applied when the delay finishes
    /// </summary>
    /// <remarks>I AM THE STORM THAT IS APPROACHING</remarks>
    [DataField, AutoNetworkedField]
    public int NumberOfCuts = 8;

    [DataField]
    public SoundSpecifier DeathImminentSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/heart_beat.ogg")
    {
        Params = AudioParams.Default.WithLoop(true)
    };
}
