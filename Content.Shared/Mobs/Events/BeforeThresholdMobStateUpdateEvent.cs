namespace Content.Shared.Mobs.Events;

/// <summary>
/// Raised before MobThresholdSystem changes MobState and can cancel it.
/// </summary>
public sealed class BeforeThresholdMobStateUpdateEvent(EntityUid target, MobState changeMobStateFrom, MobState changeMobStateTo) : CancellableEntityEventArgs
{
    public readonly EntityUid Target = target;
    public readonly MobState ChangeMobStateFrom = changeMobStateFrom;
    public readonly MobState ChangeMobStateTo = changeMobStateTo;
}
