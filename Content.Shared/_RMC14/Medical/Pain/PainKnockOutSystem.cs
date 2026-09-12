using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Pain;

public sealed class PainKnockOutSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    private static readonly ProtoId<StatusEffectPrototype> PainKnockOut = "PainKnockOut";

    public override void Initialize()
    {
        SubscribeLocalEvent<PainKnockOutComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<PainKnockOutComponent, StatusEffectAddedEvent>(OnStatusEffectAdded);
        SubscribeLocalEvent<PainKnockOutComponent, StatusEffectEndedEvent>(OnStatusEffectEnded);
        SubscribeLocalEvent<PainKnockOutComponent, UpdateMobStateEvent>(OnMobStateUpdate);
    }

    // temporarily making the Alive state unavailable, we save the previous Critical threshold to the PainKnockOutComponent
    private void BlockAliveState(Entity<PainKnockOutComponent> ent, MobThresholdsComponent thresholds)
    {
        if (ent.Comp.IsAlreadySaved)
            return;

        ent.Comp.IsAlreadySaved = true;
        ent.Comp.PreviousCritThreshold = _mobThresholds.GetThresholdForState(ent, MobState.Critical, thresholds);
        var alive = _mobThresholds.GetThresholdForState(ent, MobState.Alive, thresholds);
        ent.Comp.PreviousAliveThreshold = alive;
        _mobThresholds.SetMobStateThreshold(ent, alive + 1, MobState.Critical, thresholds); // +1 needed to make rejuvenation work properly
        Dirty(ent);
    }

    // make Alive state available again
    private void EnableAliveState(Entity<PainKnockOutComponent> ent, MobThresholdsComponent thresholds)
    {
        if (!ent.Comp.IsAlreadySaved)
            return;

        ent.Comp.IsAlreadySaved = false;
        _mobThresholds.SetMobStateThreshold(ent, ent.Comp.PreviousCritThreshold, MobState.Critical, thresholds);
        _mobThresholds.SetMobStateThreshold(ent, ent.Comp.PreviousAliveThreshold, MobState.Alive, thresholds);
        Dirty(ent);
    }

    private void OnComponentRemove(Entity<PainKnockOutComponent> ent, ref ComponentRemove args)
    {
        if (TryComp<MobThresholdsComponent>(ent, out var thresholds))
        {
            EnableAliveState(ent, thresholds);
        }
    }

    private void OnStatusEffectAdded(Entity<PainKnockOutComponent> ent, ref StatusEffectAddedEvent args)
    {
        if (args.Key != PainKnockOut)
            return;

        if (TryComp<MobThresholdsComponent>(ent, out var thresholds))
        {
            BlockAliveState(ent, thresholds);
        }

        if (TryComp<MobStateComponent>(ent, out var state) && state.CurrentState != MobState.Dead)
        {
            _mobState.ChangeMobState(ent, MobState.Critical, state);
        }
    }

    private void OnStatusEffectEnded(Entity<PainKnockOutComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key != PainKnockOut)
            return;

        if (TryComp<MobThresholdsComponent>(ent, out var thresholds))
        {
            EnableAliveState(ent, thresholds);
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
