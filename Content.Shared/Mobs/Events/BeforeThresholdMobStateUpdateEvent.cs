namespace Content.Shared.Mobs.Events;

/// <summary>
/// Raised before MobThresholdSystem changes MobState and can cancel it.
/// </summary>
public sealed class BeforeThresholdMobStateUpdateEvent(EntityUid target, MobState changeMobStateTo) : CancellableEntityEventArgs
{
    public readonly EntityUid Target = target;
    public readonly MobState ChangeMobStateTo = changeMobStateTo;
}
