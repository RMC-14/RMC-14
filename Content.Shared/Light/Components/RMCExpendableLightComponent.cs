namespace Content.Shared.Light.Components;

/// <summary>
///     This is an extension of the upstream ExpendableLightComponent
/// </summary>
public sealed partial class ExpendableLightComponent
{
    /// <summary>
    ///     Can the expendable light be picked up while it's turned on.
    /// </summary>
    [DataField]
    public bool PickupWhileOn = true;

    /// <summary>
    ///     How much faster this light gets dimmed by acid.
    /// </summary>
    [DataField]
    public float AcidDamageMultiplier = 1;

    [DataField]
    public TimeSpan PhaseOneDuration;

    [DataField]
    public TimeSpan PhaseTwoDuration;

    [DataField]
    public TimeSpan PhaseThreeDuration;

    [DataField]
    public TimeSpan PhaseFourDuration;

    [DataField]
    public TimeSpan PhaseFiveDuration;

    [DataField]
    public string PhaseOneBehaviourID = "phase_1";

    [DataField]
    public string PhaseTwoBehaviourID = "phase_2";

    [DataField]
    public string PhaseThreeBehaviourID = "phase_3";

    [DataField]
    public string PhaseFourBehaviourID = "phase_4";

    [DataField]
    public string PhaseFiveBehaviourID = "phase_5";
}
