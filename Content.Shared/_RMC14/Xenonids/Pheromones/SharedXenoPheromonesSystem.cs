using System.Linq;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Xenonids.CriticalGrace;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Stab;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Collections;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.Pheromones;

public abstract class SharedXenoPheromonesSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _weeds = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private readonly TimeSpan _pheromonePlasmaUseDelay = TimeSpan.FromSeconds(1);

    private readonly HashSet<EntityUid>[] _oldReceivers = Enum.GetValues<XenoPheromones>()
        .Select(_ => new HashSet<EntityUid>())
        .ToArray();
    private readonly HashSet<EntityUid> _refreshSpeeds = new();

    private EntityQuery<DamageableComponent> _damageableQuery;

    private PheromonesJob _pheromonesJob;

    public override void Initialize()
    {
        base.Initialize();

        _pheromonesJob = new PheromonesJob(_entityLookup);

        _damageableQuery = GetEntityQuery<DamageableComponent>();

        SubscribeLocalEvent<XenoPheromonesComponent, XenoPheromonesActionEvent>(OnXenoPheromonesAction);

        SubscribeLocalEvent<XenoWardingPheromonesComponent, UpdateMobStateEvent>(OnWardingUpdateMobState,
            after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<XenoWardingPheromonesComponent, ComponentRemove>(OnWardingRemove);
        SubscribeLocalEvent<XenoWardingPheromonesComponent, DamageStateCritBeforeDamageEvent>(OnWardingDamageCritModify);
        SubscribeLocalEvent<XenoWardingPheromonesComponent, GetCriticalGraceTimeEvent>(OnWardingGetGraceTime);

        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, ComponentRemove>(OnFrenzyRemove);
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, GetMeleeDamageEvent>(OnFrenzyGetMeleeDamage);
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, RMCGetTailStabBonusDamageEvent>(OnFrenzyGetTailStabDamage);
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, RefreshMovementSpeedModifiersEvent>(OnFrenzyMovementSpeedModifiers);
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, PullStartedMessage>(OnFrenzyPullStarted, after: [typeof(RMCPullingSystem)] );
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, PullStoppedMessage>(OnFrenzyPullStopped, after: [typeof(RMCPullingSystem)] );

        SubscribeLocalEvent<XenoActivePheromonesComponent, MobStateChangedEvent>(OnActiveMobStateChanged);

        Subs.BuiEvents<XenoPheromonesComponent>(XenoPheromonesUI.Key, subs =>
        {
            subs.Event<XenoPheromonesChosenBuiMsg>(OnXenoPheromonesChosenBui);
        });
    }

    private void OnActiveMobStateChanged(Entity<XenoActivePheromonesComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
            DeactivatePheromones(ent.Owner);
    }

    private void OnXenoPheromonesAction(Entity<XenoPheromonesComponent> xeno, ref XenoPheromonesActionEvent args)
    {
        args.Handled = true;
        DeactivatePheromones((xeno, xeno));
        _ui.TryOpenUi(xeno.Owner, XenoPheromonesUI.Key, xeno);
    }

    private void OnXenoPheromonesChosenBui(Entity<XenoPheromonesComponent> xeno, ref XenoPheromonesChosenBuiMsg args)
    {
        if (!Enum.IsDefined(args.Pheromones) ||
            !_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PheromonesPlasmaCost))
        {
            return;
        }

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoPheromonesActionEvent>(xeno))
        {
            _actions.SetToggled(action.AsNullable(), true);
        }

        var popup = Loc.GetString("cm-xeno-pheromones-start", ("pheromones", args.Pheromones.ToString()));
        _popup.PopupClient(popup, xeno, xeno);

        _ui.CloseUi(xeno.Owner, XenoPheromonesUI.Key, xeno);

        if (_net.IsClient)
            return;

        xeno.Comp.NextPheromonesPlasmaUse = _timing.CurTime + _pheromonePlasmaUseDelay;
        Dirty(xeno);

        var active = EnsureComp<XenoActivePheromonesComponent>(xeno);
        active.Pheromones = args.Pheromones;
        Dirty(xeno, active);

        var ev = new XenoPheromonesActivatedEvent();
        RaiseLocalEvent(xeno, ref ev);

        _entityLookup.GetEntitiesInRange(xeno.Owner.ToCoordinates(), xeno.Comp.PheromonesRange, active.Receivers);
    }

    private void OnWardingUpdateMobState(Entity<XenoWardingPheromonesComponent> warding, ref UpdateMobStateEvent args)
    {
        if (args.Component.CurrentState == MobState.Dead ||
            args.State != MobState.Dead ||
            !_damageableQuery.TryGetComponent(warding, out var damageable) ||
            !_mobThreshold.TryGetDeadThreshold(warding, out var threshold) ||
            !_mobState.HasState(warding, MobState.Critical))
        {
            return;
        }

        var wardingThreshold = threshold.Value + (1 + 20 * warding.Comp.Multiplier);
        if (damageable.TotalDamage >= wardingThreshold)
            return;

        args.State = MobState.Critical;
    }

    private void OnWardingGetGraceTime(Entity<XenoWardingPheromonesComponent> warding, ref GetCriticalGraceTimeEvent args)
    {
        args.Time += TimeSpan.FromSeconds(1) * Math.Max(warding.Comp.Multiplier.Int() - 1, 0);
    }

    private void OnWardingRemove(Entity<XenoWardingPheromonesComponent> ent, ref ComponentRemove args)
    {
        if (TryComp(ent, out MobThresholdsComponent? thresholds))
            _mobThreshold.VerifyThresholds(ent, thresholds);
    }

    private void OnWardingDamageCritModify(Entity<XenoWardingPheromonesComponent> warding, ref DamageStateCritBeforeDamageEvent args)
    {
        if (_rmcFlammable.IsOnFire(warding.Owner))
            return;

        if (!TryComp<XenoRegenComponent>(warding, out var xeno) || (!xeno.HealOffWeeds && !_weeds.IsOnFriendlyWeeds(warding.Owner)))
        {
            var damageReduct = _rmcDamageable.DistributeDamageCached(warding.Owner, warding.Comp.CritDamageGroup, warding.Comp.Multiplier * 0.25);
            args.Damage -= damageReduct;
        }
        else
            args.Damage = -_rmcDamageable.DistributeDamageCached(warding.Owner, warding.Comp.CritDamageGroup, warding.Comp.Multiplier * 0.5f);
    }

    private void OnFrenzyRemove(Entity<XenoFrenzyPheromonesComponent> ent, ref ComponentRemove args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnFrenzyGetMeleeDamage(Entity<XenoFrenzyPheromonesComponent> frenzy, ref GetMeleeDamageEvent args)
    {
        args.Damage += new DamageSpecifier(_protoManager.Index(frenzy.Comp.DamageGroup), frenzy.Comp.AttackDamageAddPerMult * frenzy.Comp.Multiplier);
    }

    private void OnFrenzyGetTailStabDamage(Entity<XenoFrenzyPheromonesComponent> frenzy, ref RMCGetTailStabBonusDamageEvent args)
    {
        //1.2 = tailstab attack mult
        args.Damage += new DamageSpecifier(_protoManager.Index(frenzy.Comp.DamageGroup), frenzy.Comp.AttackDamageAddPerMult * frenzy.Comp.Multiplier * 1.2);
    }

    private void OnFrenzyMovementSpeedModifiers(Entity<XenoFrenzyPheromonesComponent> frenzy, ref RefreshMovementSpeedModifiersEvent args)
    {
        var speed = 1 + (frenzy.Comp.MovementSpeedModifier * frenzy.Comp.Multiplier).Float();
        if (HasComp<PullingSlowedComponent>(frenzy.Owner))
            speed = 1 + (frenzy.Comp.PullMovementSpeedModifier * frenzy.Comp.Multiplier).Float();

        args.ModifySpeed(speed, speed);
    }

    private void OnFrenzyPullStarted(Entity<XenoFrenzyPheromonesComponent> frenzy, ref PullStartedMessage args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(args.PullerUid);
    }

    private void OnFrenzyPullStopped(Entity<XenoFrenzyPheromonesComponent> frenzy, ref PullStoppedMessage args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(args.PullerUid);
    }

    private void AssignMaxMultiplier(ref FixedPoint2 a, FixedPoint2 b)
    {
        a = FixedPoint2.Max(a, b);
    }

    public void DeactivatePheromones(Entity<XenoPheromonesComponent?> xeno)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return;

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoPheromonesActionEvent>(xeno))
        {
            _actions.SetToggled(action.AsNullable(), false);
        }

        if (!HasComp<XenoActivePheromonesComponent>(xeno))
            return;

        if (_net.IsServer)
            RemComp<XenoActivePheromonesComponent>(xeno);

        _popup.PopupClient(Loc.GetString("cm-xeno-pheromones-stop"), xeno, xeno);
        var pheroEv = new XenoPheromonesDeactivatedEvent();
        RaiseLocalEvent(xeno, ref pheroEv);
    }

    public void TryActivatePheromonesObject(Entity<XenoPheromonesObjectComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (_net.IsClient)
            return;

        if (!TryComp(ent, out XenoPheromonesComponent? comp))
            return;

        var active = EnsureComp<XenoActivePheromonesComponent>(ent);
        active.Pheromones = ent.Comp.Pheromones;
        Dirty(ent, active);

        _entityLookup.GetEntitiesInRange(ent.Owner.ToCoordinates(), comp.PheromonesRange, active.Receivers);

        var ev = new XenoPheromonesActivatedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private bool KeepWarding(EntityUid ent, XenoWardingPheromonesComponent warding, FixedPoint2 newWardMult)
    {
        if ((!_mobThreshold.TryGetIncapThreshold(ent, out var critThres) ||
             !_damageableQuery.TryGetComponent(ent, out var damageable)))
            return false;

        if (damageable.TotalDamage < critThres)
            return false;

        if (newWardMult > warding.Multiplier)
            return false;

        if ((TryComp<XenoRegenComponent>(ent, out var xeno) && xeno.HealOffWeeds) || !_weeds.IsOnFriendlyWeeds(ent))
            return false;

        return true;
    }

    public string? GetPheroSuffix(Entity<XenoPheromonesComponent?> xeno)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return null;

        return xeno.Comp.PheroSuffix;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var oldRecovery = _oldReceivers[(int) XenoPheromones.Recovery];
        oldRecovery.Clear();

        var recoveryQuery = EntityQueryEnumerator<XenoRecoveryPheromonesComponent>();
        while (recoveryQuery.MoveNext(out var uid, out var recovery))
        {
            oldRecovery.Add(uid);
            recovery.Multiplier = 0;
        }

        var oldWarding = _oldReceivers[(int) XenoPheromones.Warding];
        oldWarding.Clear();

        var wardingQuery = EntityQueryEnumerator<XenoWardingPheromonesComponent>();
        while (wardingQuery.MoveNext(out var uid, out var warding))
        {
            //Don't clear if we would die
            if (!_mobState.IsDead(uid) && KeepWarding(uid, warding, 0))
                continue;

            oldWarding.Add(uid);
            warding.Multiplier = 0;
        }

        var oldFrenzy = _oldReceivers[(int) XenoPheromones.Frenzy];
        oldFrenzy.Clear();

        var frenzyQuery = EntityQueryEnumerator<XenoFrenzyPheromonesComponent>();
        while (frenzyQuery.MoveNext(out var uid, out var frenzy))
        {
            oldFrenzy.Add(uid);
            frenzy.Multiplier = 0;
        }

        var query = EntityQueryEnumerator<XenoActivePheromonesComponent, XenoPheromonesComponent, TransformComponent>();
        _pheromonesJob.Receivers.Clear();
        _pheromonesJob.Pheromones.Clear();
        _refreshSpeeds.Clear();

        while (query.MoveNext(out var uid, out var active, out var pheromones, out var xform))
        {
            _pheromonesJob.Pheromones.Add((uid, active, pheromones, xform));

            // We'll only update pheromones receivers whenever plasma gets used.
            // This avoids us having to do lookups every tick.
            if (_timing.CurTime < pheromones.NextPheromonesPlasmaUse)
            {
                _pheromonesJob.Receivers.Add((false, active.Receivers));
                continue;
            }

            pheromones.NextPheromonesPlasmaUse = _timing.CurTime + _pheromonePlasmaUseDelay;
            Dirty(uid, pheromones);

            if (!HasComp<XenoPheromonesObjectComponent>(uid) &&
                pheromones.PheromonesPlasmaUpkeep > 0 &&
                !_xenoPlasma.TryRemovePlasma(uid, pheromones.PheromonesPlasmaUpkeep))
            {
                _pheromonesJob.Pheromones.RemoveAt(_pheromonesJob.Pheromones.Count - 1);
                RemCompDeferred<XenoActivePheromonesComponent>(uid);
                continue;
            }

            _pheromonesJob.Receivers.Add((true, active.Receivers));
            Dirty(uid, pheromones);
        }

        // Okay so essentially:
        // 1. We only run lookups on every plasma usage and cache that for the next second
        // 2. We run lookups in parallel
        // 3. Because the lookups are cached for a second we need to make sure
        //    none of the target entities are deleted in the interim.

        _parallel.ProcessNow(_pheromonesJob, _pheromonesJob.Pheromones.Count);
        DebugTools.Assert(_pheromonesJob.Receivers.Count >= _pheromonesJob.Pheromones.Count);

        for (var i = 0; i < _pheromonesJob.Pheromones.Count; i++)
        {
            var (_, active, pheromones, _) = _pheromonesJob.Pheromones[i];
            var receivers = _pheromonesJob.Receivers[i].Receivers;

            switch (active.Pheromones)
            {
                case XenoPheromones.Recovery:
                    foreach (var receiver in receivers)
                    {
                        if (Deleted(receiver) || _mobState.IsDead(receiver))
                            continue;

                        if (receiver.Comp.IgnorePheromones == XenoPheromones.Recovery)
                            continue;

                        oldRecovery.Remove(receiver);
                        var recovery = EnsureComp<XenoRecoveryPheromonesComponent>(receiver);
                        AssignMaxMultiplier(ref recovery.Multiplier, pheromones.PheromonesMultiplier);
                    }

                    break;
                case XenoPheromones.Warding:
                    foreach (var receiver in active.Receivers)
                    {
                        if (Deleted(receiver) || _mobState.IsDead(receiver))
                            continue;

                        if (receiver.Comp.IgnorePheromones == XenoPheromones.Warding)
                            continue;

                        oldWarding.Remove(receiver);
                        var warding = EnsureComp<XenoWardingPheromonesComponent>(receiver);
                        AssignMaxMultiplier(ref warding.Multiplier, pheromones.PheromonesMultiplier);
                    }

                    break;
                case XenoPheromones.Frenzy:
                    foreach (var receiver in active.Receivers)
                    {
                        if (Deleted(receiver) || _mobState.IsDead(receiver))
                            continue;

                        if (receiver.Comp.IgnorePheromones == XenoPheromones.Frenzy)
                            continue;

                        oldFrenzy.Remove(receiver);
                        var frenzy = EnsureComp<XenoFrenzyPheromonesComponent>(receiver);
                        var old = frenzy.Multiplier;
                        AssignMaxMultiplier(ref frenzy.Multiplier, pheromones.PheromonesMultiplier);

                        if (frenzy.Multiplier != old)
                            _refreshSpeeds.Add(receiver);
                    }

                    break;
            }
        }

        foreach (var uid in _refreshSpeeds)
        {
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }

        foreach (var uid in oldRecovery)
        {
            RemComp<XenoRecoveryPheromonesComponent>(uid);
        }

        foreach (var uid in oldWarding)
        {
            RemComp<XenoWardingPheromonesComponent>(uid);
        }

        foreach (var uid in oldFrenzy)
        {
            RemComp<XenoFrenzyPheromonesComponent>(uid);
        }
    }

    private record struct PheromonesJob(EntityLookupSystem Lookup) : IParallelRobustJob
    {
        // Bumped this because most receivers aren't going to be updating on any individual tick.
        public int BatchSize => 8;

        public ValueList<(
            EntityUid Uid,
            XenoActivePheromonesComponent Active,
            XenoPheromonesComponent Pheromones,
            TransformComponent Xform
            )> Pheromones = new();

        public ValueList<(bool Update, HashSet<Entity<XenoComponent>> Receivers)> Receivers = new();

        public void Execute(int index)
        {
            ref var receivers = ref Receivers[index];

            if (!receivers.Update)
                return;

            var (_, _, pheromones, xform) = Pheromones[index];
            receivers.Receivers.Clear();
            // TODO RMC14 make this use a component that gets added when alive, removed when dead, and respects ignored pheromones
            Lookup.GetEntitiesInRange(xform.Coordinates, pheromones.PheromonesRange, receivers.Receivers);
        }
    }
}
