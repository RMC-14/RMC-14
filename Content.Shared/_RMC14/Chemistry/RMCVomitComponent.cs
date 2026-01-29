using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry;

/// <summary>
/// Tracks the vomiting state and timings.
/// When present, the entity is in the process of vomiting or on cooldown.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVomitComponent : Component
{
    /// <summary>
    /// When true, the entity is in the process of vomiting (between nausea warning and actual vomit).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsVomiting;

    /// <summary>
    /// How long until the "about to throw up" warning.
    /// </summary>
    [DataField]
    public TimeSpan WarningDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long until the actual vomit happens.
    /// </summary>
    [DataField]
    public TimeSpan VomitDelay = TimeSpan.FromSeconds(25);

    /// <summary>
    /// How long until the vomit cooldown resets after vomit.
    /// </summary>
    [DataField]
    public TimeSpan CooldownAfterVomit = TimeSpan.FromSeconds(35);

    /// <summary>
    /// How long is the stun from vomiting.
    /// apply_effect(5, STUN) which is 5 * GLOBAL_STATUS_MULTIPLIER(20) = 100 deciseconds.
    /// </summary>
    [DataField]
    public TimeSpan VomitStunDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Multiplier for how much of the chemical solution gets added to vomit (default 10%).
    /// </summary>
    [DataField]
    public float ChemMultiplier = 0.1f;

    [DataField]
    public ProtoId<ReagentPrototype> VomitPrototype = "Vomit";

    [DataField]
    public SoundSpecifier VomitSound = new SoundCollectionSpecifier("Vomit",
        AudioParams.Default.WithVariation(0.2f).WithVolume(-4f));
}
