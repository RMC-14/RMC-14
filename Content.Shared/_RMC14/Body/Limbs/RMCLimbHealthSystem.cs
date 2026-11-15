using Content.Shared._RMC14.Body.Zones;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Body.Limbs;

public sealed class RMCLimbHealthSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";

    private readonly List<RMCTargetZone> _limbs = new();
    private readonly Dictionary<RMCTargetZone, float> _weights = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCLimbHealthComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    // decides where the damage lands before armour runs so armour can read it
    private void OnBeforeDamageChanged(Entity<RMCLimbHealthComponent> ent, ref BeforeDamageChangedEvent args)
    {
        // an explicit request goes first then whatever is dealing the damage, anything else lands on a random limb
        var hit = TakeNextHit(ent) ?? AskSource(ent, args.Damage, args.Source) ?? RMCLimbHit.Random;
        hit = hit with { Zone = hit.Zone?.ToDamageZone() };

        // heals pick their limbs when they are applied since they go to whichever limbs are damaged
        if (hit.Mode != RMCLimbHitMode.Spread && IsDamage(args.Damage))
            hit = hit with { Zone = PickZone(ent, hit.Zone, args.Damage, args.Source) };

        ent.Comp.CurrentHit = hit;
        ent.Comp.CurrentHitTick = _timing.CurTick;
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
    ///     only valid during the damage call, armour reads it to know which limb was hit
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

    // the requested limb if the mob still has it, otherwise a random limb weighted by size
    private RMCTargetZone PickZone(Entity<RMCLimbHealthComponent> ent, RMCTargetZone? requested, DamageSpecifier damage, EntityUid? source)
    {
        GetPresentLimbs((ent, ent.Comp, null), _limbs);

        // clients may not know another mob's body parts so every limb is the best guess
        if (_limbs.Count == 0)
            _limbs.AddRange(ent.Comp.Definitions.Keys);

        if (requested is { } zone && _limbs.Contains(zone))
            return zone;

        // same order on client and server so the seeded roll matches
        _limbs.Sort();
        _weights.Clear();
        foreach (var limb in _limbs)
        {
            if (ent.Comp.Definitions.TryGetValue(limb, out var definition) && definition.HitWeight > 0)
                _weights[limb] = definition.HitWeight;
        }

        if (_weights.Count == 0)
            return RMCTargetZone.Chest;

        return SharedRandomExtensions.Pick(_weights, GetRandom(ent, damage, source));
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
