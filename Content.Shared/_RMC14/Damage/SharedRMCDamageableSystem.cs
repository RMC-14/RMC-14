using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Orders;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Pheromones;
using Content.Shared.Armor;
using Content.Shared.Blocking;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Damage;

public abstract class SharedRMCDamageableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<string> _types = [];
    private readonly HashSet<Entity<MarineComponent>> _marines = new();

    private EntityQuery<BarricadeComponent> _barricadeQuery;
    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;

    public override void Initialize()
    {
        _barricadeQuery = GetEntityQuery<BarricadeComponent>();
        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<DamageMobStateComponent, MapInitEvent>(OnDamageMobStateMapInit);

        SubscribeLocalEvent<DamageMultiplierFlagsComponent, DamageModifyEvent>(OnMultiplierFlagsDamageModify,
            after:
            [
                typeof(SharedArmorSystem), typeof(BlockingSystem), typeof(InventorySystem), typeof(SharedBorgSystem),
                typeof(SharedMarineOrdersSystem), typeof(CMArmorSystem), typeof(SharedXenoPheromonesSystem)
            ]);

        SubscribeLocalEvent<GunDamageMultipliersComponent, AmmoShotEvent>(OnGunDamageMultipliersAmmoShot);

        SubscribeLocalEvent<MaxDamageComponent, BeforeDamageChangedEvent>(OnMaxBeforeDamageChanged);
        SubscribeLocalEvent<MaxDamageComponent, DamageModifyEvent>(OnMaxDamageModify,
            after:
            [
                typeof(SharedArmorSystem), typeof(BlockingSystem), typeof(InventorySystem), typeof(SharedBorgSystem),
                typeof(SharedMarineOrdersSystem), typeof(CMArmorSystem), typeof(SharedXenoPheromonesSystem)
            ]);
    }

    private void OnDamageMobStateMapInit(Entity<DamageMobStateComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.DamageAt = _timing.CurTime + ent.Comp.Cooldown;
        Dirty(ent);
    }

    private void OnMultiplierFlagsDamageModify(Entity<DamageMultiplierFlagsComponent> ent, ref DamageModifyEvent args)
    {
        if (!_damageableQuery.TryComp(ent, out var damageable) || !TryComp(args.Tool, out DamageMultipliersComponent? multComponent))
            return;

        foreach (var flag in multComponent.Multipliers.Keys)
        {
            if ((ent.Comp.Flags & flag) == DamageMultiplierFlag.None)
                continue;

            args.Damage *= multComponent.Multipliers[flag];
        }

    }

    private void OnGunDamageMultipliersAmmoShot(Entity<GunDamageMultipliersComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            var comp = EnsureComp<DamageMultipliersComponent>(projectile);
            comp.Multipliers = ent.Comp.Multipliers;
        }
    }

    private void OnMaxBeforeDamageChanged(Entity<MaxDamageComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled ||
            !_damageableQuery.TryComp(ent, out var damageable))
        {
            return;
        }

        if (damageable.TotalDamage >= ent.Comp.Max && args.Damage.GetTotal() > FixedPoint2.Zero)
            args.Cancelled = true;
    }

    private void OnMaxDamageModify(Entity<MaxDamageComponent> ent, ref DamageModifyEvent args)
    {
        if (!_damageableQuery.TryComp(ent, out var damageable))
            return;

        var modifyTotal = args.Damage.GetTotal();
        if (modifyTotal <= FixedPoint2.Zero || damageable.TotalDamage + modifyTotal <= ent.Comp.Max)
            return;

        var remaining = ent.Comp.Max - damageable.TotalDamage;
        if (ent.Comp.Max <= FixedPoint2.Zero)
        {
            args.Damage *= 0;
            return;
        }

        if (modifyTotal <= remaining)
            return;

        args.Damage *= remaining.Float() / modifyTotal.Float();
    }

    public DamageSpecifier DistributeHealing(Entity<DamageableComponent?> damageable, ProtoId<DamageGroupPrototype> groupId, FixedPoint2 amount, DamageSpecifier? equal = null)
    {
        equal ??= new DamageSpecifier();
        if (!_damageableQuery.Resolve(damageable, ref damageable.Comp, false))
            return equal;

        if (!_prototypes.TryIndex(groupId, out var group))
            return equal;

        _types.Clear();
        foreach (var type in group.DamageTypes)
        {
            if (damageable.Comp.Damage.DamageDict.TryGetValue(type, out var current) &&
                current > FixedPoint2.Zero)
            {
                _types.Add(type);
            }
        }

        var damage = equal.DamageDict;
        var add = amount > FixedPoint2.Zero;
        var left = amount;
        while (add ? left > 0 : left < 0)
        {
            var lastLeft = left;
            for (var i = _types.Count - 1; i >= 0; i--)
            {
                var type = _types[i];
                var current = damageable.Comp.Damage.DamageDict[type];

                var existingHeal = add ? -damage.GetValueOrDefault(type) : damage.GetValueOrDefault(type);
                left += existingHeal;
                var toDamage = add
                    ? FixedPoint2.Min(existingHeal + left / (i + 1), current)
                    : -FixedPoint2.Min(-(existingHeal + left / (i + 1)), current);
                if (current <= FixedPoint2.Abs(toDamage))
                    _types.RemoveAt(i);

                damage[type] = toDamage;
                left -= toDamage;
            }

            if (lastLeft == left)
                break;
        }

        return equal;
    }

    public DamageSpecifier DistributeTypes(Entity<DamageableComponent?> damageable, FixedPoint2 amount, DamageSpecifier? equal = null)
    {
        foreach (var group in _prototypes.EnumeratePrototypes<DamageGroupPrototype>())
        {
            equal = DistributeHealing(damageable, group.ID, amount, equal);
        }

        return equal ?? new DamageSpecifier();
    }

    protected virtual void DoEmote(EntityUid ent, ProtoId<EmotePrototype> emote)
    {
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var damageMobStateQuery = EntityQueryEnumerator<DamageMobStateComponent>();
        while (damageMobStateQuery.MoveNext(out var uid, out var comp))
        {
            if (time < comp.DamageAt)
                continue;

            comp.DamageAt = time + comp.Cooldown;
            Dirty(uid, comp);

            if (!_mobStateQuery.TryComp(uid, out var state) ||
                !_damageableQuery.TryComp(uid, out var damageable))
            {
                continue;
            }

            switch (state.CurrentState)
            {
                case MobState.Alive:
                    _damageable.TryChangeDamage(uid, comp.NonDeadDamage, true, damageable: damageable);
                    break;
                case MobState.Critical:
                    _damageable.TryChangeDamage(uid, comp.NonDeadDamage, true, damageable: damageable);
                    _damageable.TryChangeDamage(uid, comp.CritDamage, true, damageable: damageable);
                    break;
            }
        }

        if (_net.IsClient)
            return;

        var damageOverTimeQuery = EntityQueryEnumerator<DamageOverTimeComponent>();
        while (damageOverTimeQuery.MoveNext(out var uid, out var damage))
        {
            if (time < damage.NextDamageAt)
                continue;

            damage.NextDamageAt = time + damage.DamageEvery;
            Dirty(uid, damage);

            _marines.Clear();
            _entityLookup.GetEntitiesInRange(uid.ToCoordinates(), 0.5f, _marines);
            foreach (var marine in _marines)
            {
                if (!damage.AffectsDead && _mobState.IsDead(marine))
                    continue;

                if (!damage.AffectsInfectedNested &&
                    HasComp<XenoNestedComponent>(marine) &&
                    HasComp<VictimInfectedComponent>(marine))
                {
                    continue;
                }

                _damageable.TryChangeDamage(marine, damage.Damage);

                if (damage.ArmorPiercingDamage != null)
                    _damageable.TryChangeDamage(marine, damage.ArmorPiercingDamage, true);

                if (damage.Emote is { } emote)
                    DoEmote(marine, emote);

                if (damage.Popup is { } popup)
                    _popup.PopupEntity(popup, marine, marine);
            }

            var anchoredEnumerator = _rmcMap.GetAnchoredEntitiesEnumerator(uid);
            while (anchoredEnumerator.MoveNext(out var anchored))
            {
                if (!_barricadeQuery.HasComp(anchored))
                    continue;

                _damageable.TryChangeDamage(anchored, damage.BarricadeDamage);

                // EXTREMELY FAITHFUL REMAKE
                // (i don't want to track this properly right now please god get me out of spitter/boiler code)
                if (_random.Prob(0.75f))
                    _audio.PlayPvs(damage.BarricadeSound, uid);
            }
        }
    }
}
