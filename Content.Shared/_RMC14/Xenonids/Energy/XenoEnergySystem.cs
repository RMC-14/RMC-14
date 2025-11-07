using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared._RMC14.TrainingDummy;
using Content.Shared.Alert;
using Content.Shared.Rounding;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Energy;

public sealed class XenoEnergySystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly StandingStateSystem _stand = default!;

    private void OnXenoPlasmaMapInit(Entity<XenoEnergyComponent> ent, ref MapInitEvent args)
    {
        UpdateAlert(ent);
    }

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoEnergyComponent, MapInitEvent>(OnXenoEnergyMapInit);
        SubscribeLocalEvent<XenoEnergyComponent, ComponentRemove>(OnXenoEnergyRemove);
        SubscribeLocalEvent<XenoEnergyComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<XenoEnergyComponent, XenoProjectileHitUserEvent>(OnXenoProjectileHitUser);
        SubscribeLocalEvent<XenoEnergyComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<XenoActionEnergyComponent, RMCActionUseAttemptEvent>(OnXenoActionEnergyUseAttempt);
        SubscribeLocalEvent<XenoActionEnergyComponent, RMCActionUseEvent>(OnXenoActionEnergyUse);
    }

    private void OnXenoEnergyMapInit(Entity<XenoEnergyComponent> ent, ref MapInitEvent args)
    {
        UpdateAlert(ent);
    }

    private void OnXenoEnergyRemove(Entity<XenoEnergyComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    private void OnMeleeHit(Entity<XenoEnergyComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var isHit = false;
        var isDown = false;
        foreach (var hit in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno.Owner, hit))
                continue;

            if (xeno.Comp.IgnoreLateInfected && TryComp<VictimInfectedComponent>(hit, out var infect) && infect.CurrentStage >= infect.FinalSymptomsStart)
                continue;

            if (HasComp<RMCTrainingDummyComponent>(hit))
                return;

            isHit = true;
            if (_stand.IsDown(hit))
                isDown = true;
            break;
        }

        if (!isHit)
            return;

        AddEnergy(xeno, (int) ( isDown ? xeno.Comp.GainAttackDowned : xeno.Comp.GainAttack));
        UpdateAlert(xeno);
    }

    private void OnXenoProjectileHitUser(Entity<XenoEnergyComponent> xeno, ref XenoProjectileHitUserEvent args)
    {
        if (!xeno.Comp.GainOnProjectiles)
            return;

        if (_xeno.CanAbilityAttackTarget(xeno, args.Hit))
        {
            AddEnergy(xeno, xeno.Comp.GainAttack);
            UpdateAlert(xeno);
        }
    }

    private void OnRejuvenate(Entity<XenoEnergyComponent> ent, ref RejuvenateEvent args)
    {
        AddEnergy(ent, ent.Comp.Max);
        UpdateAlert(ent);
    }

    private void UpdateAlert(Entity<XenoEnergyComponent> xeno)
    {
        var level = MathF.Max(0f, xeno.Comp.Current);
        var max = _alerts.GetMaxSeverity(xeno.Comp.Alert);
        var severity = max - ContentHelpers.RoundToLevels(level, xeno.Comp.Max, max + 1);
        string? energyResourceMessage = (int)xeno.Comp.Current + " / " + xeno.Comp.Max;
        _alerts.ShowAlert(xeno, xeno.Comp.Alert, (short)severity, dynamicMessage: energyResourceMessage);
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
        var rev = new XenoEnergyGainAttemptEvent();
        RaiseLocalEvent(xeno, rev);

        if (rev.Cancelled)
            return;

        if (popup && xeno.Comp.Current < xeno.Comp.Max && energy > 0)
            _popup.PopupClient(Loc.GetString(xeno.Comp.PopupGain), xeno, xeno);

        xeno.Comp.Current = Math.Min(xeno.Comp.Max, xeno.Comp.Current + energy);
        Dirty(xeno);
        UpdateAlert(xeno);
        var ev = new XenoEnergyChangedEvent(xeno.Comp.Current);
        RaiseLocalEvent(xeno, ref ev);
    }

    public bool HasEnergy(Entity<XenoEnergyComponent> xeno, int energy)
    {
        return xeno.Comp.Current >= energy;
    }

    public bool HasEnergyPopup(Entity<XenoEnergyComponent?> xeno, int energy, bool predicted = true)
    {
        void DoPopup()
        {
            var popup = Loc.GetString(xeno.Comp != null ? xeno.Comp.PopupNotEnough : "rmc-xeno-not-enough-energy");
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
        UpdateAlert((xeno, xeno.Comp));
        var ev = new XenoEnergyChangedEvent(xeno.Comp.Current);
        RaiseLocalEvent(xeno, ref ev);
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

        _popup.PopupClient(Loc.GetString(xeno.Comp.PopupNotEnough), xeno, xeno);
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
            if (comp.GenerationCap == null || comp.Current < comp.GenerationCap)
                AddEnergy((uid, comp), comp.Gain, false);
            Dirty(uid, comp);
        }
    }
}
