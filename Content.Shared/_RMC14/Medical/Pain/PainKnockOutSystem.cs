using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Mobs.Events;
using Content.Shared.Rejuvenate;

namespace Content.Shared._RMC14.Medical.Pain;

public sealed class PainKnockOutSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;


    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string _painKnockOutKey = "PainKnockOut";

    public override void Initialize()
    {
        SubscribeLocalEvent<PainKnockOutComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<PainKnockOutComponent, StatusEffectAddedEvent>(OnStatusEffectAdded);
        SubscribeLocalEvent<PainKnockOutComponent, StatusEffectEndedEvent>(OnStatusEffectEnded);
        SubscribeLocalEvent<PainKnockOutComponent, UpdateMobStateEvent>(OnMobStateUpdate);
    }

    // temporarily making the Alive state unavailable, we save the previous Critical threshold to the PainKnockOutComponent
    private void BlockAliveState(EntityUid uid, PainKnockOutComponent knockout, MobThresholdsComponent thresholds)
    {
        if (knockout.IsAlreadySaved)
            return;

        knockout.IsAlreadySaved = true;
        knockout.previousCritThreshold = _mobThresholds.GetThresholdForState(uid, MobState.Critical, thresholds);
        var alive = _mobThresholds.GetThresholdForState(uid, MobState.Alive, thresholds);
        knockout.previousAliveThreshold = alive;
        _mobThresholds.SetMobStateThreshold(uid, alive + 1, MobState.Critical, thresholds); // +1 needed to rejuvenation working propertly
        Dirty(uid, knockout);
    }

    // make Alive state available again
    private void EnableAliveState(EntityUid uid, PainKnockOutComponent knockout, MobThresholdsComponent thresholds)
    {
        if (!knockout.IsAlreadySaved)
            return;

        knockout.IsAlreadySaved = false;
        _mobThresholds.SetMobStateThreshold(uid, knockout.previousCritThreshold, MobState.Critical, thresholds);
        _mobThresholds.SetMobStateThreshold(uid, knockout.previousAliveThreshold, MobState.Alive, thresholds);
        Dirty(uid, knockout);
    }

    private void OnComponentRemove(EntityUid uid, PainKnockOutComponent knockout, ref ComponentRemove args)
    {
        if (TryComp<MobThresholdsComponent>(uid, out var thresholds))
        {
            EnableAliveState(uid, knockout, thresholds);
        }
    }

    private void OnStatusEffectAdded(Entity<PainKnockOutComponent> ent, ref StatusEffectAddedEvent args)
    {
        if (args.Key != _painKnockOutKey)
            return;

        if (TryComp<MobThresholdsComponent>(ent, out var thresholds))
        {
            BlockAliveState(ent.Owner, ent.Comp, thresholds);
        }

        if (TryComp<MobStateComponent>(ent, out var state) && state.CurrentState != MobState.Dead)
        {
            _mobState.ChangeMobState(ent, MobState.Critical, state);
        }
    }

    private void OnStatusEffectEnded(Entity<PainKnockOutComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key != _painKnockOutKey)
            return;

        if (TryComp<MobThresholdsComponent>(ent, out var thresholds))
        {
            EnableAliveState(ent.Owner, ent.Comp, thresholds);
        }

        if (TryComp<MobStateComponent>(ent, out var state))
        {
            _mobState.UpdateMobState(ent, state);
        }
    }

    private void OnMobStateUpdate(Entity<PainKnockOutComponent> ent, ref UpdateMobStateEvent args)
    {
        if (args.State == MobState.Dead || args.Component.CurrentState == MobState.Dead)
            return;
        args.State = MobState.Critical;
    }
}
