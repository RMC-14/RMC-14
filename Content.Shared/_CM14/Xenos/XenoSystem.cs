using Content.Shared._CM14.Xenos.Construction;
using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.Access.Components;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos;

public sealed class XenoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruction = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private EntityQuery<DamageableComponent> _damageableQuery;

    public override void Initialize()
    {
        base.Initialize();

        _damageableQuery = GetEntityQuery<DamageableComponent>();

        SubscribeLocalEvent<XenoComponent, MapInitEvent>(OnXenoMapInit);
        SubscribeLocalEvent<XenoComponent, EntityUnpausedEvent>(OnXenoUnpaused);
        SubscribeLocalEvent<XenoComponent, GetAccessTagsEvent>(OnXenoGetAdditionalAccess);
    }

    private void OnXenoMapInit(Entity<XenoComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextRegenTime = _timing.CurTime + ent.Comp.PlasmaRegenCooldown;

        foreach (var actionId in ent.Comp.ActionIds)
        {
            if (!ent.Comp.Actions.ContainsKey(actionId) &&
                _action.AddAction(ent, actionId) is { } newAction)
            {
                ent.Comp.Actions[actionId] = newAction;
            }
        }
    }

    private void OnXenoUnpaused(Entity<XenoComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextRegenTime += args.PausedTime;
        ent.Comp.NextPheromonesPlasmaUse += args.PausedTime;
    }

    private void OnXenoGetAdditionalAccess(Entity<XenoComponent> ent, ref GetAccessTagsEvent args)
    {
        args.Tags.UnionWith(ent.Comp.AccessLevels);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoComponent>();
        var time = _timing.CurTime;

        while (query.MoveNext(out var uid, out var xeno))
        {
            if (time < xeno.NextRegenTime)
                continue;

            xeno.NextRegenTime = time + xeno.PlasmaRegenCooldown;

            if (_xenoConstruction.IsOnWeeds(uid))
            {
                HealDamage(uid, xeno.HealthRegenOnWeeds);
                _xenoPlasma.RegenPlasma((uid, xeno), xeno.PlasmaRegenOnWeeds);
            }

            Dirty(uid, xeno);
        }
    }

    public void MakeXeno(Entity<XenoComponent?> uid)
    {
        EnsureComp<XenoComponent>(uid);
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
