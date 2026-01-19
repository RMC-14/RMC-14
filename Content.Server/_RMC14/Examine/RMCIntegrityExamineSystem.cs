using System;
using Content.Server.Destructible;
using Content.Shared._RMC14.Xenonids.ClawSharpness;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Robust.Shared.Localization;

namespace Content.Server._RMC14.Examine;

public sealed class RMCIntegrityExamineSystem : EntitySystem
{
    [Dependency] private readonly DestructibleSystem _destructible = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCIntegrityExamineComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, RMCIntegrityExamineComponent component, ExaminedEvent args)
    {
        if (!TryComp(uid, out DamageableComponent? damageable))
            return;

        var maxDamage = GetMaxDamage(uid);
        if (maxDamage == null || maxDamage.Value <= FixedPoint2.Zero || maxDamage.Value == FixedPoint2.MaxValue)
            return;

        var percent = GetIntegrityPercent(damageable.TotalDamage, maxDamage.Value);
        args.PushMarkup(Loc.GetString(component.PercentMessage, ("percent", percent)), -100);
    }

    private FixedPoint2? GetMaxDamage(EntityUid uid)
    {
        if (TryComp(uid, out DestructibleComponent? destructible))
        {
            var destroyedAt = _destructible.DestroyedAt(uid, destructible);
            if (destroyedAt != FixedPoint2.MaxValue && destroyedAt > FixedPoint2.Zero)
                return destroyedAt;
        }

        if (TryComp(uid, out ReceiverXenoClawsComponent? claws) && claws.MaxHealth > 0f)
            return FixedPoint2.New(claws.MaxHealth);

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
