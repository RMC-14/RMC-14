using Content.Shared.StatusEffect;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;

namespace Content.Shared._RMC14.Medical.Pain;

public sealed class PainKnockOutSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;


    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string PainKnockOutKey = "PainKnockOut";

    public override void Initialize()
    {
        SubscribeLocalEvent<PainKnockOutComponent, ComponentStartup>(OnComponentStart);
        SubscribeLocalEvent<PainKnockOutComponent, UpdateMobStateEvent>(OnMobStateUpdate);
        SubscribeLocalEvent<PainKnockOutComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnComponentStart(EntityUid uid, PainKnockOutComponent comp, ComponentStartup args)
    {
        if(TryComp<MobStateComponent>(uid, out var state))
        {
            _mobState.UpdateMobState(uid, state);
        }
    }

    // TODO remove it when UpdateMobStateEvent would work propertly with Critical
    private void OnMobStateChanged(Entity<PainKnockOutComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.OldMobState == MobState.Dead || args.NewMobState == MobState.Dead)
            return;
        _mobState.ChangeMobState(ent, MobState.Critical, args.Component);
    }

    private void OnMobStateUpdate(Entity<PainKnockOutComponent> ent, ref UpdateMobStateEvent args)
    {
        if (args.State == MobState.Dead || args.Component.CurrentState == MobState.Dead)
            return;
        args.State = MobState.Critical;
    }
}
