using System.Collections.Immutable;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Content.Shared.Coordinates;
using Content.Server._RMC14.Decals;
using Content.Server.Spawners.Components;

namespace Content.Server._RMC14.BloodSplash;

public sealed class BloodSplashSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RMCDecalSystem _rmcDecal = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    private readonly HashSet<ProtoId<DamageTypePrototype>> _bruteTypes = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodSplashComponent, DamageChangedEvent>(OnDamageChanged);

        _bruteTypes.Clear();

        if (_prototypes.TryIndex(BruteGroup, out var bruteProto))
        {
            foreach (var type in bruteProto.DamageTypes)
            {
                _bruteTypes.Add(type);
            }
        }
    }

    private void ActivateSplash(Entity<BloodSplashComponent> ent)
    {
        if (!_prototypes.TryIndex(ent.Comp.BloodDecalSpawnerPrototype, out var prototype) ||
            !prototype.TryGetComponent(out RandomDecalSpawnerComponent? spawner, _compFactory))
        {
            return;
        }

        var decalsInTile = _rmcDecal.GetDecalsInTile(ent, spawner.Decals);

        // If MaxDecalsPerTile is null, it's unlimited. Otherwise check the limit.
        var canSpawn = spawner.MaxDecalsPerTile == null || decalsInTile < spawner.MaxDecalsPerTile;

        if (canSpawn)
        {
            Spawn(ent.Comp.BloodDecalSpawnerPrototype, ent.Owner.ToCoordinates());
        }
    }

    private void OnDamageChanged(Entity<BloodSplashComponent> ent, ref DamageChangedEvent args)
    {
        var time = _timing.CurTime;
        if (ent.Comp.NextSplashAvailable > time)
            return;

        if (_mobState.IsDead(ent) && !ent.Comp.WorksWhileDead)
            return;

        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        // Self-inflicted damage or from fire damage over time doesn't trigger splash
        if (args.Origin is { } origin && origin == ent.Owner)
            return;

        // activate blood splash only when damage is big enough
        if (args.DamageDelta.GetTotal() < ent.Comp.MinimalTriggerDamage)
            return;

        var damageDict = args.DamageDelta.DamageDict;
        var triggerProbability = ent.Comp.BaseSplashTriggerProbability; // probability of splash activation, in percentage
        triggerProbability += (float)args.DamageDelta.GetTotal() * ent.Comp.DamageTriggerProbabilityMultiplier;

        foreach (var (type, _) in damageDict)
        {
            if (_bruteTypes.Contains(type) && damageDict[type] > 0)
            {
                triggerProbability += ent.Comp.BruteDamageProbabilityModificator;
                break;
            }
        }

        triggerProbability /= 100f; // Reduce the value to decimal

        // TODO: increase probability from sharp and edge weapon + from damage in chest once limb damage implemented

        if (_random.NextFloat() > triggerProbability)
            return;

        ActivateSplash(ent);
        ent.Comp.NextSplashAvailable = _timing.CurTime + ent.Comp.SplashCooldown;
    }
}
