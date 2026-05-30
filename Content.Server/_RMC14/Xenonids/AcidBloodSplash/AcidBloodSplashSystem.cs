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
using System.Linq;
using Content.Server._RMC14.Decals;
using Content.Server.Spawners.Components;
using Content.Shared.Body.Events;
using Content.Shared.Effects;
using Content.Shared._RMC14.Stun;

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
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
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

    private void ActivateSplash(Entity<AcidBloodSplashComponent> ent, float splashRadius)
    {
        if (!_prototypes.TryIndex(ent.Comp.BloodDecalSpawnerPrototype, out var prototype) ||
            !prototype.TryGetComponent(out RandomDecalSpawnerComponent? spawner, _compFactory) ||
            _rmcDecal.GetDecalsInTile(ent, spawner.Decals) < spawner.MaxDecalsPerTile)
        {
            // create decal, probability inside prototype
            Spawn(ent.Comp.BloodDecalSpawnerPrototype, ent.Owner.ToCoordinates());
        }

        var i = 0; // parity moment, I would prefer a for loop if I knew how to do it in not ugly way.
        var targetsSet = _entityLookup.GetEntitiesInRange(ent.Owner.ToCoordinates(), splashRadius);
        var closeRangeTargets = _entityLookup.GetEntitiesInRange(ent.Owner.ToCoordinates(), ent.Comp.CloseSplashRadius);
        var targetsList = targetsSet.ToList(); // shuffle don't work on HashSet
        _random.Shuffle(targetsList);
        foreach (var target in targetsList)
        {
            if (!_xeno.CanAbilityAttackTarget(ent, target))
                continue;

            var hitProbability = ent.Comp.BaseHitProbability - i * 5;

            if (closeRangeTargets.Contains(target))
                hitProbability += 30;

            hitProbability /= 100f; // Reduce the value to decimal

            if (_random.NextFloat() > hitProbability)
                continue;

            ent.Comp.NextSplashAvailable = _timing.CurTime + ent.Comp.SplashCooldown;
            _damageable.TryChangeDamage(target, _xeno.TryApplyXenoAcidDamageMultiplier(target, ent.Comp.Damage));
            i++;

            _audio.PlayPvs(ent.Comp.AcidSplashSound, target);

            _popup.PopupEntity(Loc.GetString("rmc-xeno-acid-blood-target-others", ("target", target)), target, Filter.PvsExcept(target), true, PopupType.SmallCaution);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-acid-blood-target-self"), target, target, PopupType.MediumCaution);

            // TODO: don't activate when target don't feel pain
            if (_random.NextFloat() < ent.Comp.TargetScreamProbability && !HasComp<RMCUnconsciousComponent>(target))
                _emote.TryEmoteWithChat(target, ScreamProto);

            var filter = Filter.Pvs(target, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == ent.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { target }, filter);
        }
    }

    private void OnGib(Entity<AcidBloodSplashComponent> ent, ref BeingGibbedEvent args)
    {
        if (!ent.Comp.IsActivateSplashOnGib)
            return;

        ActivateSplash(ent, ent.Comp.GibSplashRadius);
    }

    private void OnDamageChanged(Entity<AcidBloodSplashComponent> ent, ref DamageChangedEvent args)
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

        // activate acid splash only when damage is big enough
        if (args.DamageDelta.GetTotal() < ent.Comp.MinimalTriggerDamage)
            return;

        var damageDict = args.DamageDelta.DamageDict;
        var triggerProbability = ent.Comp.BaseSplashTriggerProbability; // probability of splash activation, in percents
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

        // TODO: increase probability from sharp and edge weapon + from damage in chest

        if (_random.NextFloat() > triggerProbability)
            return;

        ActivateSplash(ent, ent.Comp.StandardSplashRadius);
    }
}
