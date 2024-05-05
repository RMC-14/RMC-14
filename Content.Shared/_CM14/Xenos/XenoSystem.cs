using Content.Shared._CM14.Marines;
using Content.Shared._CM14.Medical.Scanner;
using Content.Shared._CM14.Xenos.Construction;
using Content.Shared._CM14.Xenos.Evolution;
using Content.Shared._CM14.Xenos.Hive;
using Content.Shared._CM14.Xenos.Pheromones;
using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared._CM14.Xenos.Rest;
using Content.Shared.Access.Components;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio;
using Content.Shared.Standing;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._CM14.Xenos;

public sealed class XenoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruction = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private readonly HashSet<EntityUid> _toUpdate = new();

    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<XenoPlasmaComponent> _xenoPlasmaQuery;
    private EntityQuery<XenoRecoveryPheromonesComponent> _xenoRecoveryQuery;

    public override void Initialize()
    {
        base.Initialize();

        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _marineQuery = GetEntityQuery<MarineComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();
        _xenoPlasmaQuery = GetEntityQuery<XenoPlasmaComponent>();
        _xenoRecoveryQuery = GetEntityQuery<XenoRecoveryPheromonesComponent>();

        SubscribeLocalEvent<XenoComponent, MapInitEvent>(OnXenoMapInit);
        SubscribeLocalEvent<XenoComponent, GetAccessTagsEvent>(OnXenoGetAdditionalAccess);
        SubscribeLocalEvent<XenoComponent, NewXenoEvolvedComponent>(OnNewXenoEvolved);
        SubscribeLocalEvent<XenoComponent, HealthScannerAttemptTargetEvent>(OnXenoHealthScannerAttemptTarget);
        SubscribeLocalEvent<XenoComponent, GetDefaultRadioChannelEvent>(OnXenoGetDefaultRadioChannel);

        SubscribeLocalEvent<XenoWeedsComponent, StartCollideEvent>(OnWeedsStartCollide);
        SubscribeLocalEvent<XenoWeedsComponent, EndCollideEvent>(OnWeedsEndCollide);

        UpdatesAfter.Add(typeof(SharedXenoPheromonesSystem));
    }

    private void OnXenoMapInit(Entity<XenoComponent> xeno, ref MapInitEvent args)
    {
        foreach (var actionId in xeno.Comp.ActionIds)
        {
            if (!xeno.Comp.Actions.ContainsKey(actionId) &&
                _action.AddAction(xeno, actionId) is { } newAction)
            {
                xeno.Comp.Actions[actionId] = newAction;
            }
        }

        xeno.Comp.NextRegenTime = _timing.CurTime + xeno.Comp.RegenCooldown;
        xeno.Comp.OnWeeds = _xenoConstruction.IsOnWeeds(xeno.Owner);
        Dirty(xeno);
    }

    private void OnXenoGetAdditionalAccess(Entity<XenoComponent> xeno, ref GetAccessTagsEvent args)
    {
        args.Tags.UnionWith(xeno.Comp.AccessLevels);
    }

    private void OnNewXenoEvolved(Entity<XenoComponent> newXeno, ref NewXenoEvolvedComponent args)
    {
        var oldRotation = _transform.GetWorldRotation(args.OldXeno);
        _transform.SetWorldRotation(newXeno, oldRotation);
    }

    private void OnXenoHealthScannerAttemptTarget(Entity<XenoComponent> ent, ref HealthScannerAttemptTargetEvent args)
    {
        args.Popup = "The scanner can't make sense of this creature.";
        args.Cancelled = true;
    }

    private void OnXenoGetDefaultRadioChannel(Entity<XenoComponent> ent, ref GetDefaultRadioChannelEvent args)
    {
        args.Channel = SharedChatSystem.HivemindChannel;
    }

    private void OnWeedsStartCollide(Entity<XenoWeedsComponent> ent, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (_xenoQuery.TryGetComponent(other, out var xeno) && !xeno.OnWeeds)
            _toUpdate.Add(other);
    }

    private void OnWeedsEndCollide(Entity<XenoWeedsComponent> ent, ref EndCollideEvent args)
    {
        var other = args.OtherEntity;
        if (_xenoQuery.TryGetComponent(other, out var xeno) && xeno.OnWeeds)
            _toUpdate.Add(other);
    }

    public void MakeXeno(Entity<XenoComponent?> xeno)
    {
        EnsureComp<XenoComponent>(xeno);
    }

    public void SetHive(Entity<XenoComponent?> xeno, Entity<HiveComponent?> hive)
    {
        if (!Resolve(xeno, ref xeno.Comp) ||
            !Resolve(hive, ref hive.Comp))
        {
            return;
        }

        xeno.Comp.Hive = hive;
        Dirty(xeno, xeno.Comp);
    }

    private FixedPoint2 GetWeedsHealAmount(Entity<XenoComponent> xeno)
    {
        if (!_mobThresholds.TryGetIncapThreshold(xeno, out var threshold))
            return FixedPoint2.Zero;

        FixedPoint2 multiplier;
        if (_mobState.IsCritical(xeno))
            multiplier = xeno.Comp.CritHealMultiplier;
        else if (_standing.IsDown(xeno) || HasComp<XenoRestingComponent>(xeno))
            multiplier = xeno.Comp.RestHealMultiplier;
        else
            multiplier = xeno.Comp.StandHealingMultiplier;

        var passiveHeal = threshold.Value / 65 + xeno.Comp.FlatHealing;
        var recovery = (CompOrNull<XenoRecoveryPheromonesComponent>(xeno)?.Multiplier ?? 0) / 2;
        var recoveryHeal = (threshold.Value / 65) * (recovery / 2);
        return (passiveHeal + recoveryHeal) * multiplier / 2;
    }

    public void HealDamage(Entity<DamageableComponent?> xeno, FixedPoint2 amount)
    {
        if (!_damageableQuery.Resolve(xeno, ref xeno.Comp, false) ||
            xeno.Comp.Damage.GetTotal() <= FixedPoint2.Zero)
        {
            return;
        }

        if (_mobStateQuery.TryGetComponent(xeno, out var mobState) &&
            _mobState.IsDead(xeno, mobState))
        {
            return;
        }

        var heal = new DamageSpecifier();
        var groups = new Dictionary<string, List<string>>();
        foreach (var group in _prototypes.EnumeratePrototypes<DamageGroupPrototype>())
        {
            foreach (var type in group.DamageTypes)
            {
                if (xeno.Comp.Damage.DamageDict.TryGetValue(type, out var damage) &&
                    damage > FixedPoint2.Zero)
                {
                    groups.GetOrNew(group.ID).Add(type);
                }
            }
        }

        var typesLeft = new List<string>();
        foreach (var (_, types) in groups)
        {
            var left = amount;
            FixedPoint2? lastLeft;
            typesLeft.Clear();
            typesLeft.AddRange(types);

            while (left > 0)
            {
                lastLeft = left;

                for (var i = typesLeft.Count - 1; i >= 0; i--)
                {
                    var type = typesLeft[i];
                    var damage = xeno.Comp.Damage.DamageDict[type];
                    var existingHeal = -heal.DamageDict.GetValueOrDefault(type);
                    left += existingHeal;
                    var toHeal = FixedPoint2.Min(existingHeal + left / (i + 1), damage);
                    if (damage <= toHeal)
                        typesLeft.RemoveAt(i);

                    heal.DamageDict[type] = -toHeal;
                    left -= toHeal;
                }

                if (lastLeft == left)
                    break;
            }
        }

        if (heal.GetTotal() < FixedPoint2.Zero)
            _damageable.TryChangeDamage(xeno, heal);
    }

    // TODO CM14 generalize this for survivors, synthetics, enemy hives, etc
    public bool CanHitLiving(EntityUid xeno, EntityUid defender)
    {
        return _marineQuery.HasComponent(defender);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoComponent>();
        var time = _timing.CurTime;

        while (query.MoveNext(out var uid, out var xeno))
        {
            if (time < xeno.NextRegenTime)
                continue;

            xeno.NextRegenTime = time + xeno.RegenCooldown;

            if (xeno.OnWeeds)
            {
                var heal = GetWeedsHealAmount((uid, xeno));
                if (heal > FixedPoint2.Zero)
                {
                    HealDamage(uid, heal);

                    if (_xenoPlasmaQuery.TryComp(uid, out var plasma))
                    {
                        var plasmaRestored = plasma.PlasmaRegenOnWeeds * plasma.MaxPlasma / 100 / 2;
                        _xenoPlasma.RegenPlasma((uid, plasma), plasmaRestored);

                        if (_xenoRecoveryQuery.TryComp(uid, out var recovery))
                            _xenoPlasma.RegenPlasma((uid, plasma), plasmaRestored * recovery.Multiplier / 4);
                    }
                }
            }
            else
            {
                if (_xenoPlasmaQuery.TryComp(uid, out var plasma))
                    _xenoPlasma.RegenPlasma((uid, plasma), plasma.PlasmaRegenOffWeeds * plasma.MaxPlasma / 100 / 2);
            }

            Dirty(uid, xeno);
        }

        foreach (var xenoId in _toUpdate)
        {
            if (!_xenoQuery.TryGetComponent(xenoId, out var xeno))
                continue;

            var any = false;
            foreach (var contact in _physics.GetContactingEntities(xenoId))
            {
                if (HasComp<XenoWeedsComponent>(contact))
                {
                    any = true;
                    break;
                }
            }

            if (xeno.OnWeeds == any)
                continue;

            xeno.OnWeeds = any;
            Dirty(xenoId, xeno);

            var ev = new XenoOnWeedsChangedEvent(xeno.OnWeeds);
            RaiseLocalEvent(xenoId, ref ev);
        }

        _toUpdate.Clear();
    }
}
