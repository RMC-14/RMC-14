using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Xenonids.AcidBloodSplash;

public sealed class AcidBloodSplashSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    private readonly HashSet<ProtoId<DamageTypePrototype>> _bruteTypes = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<AcidBloodSplashComponent, DamageChangedEvent>(OnDamageChanged);

        _bruteTypes.Clear();

        if (_prototypes.TryIndex(BruteGroup, out var bruteProto))
        {
            foreach (var type in bruteProto.DamageTypes)
            {
                _bruteTypes.Add(type);
            }
        }
    }

    private void OnDamageChanged(EntityUid uid, AcidBloodSplashComponent comp, ref DamageChangedEvent args)
    {
        if (comp.NextSplashAvailable > _timing.CurTime)
            return;

        if (_mobState.IsDead(uid))
            return;

        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        // activate acid splash only when damage is big enough
        if (args.DamageDelta.GetTotal() < comp.MinimalTriggerDamage)
            return;

        if (!TryComp(uid, out TransformComponent? xform))
            return;

        var damageDict = args.DamageDelta.DamageDict;
        var probability = comp.BaseSplashTriggerProbability;
        probability += (float)args.DamageDelta.GetTotal() * comp.DamageProbabilityMultiplier;

        foreach (var (type, _) in damageDict)
        {
            if (_bruteTypes.Contains(type) && damageDict[type] > 0)
                probability += comp.BruteDamageProbabilityModificator;
        }

        // TODO: increase probability from sharp and edge weapon + from damage in chest

        if (_random.NextFloat() > probability)
            return;

        var test = _entityLookup.GetEntitiesInRange(xform.Coordinates, comp.CloseSplashRadius);
    }
}
