using System;
using Content.Shared._RMC14.Xenonids.ClawSharpness;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Examine;

public sealed class RMCIntegrityExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCIntegrityExamineComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<RMCIntegrityExamineComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp(ent, out DamageableComponent? damageable))
            return;

        var maxDamage = GetMaxDamage(ent);
        if (maxDamage == null || maxDamage.Value <= FixedPoint2.Zero || maxDamage.Value == FixedPoint2.MaxValue)
            return;

        var percent = GetIntegrityPercent(damageable.TotalDamage, maxDamage.Value);
        args.PushMarkup(Loc.GetString(ent.Comp.PercentMessage, ("percent", percent)), -100);
    }

    private FixedPoint2? GetMaxDamage(Entity<RMCIntegrityExamineComponent> ent)
    {
        if (TryComp(ent, out ReceiverXenoClawsComponent? claws) && claws.MaxHealth > 0f)
            return FixedPoint2.New(claws.MaxHealth);

        if (ent.Comp.MaxIntegrity is { } maxIntegrity)
            return maxIntegrity;

        return null;
    }

    private static int GetIntegrityPercent(FixedPoint2 totalDamage, FixedPoint2 maxDamage)
    {
        var remaining = maxDamage - totalDamage;
        if (remaining < FixedPoint2.Zero)
            remaining = FixedPoint2.Zero;

        var max = maxDamage.Float();
        if (max <= 0f)
            return 0;

        var percent = MathF.Floor(remaining.Float() / max * 100f);
        return Math.Clamp((int) percent, 0, 100);
    }
}
