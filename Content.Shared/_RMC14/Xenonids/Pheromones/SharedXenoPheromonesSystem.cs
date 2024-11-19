using System.Linq;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Collections;
using Robust.Shared.Network;
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

        _pheromonesJob = new PheromonesJob()
        {
            Lookup = _entityLookup,
        };

        _damageableQuery = GetEntityQuery<DamageableComponent>();

        SubscribeLocalEvent<XenoPheromonesComponent, XenoPheromonesActionEvent>(OnXenoPheromonesAction);

        // TODO RMC14 reduce crit damage
        SubscribeLocalEvent<XenoWardingPheromonesComponent, UpdateMobStateEvent>(OnWardingUpdateMobState,
            after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<XenoWardingPheromonesComponent, ComponentRemove>(OnWardingRemove);
        SubscribeLocalEvent<XenoWardingPheromonesComponent, DamageModifyEvent>(OnWardingDamageModify);

        // TODO RMC14 stack slash damage
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, ComponentRemove>(OnFrenzyRemove);
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, GetMeleeDamageEvent>(OnFrenzyGetMeleeDamage);
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, RefreshMovementSpeedModifiersEvent>(OnFrenzyMovementSpeedModifiers);

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
        if (!Enum.IsDefined(typeof(XenoPheromones), args.Pheromones) ||
            !_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PheromonesPlasmaCost))
        {
            return;
        }

        foreach (var (actionId, action) in _actions.GetActions(xeno))
        {
            if (action.BaseEvent is XenoPheromonesActionEvent)
                _actions.SetToggled(actionId, true);
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
            !_mobThreshold.TryGetDeadThreshold(warding, out var threshold))
        {
            return;
        }

        // TODO RMC14 crit grace period
        // TODO RMC14 20
        var wardingThreshold = threshold.Value + (1 + 40 * warding.Comp.Multiplier);
        if (damageable.TotalDamage >= wardingThreshold)
            return;

        args.State = MobState.Critical;
    }

    private void OnWardingRemove(Entity<XenoWardingPheromonesComponent> ent, ref ComponentRemove args)
    {
        if (TryComp(ent, out MobThresholdsComponent? thresholds))
            _mobThreshold.VerifyThresholds(ent, thresholds);
    }

    private void OnWardingDamageModify(Entity<XenoWardingPheromonesComponent> warding, ref DamageModifyEvent args)
    {
        var damage = args.Damage.DamageDict;
        var multiplier = FixedPoint2.Max(1 - 0.25 * warding.Comp.Multiplier, 0);

        foreach (var type in warding.Comp.DamageTypes)
        {
            if (args.Damage.DamageDict.TryGetValue(type, out var amount))
                damage[type] = amount * multiplier;
        }
    }

    private void OnFrenzyRemove(Entity<XenoFrenzyPheromonesComponent> ent, ref ComponentRemove args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnFrenzyGetMeleeDamage(Entity<XenoFrenzyPheromonesComponent> frenzy, ref GetMeleeDamageEvent args)
    {
        args.Modifiers.Add(new DamageModifierSet
        {
            Coefficients = frenzy.Comp.DamageTypes.ToDictionary(key => key.ToString(), _ => frenzy.Comp.AttackDamageModifier)
        });
    }

    private void OnFrenzyMovementSpeedModifiers(Entity<XenoFrenzyPheromonesComponent> frenzy, ref RefreshMovementSpeedModifiersEvent args)
    {
        var speed = 1 + (frenzy.Comp.MovementSpeedModifier * frenzy.Comp.Multiplier).Float();
        args.ModifySpeed(speed, speed);
    }

    private void AssignMaxMultiplier(ref FixedPoint2 a, FixedPoint2 b)
    {
        a = FixedPoint2.Max(a, b);
    }

    public void DeactivatePheromones(Entity<XenoPheromonesComponent?> xeno)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return;

        foreach (var (actionId, action) in _actions.GetActions(xeno))
        {
            if (action.BaseEvent is XenoPheromonesActionEvent)
                _actions.SetToggled(actionId, false);
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

    private record struct PheromonesJob() : IParallelRobustJob
    {
        // Bumped this because most receivers aren't going to be updating on any individual tick.
        public int BatchSize => 8;

        public EntityLookupSystem Lookup;

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
            Lookup.GetEntitiesInRange(xform.Coordinates, pheromones.PheromonesRange, receivers.Receivers);
        }
    }
}
