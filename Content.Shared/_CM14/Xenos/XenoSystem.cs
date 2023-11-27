using Content.Shared._CM14.Xenos.Construction;
using Content.Shared._CM14.Xenos.Evolution;
using Content.Shared._CM14.Xenos.Hive;
using Content.Shared.Access.Components;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos;

public sealed class XenoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruction = default!;

    private readonly HashSet<EntityUid> _toUpdate = new();

    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<XenoComponent> _xenoQuery;

    public override void Initialize()
    {
        base.Initialize();

        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<XenoComponent, MapInitEvent>(OnXenoMapInit);
        SubscribeLocalEvent<XenoComponent, EntityUnpausedEvent>(OnXenoUnpaused);
        SubscribeLocalEvent<XenoComponent, GetAccessTagsEvent>(OnXenoGetAdditionalAccess);
        SubscribeLocalEvent<XenoComponent, NewXenoEvolvedComponent>(OnNewXenoEvolved);
        SubscribeLocalEvent<XenoWeedsComponent, StartCollideEvent>(OnWeedsStartCollide);
        SubscribeLocalEvent<XenoWeedsComponent, EndCollideEvent>(OnWeedsEndCollide);
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

    private void OnXenoUnpaused(Entity<XenoComponent> xeno, ref EntityUnpausedEvent args)
    {
        xeno.Comp.NextRegenTime += args.PausedTime;
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
                HealDamage(uid, xeno.HealthRegenOnWeeds);
                var ev = new XenoRegenEvent();
                RaiseLocalEvent(uid, ref ev);
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

    public void HealDamage(Entity<DamageableComponent?> xeno, FixedPoint2 amount)
    {
        if (!_damageableQuery.Resolve(xeno, ref xeno.Comp, false))
            return;

        var heal = new DamageSpecifier();
        foreach (var (type, typeAmount) in xeno.Comp.Damage.DamageDict)
        {
            var total = heal.GetTotal();
            if (typeAmount + total >= amount)
            {
                var change = -FixedPoint2.Min(typeAmount, amount - total);
                if (!heal.DamageDict.TryAdd(type, change))
                {
                    heal.DamageDict[type] += change;
                }

                break;
            }
            else
            {
                if (!heal.DamageDict.TryAdd(type, -typeAmount))
                {
                    heal.DamageDict[type] += -typeAmount;
                }
            }
        }

        if (heal.GetTotal() < FixedPoint2.Zero)
            _damageable.TryChangeDamage(xeno, heal);
    }
}
