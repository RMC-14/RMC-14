using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.ShootingTarget;

public sealed partial class RMCTargetSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCTargetComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCTargetComponent, ExaminedEvent>(OnExamined);
    }

    private void OnDamageChanged(Entity<RMCTargetComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
        {
            if (args.Damageable.TotalDamage == FixedPoint2.Zero)
                ResetStats(ent.Comp);
            return;
        }

        if (args.DamageDelta == null)
            return;

        var damage = GetPositiveDamage(args.DamageDelta);
        if (damage <= FixedPoint2.Zero)
            return;

        ent.Comp.TotalDamage += damage;
        UpdateDamageWindow(ent, damage);

        if (!_net.IsServer)
            return;

        if (_timing.CurTime < ent.Comp.LastPopupAt + ent.Comp.PopupCooldown)
            return;

        ent.Comp.LastPopupAt = _timing.CurTime;
        _popup.PopupCoordinates(
            Loc.GetString("rmc-target-dps-popup", ("dps", $"{GetDps(ent.Comp):0.0}")),
            new EntityCoordinates(ent, ent.Comp.PopupOffset),
            PopupType.Small);
    }

    private void OnExamined(Entity<RMCTargetComponent> ent, ref ExaminedEvent args)
    {
        UpdateDamageWindow(ent);

        using (args.PushGroup(nameof(RMCTargetSystem)))
        {
            args.PushMarkup(Loc.GetString(
                "rmc-target-dps-examine",
                ("dps", $"{GetDps(ent.Comp):0.0}"),
                ("window", $"{ent.Comp.DpsWindow.TotalSeconds:0}"),
                ("total", $"{ent.Comp.TotalDamage:0.0}")
            ));
        }
    }

    private void UpdateDamageWindow(Entity<RMCTargetComponent> ent, FixedPoint2? newDamage = null)
    {
        var cutoff = _timing.CurTime - ent.Comp.DpsWindow;

        while (ent.Comp.DamageSamples.Count > 0 &&
               ent.Comp.DamageSamples.Peek().Time <= cutoff)
        {
            var sample = ent.Comp.DamageSamples.Dequeue();
            ent.Comp.WindowDamage -= sample.Damage;
        }

        if (newDamage == null)
            return;

        ent.Comp.DamageSamples.Enqueue((_timing.CurTime, newDamage.Value));
        ent.Comp.WindowDamage += newDamage.Value;
    }

    private void ResetStats(RMCTargetComponent comp)
    {
        comp.DamageSamples.Clear();
        comp.WindowDamage = FixedPoint2.Zero;
        comp.TotalDamage = FixedPoint2.Zero;
        comp.LastPopupAt = TimeSpan.MinValue;
    }

    private static FixedPoint2 GetPositiveDamage(DamageSpecifier damage)
    {
        var total = FixedPoint2.Zero;
        foreach (var amount in damage.DamageDict.Values)
        {
            if (amount > 0)
                total += amount;
        }
        return total;
    }

    private double GetDps(RMCTargetComponent comp)
    {
        if (comp.DamageSamples.Count == 0 || comp.DpsWindow <= TimeSpan.Zero)
            return 0;

        var elapsed = _timing.CurTime - comp.DamageSamples.Peek().Time;
        var seconds = Math.Clamp(elapsed.TotalSeconds, 0.5, comp.DpsWindow.TotalSeconds);
        return comp.WindowDamage.Double() / seconds;
    }
}
