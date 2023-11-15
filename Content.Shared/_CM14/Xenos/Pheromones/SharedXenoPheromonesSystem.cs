using System.Linq;
using Content.Shared._CM14.Xenos.Construction;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Pheromones;

public abstract class SharedXenoPheromonesSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruction = default!;

    // TODO CM14 move all of this to a component
    [ValidatePrototypeId<DamageTypePrototype>]
    private const string FrenzyDamageTypeOne = "Blunt";

    [ValidatePrototypeId<DamageTypePrototype>]
    private const string FrenzyDamageTypeTwo = "Slash";

    [ValidatePrototypeId<DamageTypePrototype>]
    private const string FrenzyDamageTypeThree = "Piercing";

    [ValidatePrototypeId<DamageTypePrototype>]
    private const string WardingDamageTypeOne = "Bloodloss";

    [ValidatePrototypeId<DamageTypePrototype>]
    private const string WardingDamageTypeTwo = "Asphyxiation";

    private static readonly FixedPoint2 RecoveryHealthRegen = 0.5;
    private static readonly FixedPoint2 RecoveryPlasmaRegen = 1.5;
    private static readonly TimeSpan RecoveryDelay = TimeSpan.FromSeconds(1);
    private static readonly float FrenzyAttackDamageModifier = 1.1f;
    private static readonly FixedPoint2 FrenzyMovementSpeedModifier = 1.1;

    private readonly TimeSpan _pheromonePlasmaUseDelay = TimeSpan.FromSeconds(0.5);
    private readonly HashSet<Entity<XenoComponent>> _receivers = new();

    private readonly HashSet<EntityUid>[] _oldReceivers = Enum.GetValues<XenoPheromones>()
        .Select(_ => new HashSet<EntityUid>())
        .ToArray();

    private EntityQuery<DamageableComponent> _damageableQuery;

    public override void Initialize()
    {
        base.Initialize();

        _damageableQuery = GetEntityQuery<DamageableComponent>();

        SubscribeLocalEvent<XenoComponent, XenoPheromonesActionEvent>(OnXenoPheromonesAction);
        SubscribeLocalEvent<XenoComponent, XenoPheromonesChosenBuiMessage>(OnXenoPheromonesChosenBui);

        // TODO CM14 make pheromone components session specific, xeno only
        SubscribeLocalEvent<XenoRecoveryPheromonesComponent, MapInitEvent>(OnRecoveryMapInit);
        SubscribeLocalEvent<XenoRecoveryPheromonesComponent, EntityUnpausedEvent>(OnRecoveryUnpaused);

        // TODO CM14 reduce crit damage
        SubscribeLocalEvent<XenoWardingPheromonesComponent, UpdateMobStateEvent>(OnWardingUpdateMobState,
            after: new[] { typeof(MobThresholdSystem) });
        SubscribeLocalEvent<XenoWardingPheromonesComponent, ComponentRemove>(OnWardingRemove);
        SubscribeLocalEvent<XenoWardingPheromonesComponent, DamageModifyEvent>(OnWardingDamageModify);

        // TODO CM14 stack slash damage
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, GetMeleeDamageEvent>(OnFrenzyGetMeleeDamage);
        SubscribeLocalEvent<XenoFrenzyPheromonesComponent, RefreshMovementSpeedModifiersEvent>(OnFrenzyMovementSpeedModifiers);
    }

    private void OnXenoPheromonesAction(Entity<XenoComponent> xeno, ref XenoPheromonesActionEvent args)
    {
        if (RemComp<XenoPheromonesComponent>(xeno))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-pheromones-stop"), xeno, xeno);
            return;
        }

        if (_net.IsClient || !TryComp(xeno, out ActorComponent? actor))
            return;

        _ui.TryOpen(xeno, XenoPheromonesUI.Key, actor.PlayerSession);
    }

    private void OnXenoPheromonesChosenBui(Entity<XenoComponent> xeno, ref XenoPheromonesChosenBuiMessage args)
    {
        if (!Enum.IsDefined(typeof(XenoPheromones), args.Pheromones) ||
            !_xeno.TryRemovePlasmaPopup(xeno, xeno.Comp.PheromonesPlasmaCost))
        {
            return;
        }

        EnsureComp<XenoPheromonesComponent>(xeno).Pheromones = args.Pheromones;
        xeno.Comp.NextPheromonesPlasmaUse = _timing.CurTime + _pheromonePlasmaUseDelay;

        var popup = Loc.GetString("cm-xeno-pheromones-start", ("pheromones", args.Pheromones.ToString()));
        _popup.PopupEntity(popup, xeno, xeno);

        if (TryComp(xeno, out ActorComponent? actor))
            _ui.TryClose(xeno, XenoPheromonesUI.Key, actor.PlayerSession);
    }

    private void OnRecoveryMapInit(Entity<XenoRecoveryPheromonesComponent> recovery, ref MapInitEvent args)
    {
        recovery.Comp.NextRegenTime = _timing.CurTime + RecoveryDelay;
    }

    private void OnRecoveryUnpaused(Entity<XenoRecoveryPheromonesComponent> recovery, ref EntityUnpausedEvent args)
    {
        recovery.Comp.NextRegenTime += args.PausedTime;
    }

    private void OnWardingUpdateMobState(Entity<XenoWardingPheromonesComponent> warding, ref UpdateMobStateEvent args)
    {
        if (args.State != MobState.Dead ||
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
        _mobThreshold.VerifyThresholds(ent);
    }

    private void OnWardingDamageModify(Entity<XenoWardingPheromonesComponent> warding, ref DamageModifyEvent args)
    {
        var damage = args.Damage.DamageDict;
        var multiplier = FixedPoint2.Max(1 - 0.25 * warding.Comp.Multiplier, 0);
        if (args.Damage.DamageDict.TryGetValue(WardingDamageTypeOne, out var amountOne))
        {
            damage[WardingDamageTypeOne] = amountOne * multiplier;
        }

        if (args.Damage.DamageDict.TryGetValue(WardingDamageTypeTwo, out var amountTwo))
        {
            damage[WardingDamageTypeOne] = amountTwo * multiplier;
        }
    }

    private void OnFrenzyGetMeleeDamage(Entity<XenoFrenzyPheromonesComponent> frenzy, ref GetMeleeDamageEvent args)
    {
        args.Modifiers.Add(new DamageModifierSet
        {
            Coefficients = new Dictionary<string, float>
            {
                [FrenzyDamageTypeOne] = FrenzyAttackDamageModifier,
                [FrenzyDamageTypeTwo] = FrenzyAttackDamageModifier,
                [FrenzyDamageTypeThree] = FrenzyAttackDamageModifier
            }
        });
    }

    private void OnFrenzyMovementSpeedModifiers(Entity<XenoFrenzyPheromonesComponent> frenzy, ref RefreshMovementSpeedModifiersEvent args)
    {
        var speed = (FrenzyMovementSpeedModifier * frenzy.Comp.Multiplier).Float();
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
            if (_timing.CurTime > recovery.NextRegenTime)
            {
                if (_xenoConstruction.IsOnWeeds(uid))
                {
                    _xeno.HealDamage(uid, RecoveryHealthRegen * recovery.Multiplier);
                    _xeno.RegenPlasma(uid, RecoveryPlasmaRegen * recovery.Multiplier);
                }

                recovery.NextRegenTime = _timing.CurTime + RecoveryDelay;
            }

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

        var query = EntityQueryEnumerator<XenoPheromonesComponent, XenoComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var pheromones, out var xeno, out var xform))
        {
            if (_timing.CurTime >= xeno.NextPheromonesPlasmaUse)
            {
                xeno.NextPheromonesPlasmaUse += _pheromonePlasmaUseDelay;
                if (!_xeno.TryRemovePlasma((uid, xeno), xeno.PheromonesPlasmaUpkeep / 10))
                {
                    RemCompDeferred<XenoPheromonesComponent>(uid);
                    continue;
                }

                Dirty(uid, xeno);
            }

            _receivers.Clear();
            _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.PheromonesRange, _receivers);

            switch (pheromones.Pheromones)
            {
                case XenoPheromones.Recovery:
                    foreach (var receiver in _receivers)
                    {
                        oldRecovery.Remove(receiver);
                        var recovery = EnsureComp<XenoRecoveryPheromonesComponent>(receiver);
                        AssignMaxMultiplier(ref recovery.Multiplier, xeno.PheromonesMultiplier);
                    }

                    break;
                case XenoPheromones.Warding:
                    foreach (var receiver in _receivers)
                    {
                        oldWarding.Remove(receiver);
                        var warding = EnsureComp<XenoWardingPheromonesComponent>(receiver);
                        AssignMaxMultiplier(ref warding.Multiplier, xeno.PheromonesMultiplier);
                    }

                    break;
                case XenoPheromones.Frenzy:
                    foreach (var receiver in _receivers)
                    {
                        oldFrenzy.Remove(receiver);
                        var frenzy = EnsureComp<XenoFrenzyPheromonesComponent>(receiver);
                        AssignMaxMultiplier(ref frenzy.Multiplier, xeno.PheromonesMultiplier);

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
}
