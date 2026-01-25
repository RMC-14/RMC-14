using System.Collections.Immutable;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;

namespace Content.Shared._RMC14.HealthExaminable;

public sealed class RMCHealthExaminableSystem : EntitySystem
{
    private readonly ImmutableArray<FixedPoint2> _thresholds = ImmutableArray.Create<FixedPoint2>(25, 50, 75);

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCHealthExaminableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<RMCHealthExaminableComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp(ent, out DamageableComponent? damageable))
            return;

        using (args.PushGroup(nameof(RMCHealthExaminableSystem), -1))
        {
            foreach (var group in ent.Comp.Groups)
            {
                if (!damageable.DamagePerGroup.TryGetValue(group, out var groupDamage))
                    continue;

                for (var i = _thresholds.Length - 1; i >= 0; i--)
                {
                    var threshold = _thresholds[i];
                    if (groupDamage < threshold)
                        continue;

                    var id = $"rmc-health-examinable-{group}-{threshold.Int()}";
                    if (!Loc.TryGetString(id, out var msg, ("target", Identity.Entity(ent, EntityManager, args.Examiner))))
                        continue;

                    args.PushMarkup(msg);
                    break;
                }
            }
        }
    }
}
