using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Melee;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OmaeWaMouShindeiruSystem))]
public sealed partial class OmaeWaMouShindeiruComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool DamageOnHit;

    [DataField, AutoNetworkedField]
    public bool BypassResistances = true;

    [DataField, AutoNetworkedField]
    public bool CanStunOnHit = true;

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

    /// <summary>
    /// Targets currently waiting for the delayed attack to trigger
    /// </summary>
    public readonly HashSet<EntityUid> PendingTargets = [];

    [DataField]
    public SoundSpecifier HeartBeatSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/alien_knockdown.ogg")
    {
        Params = AudioParams.Default.WithLoop(true)
    };

    [DataField]
    public SoundSpecifier BoneBreakSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/alien_knockdown.ogg"); //TODO RMC14 Bonebreak sound bone_break1
}
