using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

[Virtual]
public partial class Ciphering : RMCChemicalEffect
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
        if (GetHiveBySlot(args.EntityManager, level) is not { } hive)
            return;

        var parasite = args.EntityManager.System<SharedXenoParasiteSystem>();
        parasite.SetHive((args.TargetEntity, victim), hive);
    }

    public static EntityUid? GetHiveBySlot(IEntityManager entities, int slot)
    {
        var hives = entities.EntityQueryEnumerator<HiveSlotComponent>();
        while (hives.MoveNext(out var hiveUid, out var hiveSlot))
        {
            if (hiveSlot.Position == slot)
                return hiveUid;
        }

        return null;
    }
}
