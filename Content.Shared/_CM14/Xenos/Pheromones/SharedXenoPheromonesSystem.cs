using System.Linq;
using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._CM14.Xenos.Pheromones;

public abstract class SharedXenoPheromonesSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private readonly TimeSpan _pheromonePlasmaUseDelay = TimeSpan.FromSeconds(1);

    private readonly HashSet<EntityUid>[] _oldReceivers = Enum.GetValues<XenoPheromones>()
        .Select(_ => new HashSet<EntityUid>())
        .ToArray();

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

        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, ComponentGetStateAttemptEvent>(OnComponentGetStateAttempt);
        SubscribeLocalEvent<XenoWardingPheromonesComponent, ComponentGetStateAttemptEvent>(OnComponentGetStateAttempt);
        SubscribeLocalEvent<XenoRecoveryPheromonesComponent, ComponentGetStateAttemptEvent>(OnComponentGetStateAttempt);
        SubscribeLocalEvent<XenoPheromonesComponent, ComponentGetStateAttemptEvent>(OnComponentGetStateAttempt);
        SubscribeLocalEvent<XenoActivePheromonesComponent, ComponentGetStateAttemptEvent>(OnComponentGetStateAttempt);
        SubscribeLocalEvent<XenoComponent, ComponentStartup>(OnXenoStartup);

        // TODO CM14 reduce crit damage
        SubscribeLocalEvent<XenoWardingPheromonesComponent, UpdateMobStateEvent>(OnWardingUpdateMobState,
            after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<XenoWardingPheromonesComponent, ComponentRemove>(OnWardingRemove);
        SubscribeLocalEvent<XenoWardingPheromonesComponent, DamageModifyEvent>(OnWardingDamageModify);

        // TODO CM14 stack slash damage
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, GetMeleeDamageEvent>(OnFrenzyGetMeleeDamage);
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, RefreshMovementSpeedModifiersEvent>(OnFrenzyMovementSpeedModifiers);

        Subs.BuiEvents<XenoPheromonesComponent>(XenoPheromonesUI.Key, subs =>
        {
            subs.Event<XenoPheromonesChosenBuiMsg>(OnXenoPheromonesChosenBui);
        });
    }

    private void OnComponentGetStateAttempt<T>(EntityUid uid, T comp, ref ComponentGetStateAttemptEvent ev)
    {
        // Apparently this happens in replays
        if (ev.Player is null)
            return;

        ev.Cancelled = !HasComp<XenoComponent>(ev.Player.AttachedEntity);
    }

    private void OnXenoStartup(EntityUid uid, XenoComponent comp, ref ComponentStartup ev)
    {
        DirtyPheromones<XenoPheromonesComponent>();
        DirtyPheromones<XenoWardingPheromonesComponent>();
        DirtyPheromones<XenoFrenzyPheromonesComponent>();
        DirtyPheromones<XenoRecoveryPheromonesComponent>();
    }

    private void DirtyPheromones<T>() where T: IComponent
    {
        var pheromoneQuery = EntityQueryEnumerator<T>();
        while (pheromoneQuery.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
    }

    private void OnXenoPheromonesAction(Entity<XenoPheromonesComponent> xeno, ref XenoPheromonesActionEvent args)
    {
        args.Handled = true;
        if (RemComp<XenoActivePheromonesComponent>(xeno))
        {
            _actions.SetToggled(args.Action, false);
            _popup.PopupClient(Loc.GetString("cm-xeno-pheromones-stop"), xeno, xeno);
            return;
        }

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

        EnsureComp<XenoActivePheromonesComponent>(xeno).Pheromones = args.Pheromones;
        xeno.Comp.NextPheromonesPlasmaUse = _timing.CurTime + _pheromonePlasmaUseDelay;

        var popup = Loc.GetString("cm-xeno-pheromones-start", ("pheromones", args.Pheromones.ToString()));
        _popup.PopupClient(popup, xeno, xeno);

        _ui.CloseUi(xeno.Owner, XenoPheromonesUI.Key, xeno);
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

        var wardingThreshold = threshold.Value * (1.1 * warding.Comp.Multiplier);
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

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
        _pheromonesJob.Pheromones.Clear();

        while (query.MoveNext(out var uid, out var active, out var pheromones, out var xform))
        {
            // We'll only update pheromones receivers whenever plasma gets used.
            // This avoids us having to do lookups every tick.
            if (_timing.CurTime < pheromones.NextPheromonesPlasmaUse)
            {
                continue;
            }

            pheromones.NextPheromonesPlasmaUse += _pheromonePlasmaUseDelay;
            if (!_xenoPlasma.TryRemovePlasma(uid, pheromones.PheromonesPlasmaUpkeep))
            {
                RemCompDeferred<XenoActivePheromonesComponent>(uid);
                continue;
            }

            Dirty(uid, pheromones);
            _pheromonesJob.Pheromones.Add((uid, active, pheromones, xform));
        }

        // Bump _receivers to match _pheromones.
        // Can't use an ObjectPool because Sandboxing in shared and this avoids
        // re-allocating the sets every single tick.
        // Downside is more memory overhead, especially if pheromones drops off.
        for (var i = _pheromonesJob.Receivers.Count; i < _pheromonesJob.Pheromones.Count; i++)
        {
            _pheromonesJob.Receivers.Add(new HashSet<Entity<XenoComponent>>());
        }

        _parallel.ProcessNow(_pheromonesJob, _pheromonesJob.Pheromones.Count);
        DebugTools.Assert(_pheromonesJob.Receivers.Count >= _pheromonesJob.Pheromones.Count);

        for (var i = 0; i < _pheromonesJob.Pheromones.Count; i++)
        {
            var (uid, active, pheromones, xform) = _pheromonesJob.Pheromones[i];
            var receivers = _pheromonesJob.Receivers[i];

            switch (active.Pheromones)
            {
                case XenoPheromones.Recovery:
                    foreach (var receiver in receivers)
                    {
                        oldRecovery.Remove(receiver);
                        var recovery = EnsureComp<XenoRecoveryPheromonesComponent>(receiver);
                        AssignMaxMultiplier(ref recovery.Multiplier, pheromones.PheromonesMultiplier);
                    }

                    break;
                case XenoPheromones.Warding:
                    foreach (var receiver in receivers)
                    {
                        oldWarding.Remove(receiver);
                        var warding = EnsureComp<XenoWardingPheromonesComponent>(receiver);
                        AssignMaxMultiplier(ref warding.Multiplier, pheromones.PheromonesMultiplier);
                    }

                    break;
                case XenoPheromones.Frenzy:
                    foreach (var receiver in receivers)
                    {
                        oldFrenzy.Remove(receiver);
                        var frenzy = EnsureComp<XenoFrenzyPheromonesComponent>(receiver);
                        AssignMaxMultiplier(ref frenzy.Multiplier, pheromones.PheromonesMultiplier);

                        _movementSpeed.RefreshMovementSpeedModifiers(receiver);
                    }

                    break;
            }
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
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }
    }

    private record struct PheromonesJob() : IParallelRobustJob
    {
        public EntityLookupSystem Lookup;

        public ValueList<(
            EntityUid Uid,
            XenoActivePheromonesComponent Active,
            XenoPheromonesComponent Pheromones,
            TransformComponent Xform
            )> Pheromones = new();

        public ValueList<HashSet<Entity<XenoComponent>>> Receivers = new();

        public void Execute(int index)
        {
            var (_, _, pheromones, xform) = Pheromones[index];
            ref var receivers = ref Receivers[index];
            receivers.Clear();
            Lookup.GetEntitiesInRange(xform.Coordinates, pheromones.PheromonesRange, receivers);
        }
    }
}
