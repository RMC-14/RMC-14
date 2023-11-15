using Content.Shared._CM14.Xenos.Construction;
using Content.Shared.Access.Components;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos;

public sealed class XenoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruction = default!;

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
        SubscribeLocalEvent<XenoComponent, RejuvenateEvent>(OnXenoRejuvenate);
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

    private void OnXenoRejuvenate(Entity<XenoComponent> ent, ref RejuvenateEvent args)
    {
        RegenPlasma((ent, ent), ent.Comp.MaxPlasma);
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
                RegenPlasma((uid, xeno), xeno.PlasmaRegenOnWeeds);
            }

            Dirty(uid, xeno);
        }
    }

    public void MakeXeno(Entity<XenoComponent?> uid)
    {
        EnsureComp<XenoComponent>(uid);
    }

    public bool HasPlasma(Entity<XenoComponent> xeno, FixedPoint2 plasma)
    {
        return xeno.Comp.Plasma >= plasma;
    }

    public void RegenPlasma(Entity<XenoComponent?> xeno, FixedPoint2 amount)
    {
        if (!_xenoQuery.Resolve(xeno, ref xeno.Comp))
            return;

        xeno.Comp.Plasma = FixedPoint2.Min(xeno.Comp.Plasma + amount, xeno.Comp.MaxPlasma);
        Dirty(xeno, xeno.Comp);
    }

    public bool TryRemovePlasma(Entity<XenoComponent> xeno, FixedPoint2 plasma)
    {
        if (!HasPlasma(xeno, plasma))
            return false;

        RemovePlasma(xeno, plasma);
        return true;
    }

    public bool TryRemovePlasmaPopup(Entity<XenoComponent> xeno, FixedPoint2 plasma)
    {
        if (TryRemovePlasma(xeno, plasma))
            return true;

        _popup.PopupClient(Loc.GetString("cm-xeno-not-enough-plasma"), xeno, xeno);
        return false;
    }

    public void RemovePlasma(Entity<XenoComponent> xeno, FixedPoint2 plasma)
    {
        xeno.Comp.Plasma = FixedPoint2.Max(xeno.Comp.Plasma - plasma, FixedPoint2.Zero);
        Dirty(xeno);
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
