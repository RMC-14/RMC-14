using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Mobs.Events;

namespace Content.Shared._RMC14.Medical.Pain;

public sealed class PainKnockOutSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;


    [ValidatePrototypeId<StatusEffectPrototype>]
    private readonly string _painKnockOutKey = "PainKnockOut";

    public override void Initialize()
    {
        SubscribeLocalEvent<PainKnockOutComponent, ComponentStartup>(OnComponentStart);
        SubscribeLocalEvent<PainKnockOutComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<PainKnockOutComponent, StatusEffectAddedEvent>(OnStatusEffectAdded);
        SubscribeLocalEvent<PainKnockOutComponent, StatusEffectEndedEvent>(OnStatusEffectEnded);
        SubscribeLocalEvent<PainKnockOutComponent, UpdateMobStateEvent>(OnMobStateUpdate);
        SubscribeLocalEvent<PainKnockOutComponent, BeforeThresholdMobStateUpdateEvent>(OnThresholdMobStateChangeCancel);
    }

    private void OnComponentStart(EntityUid uid, PainKnockOutComponent comp, ComponentStartup args)
    {
        if (TryComp<MobStateComponent>(uid, out var state))
        {
            _mobState.UpdateMobState(uid, state);
        }
    }

    private void OnComponentShutdown(EntityUid uid, PainKnockOutComponent comp, ComponentShutdown args)
    {
        if (TryComp<MobStateComponent>(uid, out var state))
        {
            _mobState.UpdateMobState(uid, state);
        }
    }

    private void OnStatusEffectAdded(Entity<PainKnockOutComponent> ent, ref StatusEffectAddedEvent args)
    {
        if (args.Key != _painKnockOutKey)
            return;

        if (TryComp<MobStateComponent>(ent, out var state))
        {
            _mobState.UpdateMobState(ent, state);
        }
    }

    private void OnStatusEffectEnded(Entity<PainKnockOutComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key != _painKnockOutKey)
            return;

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

    private void OnThresholdMobStateChangeCancel(Entity<PainKnockOutComponent> ent, ref BeforeThresholdMobStateUpdateEvent args)
    {
        if (args.ChangeMobStateTo != MobState.Dead)
            args.Cancel();
    }
}
