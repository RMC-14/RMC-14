using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Energy;

public sealed class XenoEnergySystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoEnergyComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<XenoEnergyComponent, XenoProjectileHitUserEvent>(OnXenoProjectileHitUser);
        SubscribeLocalEvent<XenoEnergyComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<XenoActionEnergyComponent, RMCActionUseAttemptEvent>(OnXenoActionEnergyUseAttempt);
        SubscribeLocalEvent<XenoActionEnergyComponent, RMCActionUseEvent>(OnXenoActionEnergyUse);
    }

    private void OnMeleeHit(Entity<XenoEnergyComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var isHit = false;
        foreach (var hit in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno.Owner, hit))
                continue;

            isHit = true;
            break;
        }

        if (!isHit)
            return;

        AddEnergy(xeno, xeno.Comp.GainAttack);
    }

    private void OnXenoProjectileHitUser(Entity<XenoEnergyComponent> xeno, ref XenoProjectileHitUserEvent args)
    {
        if (_xeno.CanAbilityAttackTarget(xeno, args.Hit))
            AddEnergy(xeno, xeno.Comp.GainAttack);
    }

    private void OnRejuvenate(Entity<XenoEnergyComponent> ent, ref RejuvenateEvent args)
    {
        AddEnergy(ent, ent.Comp.Max);
    }

    private void OnXenoActionEnergyUseAttempt(Entity<XenoActionEnergyComponent> action, ref RMCActionUseAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasEnergyPopup(args.User, action.Comp.Cost))
            args.Cancelled = true;
    }

    private void OnXenoActionEnergyUse(Entity<XenoActionEnergyComponent> action, ref RMCActionUseEvent args)
    {
        if (!TryComp(args.User, out XenoEnergyComponent? energy))
            return;

        RemoveEnergy((args.User, energy), action.Comp.Cost);
    }

    public void AddEnergy(Entity<XenoEnergyComponent> xeno, int energy, bool popup = true)
    {
        if (popup && xeno.Comp.Current < xeno.Comp.Max && energy > 0)
            _popup.PopupClient(Loc.GetString("rmc-xeno-energy-increase-user"), xeno, xeno);

        xeno.Comp.Current = Math.Min(xeno.Comp.Max, xeno.Comp.Current + energy);
        Dirty(xeno);
    }

    public bool HasEnergy(Entity<XenoEnergyComponent> xeno, int energy)
    {
        return xeno.Comp.Current >= energy;
    }

    public bool HasEnergyPopup(Entity<XenoEnergyComponent?> xeno, int energy, bool predicted = true)
    {
        void DoPopup()
        {
            var popup = Loc.GetString("rmc-xeno-not-enough-energy");
            if (predicted)
                _popup.PopupClient(popup, xeno, xeno, PopupType.SmallCaution);
            else
                _popup.PopupEntity(popup, xeno, xeno, PopupType.SmallCaution);
        }

        if (!Resolve(xeno, ref xeno.Comp, false))
        {
            DoPopup();
            return false;
        }

        if (!HasEnergy((xeno, xeno.Comp), energy))
        {
            DoPopup();
            return false;
        }

        return true;
    }

    public void RemoveEnergy(Entity<XenoEnergyComponent?> xeno, int plasma)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return;

        xeno.Comp.Current = int.Max(0, xeno.Comp.Current - plasma);
        Dirty(xeno);
    }

    public bool TryRemoveEnergy(Entity<XenoEnergyComponent?> xeno, int energy)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return false;

        if (!HasEnergy((xeno, xeno.Comp), energy))
            return false;

        RemoveEnergy((xeno, xeno.Comp), energy);
        return true;
    }

    public bool TryRemoveEnergyPopup(Entity<XenoEnergyComponent?> xeno, int energy)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return false;

        if (TryRemoveEnergy((xeno, xeno.Comp), energy))
            return true;

        _popup.PopupClient(Loc.GetString("rmc-xeno-not-enough-energy"), xeno, xeno);
        return false;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoEnergyComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsDead(uid))
                continue;

            if (time < comp.NextGain)
                continue;

            comp.NextGain = time + comp.GainEvery;
            AddEnergy((uid, comp), comp.Gain, false);
            Dirty(uid, comp);
        }
    }
}
