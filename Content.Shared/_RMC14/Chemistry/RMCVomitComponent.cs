using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry;

/// <summary>
/// Tracks the vomiting state and timings.
/// When present, the entity is in the process of vomiting or on cooldown.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVomitComponent : Component
{
    [DataField, AutoNetworkedField]
    public RMCVomitPhase Phase = RMCVomitPhase.Nausea;

    [DataField, AutoNetworkedField]
    public TimeSpan NextPhaseAt;

    /// <summary>
    /// Hunger change to apply when vomiting. Negative values decrease hunger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HungerLoss = -40f;

    /// <summary>
    /// Toxin healing to apply when vomiting (stored per-instance since callers can override).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ToxinHeal = 3f;

    /// <summary>
    /// How long from start until the "about to throw up" warning.
    /// </summary>
    [DataField]
    public TimeSpan WarningDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long from start until the actual vomit happens.
    /// </summary>
    [DataField]
    public TimeSpan VomitDelay = TimeSpan.FromSeconds(25);

    /// <summary>
    /// How long after vomiting before the component is removed (cooldown).
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
    /// Multiplier for how much of the chemical solution gets added to the vomit (default 10%).
    /// </summary>
    [DataField]
    public float ChemMultiplier = 0.1f;

    [DataField]
    public ProtoId<ReagentPrototype> VomitPrototype = "Vomit";

    [DataField]
    public SoundSpecifier VomitSound = new SoundCollectionSpecifier("Vomit", AudioParams.Default.WithVariation(0.2f).WithVolume(-4f));
}

[Serializable, NetSerializable]
public enum RMCVomitPhase : byte
{
    Nausea,
    Warning,
    Cooldown,
}
