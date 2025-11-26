using System.Collections.Immutable;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Content.Shared.Coordinates;
using Content.Shared.Chat.Prototypes;
using Content.Shared._RMC14.Emote;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Audio.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using System.Linq;
using Content.Server._RMC14.Decals;
using Content.Server.Spawners.Components;
using Content.Shared.Decals;
using Content.Shared.Body.Events;

namespace Content.Server._RMC14.Xenonids.AcidBloodSplash;

public sealed class AcidBloodSplashSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCDecalSystem _rmcDecal = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    private static readonly ProtoId<EmotePrototype> ScreamProto = "Scream";
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    private readonly HashSet<ProtoId<DamageTypePrototype>> _bruteTypes = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<AcidBloodSplashComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<AcidBloodSplashComponent, BeingGibbedEvent>(OnGib);

        _bruteTypes.Clear();

        if (_prototypes.TryIndex(BruteGroup, out var bruteProto))
        {
            foreach (var type in bruteProto.DamageTypes)
            {
                _bruteTypes.Add(type);
            }
        }
    }

    private void ActivateSplash(EntityUid uid, AcidBloodSplashComponent comp, float splashRadius)
    {
        if (!_prototypes.TryIndex(comp.BloodDecalSpawnerPrototype, out var prototype) ||
            !prototype.TryGetComponent(out RandomDecalSpawnerComponent? spawner, _compFactory) ||
            _rmcDecal.GetDecalsInTile(uid, spawner.Decals) < spawner.MaxDecalsPerTile)
        {
            // create decal, probability inside prototype
            Spawn(comp.BloodDecalSpawnerPrototype, uid.ToCoordinates());
        }

        var i = 0; // parity moment, I would prefer a for loop if I knew how to do it in not ugly way.
        var targetsSet = _entityLookup.GetEntitiesInRange(uid.ToCoordinates(), splashRadius);
        var closeRangeTargets = _entityLookup.GetEntitiesInRange(uid.ToCoordinates(), comp.CloseSplashRadius);
        var targetsList = targetsSet.ToList(); // shuffle don't work on HashSet
        _random.Shuffle(targetsList);
        foreach (var target in targetsList)
        {
            if (!_xeno.CanAbilityAttackTarget(uid, target))
                continue;

            var hitProbability = comp.BaseHitProbability - i * 0.05;

            if (closeRangeTargets.Contains(target))
                hitProbability += 0.3;

            if (_random.NextFloat() > hitProbability)
                continue;

            comp.NextSplashAvailable = _timing.CurTime + comp.SplashCooldown;
            _damageable.TryChangeDamage(target, _xeno.TryApplyXenoAcidDamageMultiplier(target, comp.Damage));
            i++;

            _audio.PlayPvs(comp.AcidSplashSound, target);

            _popup.PopupEntity(Loc.GetString("rmc-xeno-acid-blood-target-others", ("target", target)), target, Filter.PvsExcept(target), true, PopupType.SmallCaution);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-acid-blood-target-self"), target, target, PopupType.MediumCaution);

            if (_random.NextFloat() < comp.TargetScreamProbability) // TODO: don't activate when target don't feel pain
                _emote.TryEmoteWithChat(target, ScreamProto);
        }
    }

    private void OnGib(Entity<AcidBloodSplashComponent> ent, ref BeingGibbedEvent args)
    {
        if (!ent.Comp.IsActivateSplashOnGib)
            return;

        ActivateSplash(ent.Owner, ent.Comp, ent.Comp.GibSplashRadius);
    }

    private void OnDamageChanged(EntityUid uid, AcidBloodSplashComponent comp, ref DamageChangedEvent args)
    {
        var time = _timing.CurTime;
        if (comp.NextSplashAvailable > time)
            return;

        if (_mobState.IsDead(uid))
            return;

        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        // activate acid splash only when damage is big enough
        if (args.DamageDelta.GetTotal() < comp.MinimalTriggerDamage)
            return;

        var damageDict = args.DamageDelta.DamageDict;
        var triggerProbability = comp.BaseSplashTriggerProbability; // probability of splash activation
        triggerProbability += (float)args.DamageDelta.GetTotal() * comp.DamageTriggerProbabilityMultiplier;

        foreach (var (type, _) in damageDict)
        {
            if (_bruteTypes.Contains(type) && damageDict[type] > 0)
                triggerProbability += comp.BruteDamageProbabilityModificator;
        }

        // TODO: increase probability from sharp and edge weapon + from damage in chest

        if (_random.NextFloat() > triggerProbability)
            return;

        ActivateSplash(uid, comp, comp.StandardSplashRadius);
    }
}
