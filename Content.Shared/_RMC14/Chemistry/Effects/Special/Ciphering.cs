using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class Ciphering : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "This extremely complex chemical structure represents some kind of biological cipher.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out VictimInfectedComponent? victim))
            return;

        var level = Math.Clamp((int) Potency, HiveSlots.Normal, HiveSlots.Delta);

        var hives = args.EntityManager.EntityQueryEnumerator<HiveSlotComponent>();
        while (hives.MoveNext(out var hiveUid, out var slot))
        {
            if (slot.Position != level)
                continue;

            var parasite = args.EntityManager.System<SharedXenoParasiteSystem>();
            parasite.SetHive((args.TargetEntity, victim), hiveUid);
            return;
        }
    }
}
