using Content.Shared._CM14.Xenos.Leap;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Hugger;

public abstract class SharedXenoHuggerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HuggableComponent, InteractHandEvent>(OnHuggableInteractHand);
        SubscribeLocalEvent<HuggableComponent, InteractedNoHandEvent>(OnHuggableInteractNoHand);

        SubscribeLocalEvent<XenoHuggerComponent, XenoLeapHitEvent>(OnHuggerLeapHit);
        SubscribeLocalEvent<XenoHuggerComponent, AfterInteractEvent>(OnHuggerAfterInteract);
        SubscribeLocalEvent<XenoHuggerComponent, AttachHuggerDoAfterEvent>(OnHuggerAttachDoAfter);

        SubscribeLocalEvent<HuggerSpentComponent, MapInitEvent>(OnHuggerSpentMapInit);
        SubscribeLocalEvent<HuggerSpentComponent, UpdateMobStateEvent>(OnHuggerSpentUpdateMobState);

        SubscribeLocalEvent<VictimHuggedComponent, MapInitEvent>(OnVictimHuggedMapInit);
        SubscribeLocalEvent<VictimHuggedComponent, ComponentRemove>(OnVictimHuggedRemoved);
        SubscribeLocalEvent<VictimHuggedComponent, CanSeeAttemptEvent>(OnVictimHuggedCancel);

        SubscribeLocalEvent<VictimBurstComponent, MapInitEvent>(OnVictimBurstMapInit);
        SubscribeLocalEvent<VictimBurstComponent, UpdateMobStateEvent>(OnVictimUpdateMobState);
    }

    private void OnHuggableInteractHand(Entity<HuggableComponent> ent, ref InteractHandEvent args)
    {
        if (TryComp(args.User, out XenoHuggerComponent? hugger) &&
            StartHug((args.User, hugger), args.Target, args.User))
        {
            args.Handled = true;
        }
    }

    private void OnHuggableInteractNoHand(Entity<HuggableComponent> ent, ref InteractedNoHandEvent args)
    {
        if (TryComp(args.User, out XenoHuggerComponent? hugger) &&
            StartHug((args.User, hugger), args.Target, args.User))
        {
            args.Handled = true;
        }
    }

    private void OnHuggerLeapHit(Entity<XenoHuggerComponent> hugger, ref XenoLeapHitEvent args)
    {
        var coordinates = _transform.GetMoverCoordinates(hugger);
        if (coordinates.InRange(EntityManager, _transform, args.Leaping.Origin, hugger.Comp.HugRange))
            Hug(hugger, args.Hit);
    }

    private void OnHuggerAfterInteract(Entity<XenoHuggerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        if (StartHug(ent, args.Target.Value, ent))
            args.Handled = true;
    }

    private void OnHuggerAttachDoAfter(Entity<XenoHuggerComponent> ent, ref AttachHuggerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (Hug(ent, args.Target.Value))
            args.Handled = true;
    }

    protected virtual void HuggerLeapHit(Entity<XenoHuggerComponent> hugger)
    {
    }

    private void OnHuggerSpentMapInit(Entity<HuggerSpentComponent> spent, ref MapInitEvent args)
    {
        if (TryComp(spent, out MobStateComponent? mobState))
            _mobState.UpdateMobState(spent, mobState);
    }

    private void OnHuggerSpentUpdateMobState(Entity<HuggerSpentComponent> spent, ref UpdateMobStateEvent args)
    {
        args.State = MobState.Dead;
    }

    private void OnVictimHuggedMapInit(Entity<VictimHuggedComponent> victim, ref MapInitEvent args)
    {
        victim.Comp.FallOffAt = _timing.CurTime + victim.Comp.FallOffDelay;
        victim.Comp.BurstAt = _timing.CurTime + victim.Comp.BurstDelay;

        _appearance.SetData(victim, victim.Comp.HuggedLayer, true);
    }

    private void OnVictimHuggedRemoved(Entity<VictimHuggedComponent> victim, ref ComponentRemove args)
    {
        _blindable.UpdateIsBlind(victim.Owner);
        _standing.Stand(victim);
    }

    private void OnVictimHuggedCancel<T>(Entity<VictimHuggedComponent> victim, ref T args) where T : CancellableEntityEventArgs
    {
        if (victim.Comp.LifeStage <= ComponentLifeStage.Running && !victim.Comp.Recovered)
            args.Cancel();
    }

    private void OnVictimBurstMapInit(Entity<VictimBurstComponent> burst, ref MapInitEvent args)
    {
        _appearance.SetData(burst, burst.Comp.BurstLayer, true);

        if (TryComp(burst, out MobStateComponent? mobState))
            _mobState.UpdateMobState(burst, mobState);
    }

    private void OnVictimUpdateMobState(Entity<VictimBurstComponent> burst, ref UpdateMobStateEvent args)
    {
        args.State = MobState.Dead;
    }

    private bool StartHug(Entity<XenoHuggerComponent> hugger, EntityUid victim, EntityUid user)
    {
        if (!CanHug(hugger, victim))
            return false;

        var ev = new AttachHuggerDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, hugger.Comp.ManualAttachDelay, ev, hugger, victim)
        {
            BreakOnMove = true
        };
        _doAfter.TryStartDoAfter(doAfter);

        return true;
    }

    private bool CanHug(Entity<XenoHuggerComponent> hugger, EntityUid victim)
    {
        if (!HasComp<HuggableComponent>(victim) ||
            HasComp<HuggerSpentComponent>(hugger) ||
            HasComp<VictimHuggedComponent>(victim))
        {
            return false;
        }

        if (TryComp(victim, out StandingStateComponent? standing) &&
            !_standing.IsDown(victim, standing))
        {
            return false;
        }

        if (_mobState.IsDead(victim))
            return false;

        return true;
    }

    private bool Hug(Entity<XenoHuggerComponent> hugger, EntityUid victim)
    {
        if (!CanHug(hugger, victim))
            return false;

        var victimComp = EnsureComp<VictimHuggedComponent>(victim);
        victimComp.RecoverAt = _timing.CurTime + hugger.Comp.ParalyzeTime;
        _stun.TryParalyze(victim, hugger.Comp.ParalyzeTime, true);

        var container = _container.EnsureContainer<ContainerSlot>(victim, victimComp.ContainerId);
        _container.Insert(hugger.Owner, container);

        _blindable.UpdateIsBlind(victim);
        _appearance.SetData(hugger, victimComp.HuggedLayer, true);

        EnsureComp<HuggerSpentComponent>(hugger);

        HuggerLeapHit(hugger);
        return true;
    }

    public void RefreshIncubationMultipliers(Entity<VictimHuggedComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new GetHuggedIncubationMultiplierEvent(1);
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.IncubationMultiplier = ev.Multiplier;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<VictimHuggedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var hugged, out var xform))
        {
            if (hugged.FallOffAt < time && !hugged.FellOff)
            {
                hugged.FellOff = true;
                _appearance.SetData(uid, hugged.HuggedLayer, false);
                if (_container.TryGetContainer(uid, hugged.ContainerId, out var container))
                    _container.EmptyContainer(container);
            }

            if (hugged.RecoverAt < time && !hugged.Recovered)
            {
                hugged.Recovered = true;
                _blindable.UpdateIsBlind(uid);
            }

            if (_net.IsClient)
                continue;

            if (hugged.BurstAt > time)
            {
                // TODO CM14 make this less effective against late-stage infections, also make this support faster incubation
                if (hugged.IncubationMultiplier < 1)
                {
                    hugged.BurstAt += TimeSpan.FromSeconds(1 - hugged.IncubationMultiplier) * frameTime;
                }

                continue;
            }

            RemCompDeferred<VictimHuggedComponent>(uid);
            Spawn(hugged.BurstSpawn, xform.Coordinates);
            EnsureComp<VictimBurstComponent>(uid);

            _audio.PlayPvs(hugged.BurstSound, uid);
        }
    }
}
