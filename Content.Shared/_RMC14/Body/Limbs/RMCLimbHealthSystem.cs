using Content.Shared._RMC14.Body.Zones;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Body.Limbs;

public sealed class RMCLimbHealthSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";

    private readonly List<RMCTargetZone> _limbs = new();
    private readonly List<RMCTargetZone> _picked = new();
    private readonly Dictionary<RMCTargetZone, float> _weights = new();

    private enum LimbGroup
    {
        Brute,
        Burn,
    }

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCLimbHealthComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<RMCLimbHealthComponent, DamageChangedEvent>(OnDamageChanged);
    }

    // decides where the damage lands before armour runs so armour can read it
    private void OnBeforeDamageChanged(Entity<RMCLimbHealthComponent> ent, ref BeforeDamageChangedEvent args)
    {
        // an explicit request goes first then whatever is dealing the damage, anything else lands on a random limb
        var hit = TakeNextHit(ent) ?? AskSource(ent, args.Damage, args.Source) ?? RMCLimbHit.Random;
        hit = hit with { Zone = hit.Zone?.ToDamageZone() };

        // heals and spread damage pick their limbs when they are applied
        if (hit.Mode != RMCLimbHitMode.Spread && IsDamage(args.Damage))
            hit = hit with { Zone = PickZone(ent, hit, args.Damage, args.Source) };

        ent.Comp.CurrentHit = hit;
        ent.Comp.CurrentHitTick = _timing.CurTick;
    }

    // puts the change onto the limbs then makes sure they still add up to the body
    private void OnDamageChanged(Entity<RMCLimbHealthComponent> ent, ref DamageChangedEvent args)
    {
        // the server owns the limbs and sends them out, clients also get this event when damage state arrives
        if (_net.IsClient)
            return;

        var changed = false;
        if (args.DamageDelta is { } delta)
        {
            var hit = GetCurrentHit((ent, ent.Comp)) ?? RMCLimbHit.Random;
            var (brute, burn) = GetBruteBurn(delta);
            changed |= ApplyChange(ent, hit, LimbGroup.Brute, brute);
            changed |= ApplyChange(ent, hit, LimbGroup.Burn, burn);
        }

        // damage set directly has no delta and splitting can round, so the limbs always catch up to the body
        var (bodyBrute, bodyBurn) = GetBruteBurn(args.Damageable.Damage);
        changed |= ApplyChange(ent, RMCLimbHit.Random, LimbGroup.Brute, bodyBrute - SumLimbs(ent, LimbGroup.Brute));
        changed |= ApplyChange(ent, RMCLimbHit.Random, LimbGroup.Burn, bodyBurn - SumLimbs(ent, LimbGroup.Burn));

        if (changed)
            Dirty(ent);
    }

    /// <summary>
    ///     sets where the next damage on this mob lands, call it right before the damage call
    ///     it only counts for damage dealt in the same tick
    /// </summary>
    public void SetNextHit(Entity<RMCLimbHealthComponent?> ent, RMCLimbHit hit)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.NextHit = hit;
        ent.Comp.NextHitTick = _timing.CurTick;
    }

    /// <summary>
    ///     where the damage being applied to this mob right now lands
    ///     only valid during the damage call including damage changed handlers, armour reads it to know which limb was hit
    ///     will not go wrong
    /// </summary>
    public RMCLimbHit? GetCurrentHit(Entity<RMCLimbHealthComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.CurrentHitTick != _timing.CurTick)
            return null;

        return ent.Comp.CurrentHit;
    }

    private RMCLimbHit? TakeNextHit(Entity<RMCLimbHealthComponent> ent)
    {
        var hit = ent.Comp.NextHit;
        ent.Comp.NextHit = null;

        // a request left over from an earlier tick never got used
        return ent.Comp.NextHitTick == _timing.CurTick ? hit : null;
    }

    private RMCLimbHit? AskSource(EntityUid target, DamageSpecifier damage, EntityUid? source)
    {
        // never ask the mob itself
        if (source is not { } sourceUid || sourceUid == target || TerminatingOrDeleted(sourceUid))
            return null;

        var ev = new RMCGetLimbHitEvent(target, damage);
        RaiseLocalEvent(sourceUid, ref ev);
        return ev.Hit;
    }

    // the aimed limb by its aim chance otherwise a random other limb picked the way the hit mode says, like cm13's rand_zone
    private RMCTargetZone PickZone(Entity<RMCLimbHealthComponent> ent, RMCLimbHit hit, DamageSpecifier damage, EntityUid? source)
    {
        FillWeights(ent, hit.Mode);
        var random = GetRandom(ent, damage, source);
        if (hit.Zone is { } zone)
        {
            if (_limbs.Contains(zone) && random.NextFloat() < hit.AimChance)
                return zone;

            _weights.Remove(zone);
        }

        // the aimed limb is the only one left so it lands there anyway
        if (_weights.Count == 0)
            return hit.Zone is { } aimed && _limbs.Contains(aimed) ? aimed : RMCTargetZone.Chest;

        return SharedRandomExtensions.Pick(_weights, random);
    }

    // only used on the server so it doesn't need a seed
    private RMCTargetZone RandomLimb(Entity<RMCLimbHealthComponent> ent, RMCLimbHitMode mode)
    {
        FillWeights(ent, mode);
        return _weights.Count == 0 ? RMCTargetZone.Chest : _random.Pick(_weights);
    }

    // fills _limbs with the limbs the mob still has and _weights with how likely the hit lands on each
    private void FillWeights(Entity<RMCLimbHealthComponent> ent, RMCLimbHitMode mode)
    {
        GetPresentOrAllLimbs(ent, _limbs);
        _weights.Clear();
        foreach (var limb in _limbs)
        {
            if (!ent.Comp.Definitions.TryGetValue(limb, out var definition))
                continue;

            var weight = mode switch
            {
                // cm13's explosions focus on any limb
                RMCLimbHitMode.Explosion => 1f,
                RMCLimbHitMode.AnyLimb => IsAtMax(ent, limb) ? 0f : 1f,
                _ => definition.HitWeight,
            };

            if (weight > 0)
                _weights[limb] = weight;
        }

        // every limb is at max so any limb has to do
        if (_weights.Count == 0 && mode == RMCLimbHitMode.AnyLimb)
        {
            foreach (var limb in _limbs)
            {
                _weights[limb] = 1f;
            }
        }
    }

    private void GetPresentOrAllLimbs(Entity<RMCLimbHealthComponent> ent, List<RMCTargetZone> limbs)
    {
        GetPresentLimbs((ent, ent.Comp, null), limbs);

        // clients may not know another mob's body parts so every limb is the best guess
        if (limbs.Count == 0)
            limbs.AddRange(ent.Comp.Definitions.Keys);

        // same order on client and server so seeded rolls match
        limbs.Sort();
    }

    // seeded the same on client and server so predicted hits roll the same limb
    private System.Random GetRandom(EntityUid target, DamageSpecifier damage, EntityUid? source)
    {
        var sourceId = TryGetNetEntity(source, out var sourceNet) ? sourceNet.Value.Id : 0;
        var seed = unchecked(
            (int)_timing.CurTick.Value * 397 ^
            GetNetEntity(target).Id * 31 ^
            sourceId * 17 ^
            damage.GetTotal().Value);

        return new System.Random(seed);
    }

    private bool ApplyChange(Entity<RMCLimbHealthComponent> ent, RMCLimbHit hit, LimbGroup group, FixedPoint2 amount)
    {
        if (amount > FixedPoint2.Zero)
            DamageLimbs(ent, hit, group, amount);
        else if (amount < FixedPoint2.Zero)
            HealLimbs(ent, hit, group, -amount);
        else
            return false;

        return true;
    }

    private void DamageLimbs(Entity<RMCLimbHealthComponent> ent, RMCLimbHit hit, LimbGroup group, FixedPoint2 amount)
    {
        switch (hit.Mode)
        {
            case RMCLimbHitMode.Spread:
                SpreadDamage(ent, group, amount, hit.SpreadChance);
                break;
            case RMCLimbHitMode.Explosion:
                ExplosionDamage(ent, hit.Zone, group, amount);
                break;
            default:
                AddLimbDamage(ent, hit.Zone ?? RandomLimb(ent, hit.Mode), group, amount);
                break;
        }
    }

    // every limb that isn't at max has a chance to take an equal share
    private void SpreadDamage(Entity<RMCLimbHealthComponent> ent, LimbGroup group, FixedPoint2 amount, float chance)
    {
        GetPresentOrAllLimbs(ent, _limbs);
        _picked.Clear();
        foreach (var limb in _limbs)
        {
            if (!IsAtMax(ent, limb) && _random.Prob(chance))
                _picked.Add(limb);
        }

        // nothing rolled in but the body already took the damage so it still has to land somewhere
        if (_picked.Count == 0)
            _picked.AddRange(_limbs);

        var share = amount / _picked.Count;
        for (var i = 0; i < _picked.Count; i++)
        {
            // the last limb takes what rounding left over so the total stays exact
            var part = i == _picked.Count - 1 ? amount - share * (_picked.Count - 1) : share;
            AddLimbDamage(ent, _picked[i], group, part);
        }
    }

    // half on one limb then each limb's explosion weight of the other half on top
    // so the focus limb gets 1 part and every limb its weight in parts, mostly the chest abdomen and head
    private void ExplosionDamage(Entity<RMCLimbHealthComponent> ent, RMCTargetZone? focus, LimbGroup group, FixedPoint2 amount)
    {
        var focusZone = focus ?? RandomLimb(ent, RMCLimbHitMode.Explosion);
        GetPresentOrAllLimbs(ent, _limbs);
        var total = 0f;
        foreach (var limb in _limbs)
        {
            total += ent.Comp.Definitions.GetValueOrDefault(limb).ExplosionWeight;
        }

        var focused = amount / (1 + total);
        AddLimbDamage(ent, focusZone, group, focused);

        var rest = amount - focused;
        if (total <= 0)
        {
            AddLimbDamage(ent, focusZone, group, rest);
            return;
        }

        var given = FixedPoint2.Zero;
        for (var i = 0; i < _limbs.Count; i++)
        {
            var weight = ent.Comp.Definitions.GetValueOrDefault(_limbs[i]).ExplosionWeight;
            var part = i == _limbs.Count - 1 ? rest - given : rest * (weight / total);
            given += part;
            AddLimbDamage(ent, _limbs[i], group, part);
        }
    }

    private void HealLimbs(Entity<RMCLimbHealthComponent> ent, RMCLimbHit hit, LimbGroup group, FixedPoint2 amount)
    {
        // a heal aimed at one limb starts there
        if (hit.Mode == RMCLimbHitMode.Limb && hit.Zone is { } zone)
            amount -= HealLimb(ent, zone, group, amount);

        // whatever is left comes off damaged limbs one random limb at a time like cm13's heal_overall_damage
        while (amount > FixedPoint2.Zero && TryPickDamagedLimb(ent, group, out var limb))
        {
            amount -= HealLimb(ent, limb, group, amount);
        }
    }

    private FixedPoint2 HealLimb(Entity<RMCLimbHealthComponent> ent, RMCTargetZone zone, LimbGroup group, FixedPoint2 amount)
    {
        var limb = ent.Comp.Limbs.GetValueOrDefault(zone);
        var healed = FixedPoint2.Min(GetGroup(limb, group), amount);
        if (healed > FixedPoint2.Zero)
            SetLimb(ent, zone, WithGroup(limb, group, GetGroup(limb, group) - healed));

        return healed;
    }

    private void GetDamagedLimbs(Entity<RMCLimbHealthComponent> ent, LimbGroup group, List<RMCTargetZone> limbs)
    {
        limbs.Clear();
        foreach (var (zone, limb) in ent.Comp.Limbs)
        {
            if (GetGroup(limb, group) > FixedPoint2.Zero)
                limbs.Add(zone);
        }
    }

    private bool TryPickDamagedLimb(Entity<RMCLimbHealthComponent> ent, LimbGroup group, out RMCTargetZone zone)
    {
        GetDamagedLimbs(ent, group, _picked);
        if (_picked.Count == 0)
        {
            zone = default;
            return false;
        }

        zone = _random.Pick(_picked);
        return true;
    }

    private void AddLimbDamage(Entity<RMCLimbHealthComponent> ent, RMCTargetZone zone, LimbGroup group, FixedPoint2 amount)
    {
        var limb = ent.Comp.Limbs.GetValueOrDefault(zone);
        SetLimb(ent, zone, WithGroup(limb, group, GetGroup(limb, group) + amount));
    }

    // undamaged limbs drop their entry so only damaged limbs get sent to clients
    private static void SetLimb(Entity<RMCLimbHealthComponent> ent, RMCTargetZone zone, RMCLimb limb)
    {
        if (limb.Brute == FixedPoint2.Zero && limb.Burn == FixedPoint2.Zero)
            ent.Comp.Limbs.Remove(zone);
        else
            ent.Comp.Limbs[zone] = limb;
    }

    private FixedPoint2 SumLimbs(Entity<RMCLimbHealthComponent> ent, LimbGroup group)
    {
        var total = FixedPoint2.Zero;
        foreach (var limb in ent.Comp.Limbs.Values)
        {
            total += GetGroup(limb, group);
        }

        return total;
    }

    private static bool IsAtMax(Entity<RMCLimbHealthComponent> ent, RMCTargetZone zone)
    {
        var limb = ent.Comp.Limbs.GetValueOrDefault(zone);
        return ent.Comp.Definitions.TryGetValue(zone, out var definition) &&
               limb.Brute + limb.Burn >= definition.MaxHealth;
    }

    private static FixedPoint2 GetGroup(RMCLimb limb, LimbGroup group)
    {
        return group == LimbGroup.Burn ? limb.Burn : limb.Brute;
    }

    private static RMCLimb WithGroup(RMCLimb limb, LimbGroup group, FixedPoint2 value)
    {
        return group == LimbGroup.Burn ? limb with { Burn = value } : limb with { Brute = value };
    }

    // whether this adds brute or burn rather than healing it or only touching other damage types
    private bool IsDamage(DamageSpecifier damage)
    {
        var (brute, burn) = GetBruteBurn(damage);
        return brute > 0 || burn > 0;
    }

    public (FixedPoint2 Brute, FixedPoint2 Burn) GetBruteBurn(DamageSpecifier damage)
    {
        damage.TryGetDamageInGroup(_prototype.Index(BruteGroup), out var brute);
        damage.TryGetDamageInGroup(_prototype.Index(BurnGroup), out var burn);
        return (brute, burn);
    }

    /// <summary>
    ///     the damage on a limb, eyes and mouth count as the head
    /// </summary>
    public RMCLimb GetLimb(Entity<RMCLimbHealthComponent?> ent, RMCTargetZone zone)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return default;

        return ent.Comp.Limbs.GetValueOrDefault(zone.ToDamageZone());
    }

    public bool TryGetDefinition(Entity<RMCLimbHealthComponent?> ent, RMCTargetZone zone, out RMCLimbDefinition definition)
    {
        definition = default;
        return Resolve(ent, ref ent.Comp, false) &&
               ent.Comp.Definitions.TryGetValue(zone.ToDamageZone(), out definition);
    }

    /// <summary>
    ///     the limb this one hangs off, null for the chest
    /// </summary>
    public RMCTargetZone? GetParent(Entity<RMCLimbHealthComponent?> ent, RMCTargetZone zone)
    {
        return TryGetDefinition(ent, zone, out var definition) ? definition.Parent : null;
    }

    /// <summary>
    ///     the limbs that hang off this one
    /// </summary>
    public void GetChildren(Entity<RMCLimbHealthComponent?> ent, RMCTargetZone zone, List<RMCTargetZone> children)
    {
        children.Clear();
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        zone = zone.ToDamageZone();
        foreach (var (child, definition) in ent.Comp.Definitions)
        {
            if (definition.Parent == zone)
                children.Add(child);
        }
    }

    /// <summary>
    ///     whether the mob still has the body part for this limb, a missing part means a missing limb
    /// </summary>
    public bool HasLimb(Entity<RMCLimbHealthComponent?, BodyComponent?> ent, RMCTargetZone zone)
    {
        if (!Resolve(ent, ref ent.Comp1, false) ||
            !Resolve(ent, ref ent.Comp2, false))
        {
            return false;
        }

        zone = zone.ToDamageZone();
        if (!ent.Comp1.Definitions.ContainsKey(zone))
            return false;

        var (type, symmetry) = zone.ToBodyPart();
        foreach (var part in _body.GetBodyChildrenOfType(ent, type, ent.Comp2))
        {
            if (part.Component.Symmetry == symmetry)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     every limb the mob still has
    /// </summary>
    public void GetPresentLimbs(Entity<RMCLimbHealthComponent?, BodyComponent?> ent, List<RMCTargetZone> limbs)
    {
        limbs.Clear();
        if (!Resolve(ent, ref ent.Comp1, false) ||
            !Resolve(ent, ref ent.Comp2, false))
        {
            return;
        }

        foreach (var part in _body.GetBodyChildren(ent, ent.Comp2))
        {
            if (part.Component.PartType.ToZone(part.Component.Symmetry) is not { } zone)
                continue;

            if (ent.Comp1.Definitions.ContainsKey(zone))
                limbs.Add(zone);

            // TODO RMC14 abdomen body part, the torso carries the abdomen until it gets its own part
            if (zone == RMCTargetZone.Chest && ent.Comp1.Definitions.ContainsKey(RMCTargetZone.Abdomen))
                limbs.Add(RMCTargetZone.Abdomen);
        }
    }
}
