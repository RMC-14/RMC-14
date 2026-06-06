using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Orders;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Devour;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Pheromones;
using Content.Shared._RMC14.Projectiles;
using Content.Shared.Armor;
using Content.Shared.Blocking;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Damage;

public abstract class SharedRMCDamageableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";

    private static readonly ProtoId<DamageTypePrototype> LethalDamageType = "Asphyxiation";
    private readonly HashSet<ProtoId<DamageTypePrototype>> _bruteTypes = new();
    private readonly HashSet<ProtoId<DamageTypePrototype>> _burnTypes = new();
    private readonly List<string> _types = [];

    private EntityQuery<BarricadeComponent> _barricadeQuery;
    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<DamageOverTimeComponent> _damageOverTimeQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<VictimInfectedComponent> _victimInfectedQuery;
    private EntityQuery<XenoNestedComponent> _xenoNestedQuery;

    public override void Initialize()
    {
        _barricadeQuery = GetEntityQuery<BarricadeComponent>();
        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _damageOverTimeQuery = GetEntityQuery<DamageOverTimeComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _victimInfectedQuery = GetEntityQuery<VictimInfectedComponent>();
        _xenoNestedQuery = GetEntityQuery<XenoNestedComponent>();

        SubscribeLocalEvent<DamageMobStateComponent, MapInitEvent>(OnDamageMobStateMapInit);

        SubscribeLocalEvent<DamageOverTimeComponent, StartCollideEvent>(OnDamageOverTimeStartCollide);

        SubscribeLocalEvent<UserDamageOverTimeComponent, EndCollideEvent>(OnDamageOverTimeEndCollide);

        SubscribeLocalEvent<DamageMultiplierFlagsComponent, DamageModifyEvent>(OnMultiplierFlagsDamageModify,
            after:
            [
                typeof(SharedArmorSystem), typeof(BlockingSystem), typeof(InventorySystem), typeof(SharedBorgSystem),
                typeof(SharedMarineOrdersSystem), typeof(CMArmorSystem), typeof(SharedXenoPheromonesSystem),
            ]);

        SubscribeLocalEvent<GunDamageMultipliersComponent, AmmoShotEvent>(OnGunDamageMultipliersAmmoShot);

        SubscribeLocalEvent<MaxDamageComponent, BeforeDamageChangedEvent>(OnMaxBeforeDamageChanged);
        SubscribeLocalEvent<MaxDamageComponent, DamageModifyEvent>(OnMaxDamageModify,
            after:
            [
                typeof(SharedArmorSystem), typeof(BlockingSystem), typeof(InventorySystem), typeof(SharedBorgSystem),
                typeof(SharedMarineOrdersSystem), typeof(CMArmorSystem), typeof(SharedXenoPheromonesSystem),
            ]);

        SubscribeLocalEvent<DamageDealtModifierComponent, GetMeleeDamageEvent>(OnDamageModifierGetMeleeDamage);

        SubscribeLocalEvent<DamageReceivedModifierComponent, DamageModifyEvent>(OnDamageReceivedDamageModify,
            after:
            [
                typeof(SharedArmorSystem), typeof(BlockingSystem), typeof(InventorySystem), typeof(SharedBorgSystem),
                typeof(SharedMarineOrdersSystem), typeof(CMArmorSystem), typeof(SharedXenoPheromonesSystem),
            ]);

        SubscribeLocalEvent<ProjectileDamageReceivedComponent, DamageModifyEvent>(OnProjectileDamageReceivedModify,
            after:
            [
                typeof(SharedArmorSystem), typeof(BlockingSystem), typeof(InventorySystem), typeof(SharedBorgSystem),
                typeof(SharedMarineOrdersSystem), typeof(CMArmorSystem), typeof(SharedXenoPheromonesSystem),
            ]);

        SubscribeLocalEvent<DamageOnPulledWhileCritComponent, MobStateChangedEvent>(OnDamageOnPulledMobState);

        SubscribeLocalEvent<ActiveDamageOnPulledWhileCritComponent, MoveEvent>(OnActiveDamageOnPulledMove);
        SubscribeLocalEvent<ActiveDamageOnPulledWhileCritComponent, MobStateChangedEvent>(OnActiveDamageOnPulledMobState);
        SubscribeLocalEvent<ActiveDamageOnPulledWhileCritComponent, XenoTargetDevouredAttemptEvent>(OnActiveDamageOnPulledDevoured);

        SubscribeLocalEvent<DamageReceivedModifierWhenUnanchoredComponent, DamageModifyEvent>(
            OnDamageReceivedModifierWhenUnanchoredModify,
            after:
            [
                typeof(SharedArmorSystem), typeof(BlockingSystem), typeof(InventorySystem), typeof(SharedBorgSystem),
                typeof(SharedMarineOrdersSystem), typeof(CMArmorSystem), typeof(SharedXenoPheromonesSystem),
            ]
        );

        _bruteTypes.Clear();
        _burnTypes.Clear();

        if (_prototypes.TryIndex(BruteGroup, out var bruteProto))
        {
            foreach (var type in bruteProto.DamageTypes)
            {
                _bruteTypes.Add(type);
            }
        }

        if (_prototypes.TryIndex(BurnGroup, out var burnProto))
        {
            foreach (var type in burnProto.DamageTypes)
            {
                _burnTypes.Add(type);
            }
        }
    }

    private void OnProjectileDamageReceivedModify(Entity<ProjectileDamageReceivedComponent> ent, ref DamageModifyEvent args)
    {
        if (HasComp<ProjectileComponent>(args.Tool))
            args.Damage *= ent.Comp.Multiplier;
    }

    private void OnDamageMobStateMapInit(Entity<DamageMobStateComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.DamageAt = _timing.CurTime + ent.Comp.Cooldown;
        DirtyField(ent, ent.Comp, nameof(ent.Comp.DamageAt));
    }

    private void OnDamageOverTimeStartCollide(Entity<DamageOverTimeComponent> ent, ref StartCollideEvent args)
    {
        if (_net.IsClient)
            return;

        var other = args.OtherEntity;
        if (CanDamage(ent, other))
            EnsureComp<UserDamageOverTimeComponent>(other);
    }

    private void OnDamageOverTimeEndCollide(Entity<UserDamageOverTimeComponent> ent, ref EndCollideEvent args)
    {
        if (_net.IsClient)
            return;

        foreach (var contact in _physics.GetContactingEntities(ent, approximate: true))
        {
            if (_damageOverTimeQuery.HasComp(contact))
                return;
        }

        RemCompDeferred<UserDamageOverTimeComponent>(ent);
    }

    private void OnMultiplierFlagsDamageModify(Entity<DamageMultiplierFlagsComponent> ent, ref DamageModifyEvent args)
    {
        if (!_damageableQuery.HasComp(ent) ||
            !TryComp(args.Tool, out DamageMultipliersComponent? multComponent))
        {
            return;
        }

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

    private void OnDamageModifierGetMeleeDamage(Entity<DamageDealtModifierComponent> ent, ref GetMeleeDamageEvent args)
    {
        args.Damage *= ent.Comp.MeleeMultiplier;
    }

    private void OnDamageReceivedDamageModify(Entity<DamageReceivedModifierComponent> ent, ref DamageModifyEvent args)
    {
        foreach (var (type, amount) in args.Damage.DamageDict)
        {
            if (amount <= FixedPoint2.Zero)
                continue;

            if (ent.Comp.Multiplier != 1)
                args.Damage.DamageDict[type] *= ent.Comp.Multiplier;
            if (ent.Comp.BruteMultiplier != 1 && _bruteTypes.Contains(type))
                args.Damage.DamageDict[type] *= ent.Comp.BruteMultiplier;
            else if (ent.Comp.BurnMultiplier != 1 && _burnTypes.Contains(type))
                args.Damage.DamageDict[type] *= ent.Comp.BurnMultiplier;
        }
    }

    private void OnDamageOnPulledMobState(Entity<DamageOnPulledWhileCritComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        if (EnsureComp(ent, out ActiveDamageOnPulledWhileCritComponent active))
            return;

        active.Damage = ent.Comp.Damage;
        active.PullerWhitelist = ent.Comp.PullerWhitelist;
        active.Every = ent.Comp.Every;
        Dirty(ent, active);
    }

    private void OnActiveDamageOnPulledMove(Entity<ActiveDamageOnPulledWhileCritComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.Damage == null)
            return;

        if (!_rmcPulling.IsBeingPulled(ent.Owner, out var user) ||
            !_entityWhitelist.IsWhitelistPassOrNull(ent.Comp.PullerWhitelist, user))
        {
            if (ent.Comp.Pulled)
            {
                ent.Comp.Pulled = false;
                Dirty(ent);
            }

            return;
        }

        if (!args.NewPosition.TryDistance(EntityManager, _transform, args.OldPosition, out var distance))
            return;

        if (!ent.Comp.Pulled)
            ent.Comp.Pulled = true;

        ent.Comp.Accumulator += distance;
        Dirty(ent);
    }

    private void OnActiveDamageOnPulledMobState(Entity<ActiveDamageOnPulledWhileCritComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            RemCompDeferred<ActiveDamageOnPulledWhileCritComponent>(ent);
    }

    private void OnActiveDamageOnPulledDevoured(Entity<ActiveDamageOnPulledWhileCritComponent> ent, ref XenoTargetDevouredAttemptEvent args)
    {
        if (_mobThresholds.TryGetDeadThreshold(ent.Owner, out var mobThreshold) && TryComp<DamageableComponent>(ent, out var damageable))
        {
            var lethalAmountOfDamage = mobThreshold.Value - damageable.TotalDamage;
            var type = _prototypes.Index<DamageTypePrototype>(LethalDamageType);
            var damage = new DamageSpecifier(type, lethalAmountOfDamage);
            _damageable.TryChangeDamage(ent.Owner, damage, true);
        }

        args.Cancelled = true;
    }

    private void OnDamageReceivedModifierWhenUnanchoredModify(Entity<DamageReceivedModifierWhenUnanchoredComponent> ent, ref DamageModifyEvent args)
    {
        if (!TryComp(ent, out TransformComponent? xform) ||
            xform.Anchored)
        {
            return;
        }

        var total = args.Damage.GetTotal();
        if (total <= FixedPoint2.Zero)
            return;

        args.Damage *= ent.Comp.Multiplier;
    }

    /// <remarks>Do not use when <see cref="equal"/> already has damage types from <see cref="groupId"/></remarks>
    public DamageSpecifier DistributeDamageCached(Entity<DamageableComponent?> damageable, ProtoId<DamageGroupPrototype> groupId, FixedPoint2 amount, DamageSpecifier? equal = null)
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

    public DamageSpecifier DistributeHealing(Entity<DamageableComponent?> damageable, ProtoId<DamageGroupPrototype> groupId, FixedPoint2 amount)
    {
        if (amount > FixedPoint2.Zero)
            amount = -amount;

        return DistributeDamageCached(damageable, groupId, amount);
    }

    /// <remarks>Do not use when <see cref="equal"/> already has damage types from <see cref="groupId"/></remarks>
    public DamageSpecifier DistributeHealingCached(Entity<DamageableComponent?> damageable, ProtoId<DamageGroupPrototype> groupId, FixedPoint2 amount, DamageSpecifier? equal = null)
    {
        if (amount > FixedPoint2.Zero)
            amount = -amount;

        return DistributeDamageCached(damageable, groupId, amount, equal);
    }

    public DamageSpecifier DistributeTypes(Entity<DamageableComponent?> damageable, FixedPoint2 amount)
    {
        var equal = new DamageSpecifier();
        foreach (var group in _prototypes.EnumeratePrototypes<DamageGroupPrototype>())
        {
            equal = DistributeDamageCached(damageable, group.ID, amount, equal);
        }

        return equal;
    }

    public DamageSpecifier DistributeTypesTotal(Entity<DamageableComponent?> damageable, FixedPoint2 amount)
    {
        var equal = new DamageSpecifier();
        foreach (var group in _prototypes.EnumeratePrototypes<DamageGroupPrototype>())
        {
            var total = equal?.GetTotal() ?? FixedPoint2.Zero;
            var left = amount - total;
            if (left <= FixedPoint2.Zero)
            {
                break;
            }

            equal = DistributeDamageCached(damageable, group.ID, left, equal);
        }

        return equal ?? new DamageSpecifier();
    }

    protected virtual void DoEmote(EntityUid ent, ProtoId<EmotePrototype> emote)
    {
    }

    private bool CanDamage(Entity<DamageOverTimeComponent> damage, Entity<MobStateComponent?> target)
    {
        if (damage.Comp.BarricadeDamage != null && _barricadeQuery.HasComp(target))
            return true;

        if (!Resolve(target, ref target.Comp, false))
            return false;

        if (!_entityWhitelist.IsWhitelistPassOrNull(damage.Comp.Whitelist, target))
            return false;

        if (!damage.Comp.AffectsDead && _mobState.IsDead(target))
            return false;

        if (!damage.Comp.AffectsCrit && _mobState.IsCritical(target))
            return false;

        if (!damage.Comp.AffectsInfectedNested &&
            _xenoNestedQuery.HasComp(target) &&
            _victimInfectedQuery.HasComp(target))
        {
            return false;
        }

        return true;
    }

    private void DoDamage(Entity<DamageOverTimeComponent> damageEnt, EntityUid target, DamageSpecifier damage, bool ignoreResistances = false, bool acidic = false)
    {
        var damageBase = damage;

        if (acidic)
            damageBase = _xeno.TryApplyXenoAcidDamageMultiplier(target, damageBase);

        if (damageEnt.Comp.Multipliers is { } multipliers)
        {
            foreach (var multiplier in multipliers)
            {
                if (_entityWhitelist.IsWhitelistPass(multiplier.Whitelist, target))
                {
                    _damageable.TryChangeDamage(target, damageBase * multiplier.Multiplier, ignoreResistances);
                    return;
                }
            }
        }

        _damageable.TryChangeDamage(target, damageBase, ignoreResistances);
    }

    public virtual bool TryGetDestroyedAt(EntityUid destructible, [NotNullWhen(true)] out FixedPoint2? destroyed)
    {
        // TODO RMC14
        destroyed = default;
        return false;
    }

    public bool HasAnyDamage(Entity<DamageableComponent?> damageable, DamageSpecifier damage)
    {
        if (!_damageableQuery.Resolve(damageable, ref damageable.Comp, false))
            return false;

        foreach (var (type, _) in damage.DamageDict)
        {
            if (damageable.Comp.Damage.DamageDict.TryGetValue(type, out var healValue) &&
                healValue > FixedPoint2.Zero)
            {
                return true;
            }
        }

        return false;
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
            DirtyField(uid, comp, nameof(comp.DamageAt));

            if (!_mobStateQuery.TryComp(uid, out var state) ||
                !_damageableQuery.TryComp(uid, out var damageable))
            {
                continue;
            }

            switch (state.CurrentState)
            {
                case MobState.Alive:
                    if (!HasAnyDamage((uid, damageable), comp.NonDeadDamage))
                        break;

                    _damageable.TryChangeDamage(uid, comp.NonDeadDamage, true, damageable: damageable);
                    break;
                case MobState.Critical:
                    _damageable.TryChangeDamage(uid, comp.NonDeadDamage, true, damageable: damageable);
                    var ev = new DamageStateCritBeforeDamageEvent(comp.CritDamage);
                    RaiseLocalEvent(uid, ref ev);
                    _damageable.TryChangeDamage(uid, ev.Damage, true, damageable: damageable);
                    break;
            }
        }

        var pulledQuery = EntityQueryEnumerator<ActiveDamageOnPulledWhileCritComponent>();
        while (pulledQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.Accumulator >= comp.Every)
            {
                var (div, rem) = (comp.Accumulator / comp.Every, comp.Accumulator % comp.Every);
                if (comp.Damage != null)
                    _damageable.TryChangeDamage(uid, comp.Damage * div);

                comp.Accumulator = rem;
            }
        }

        // if this ever runs on the client then Dirty() needs to be added back when
        // the NextDamageAt fields are modified
        if (_net.IsClient)
            return;

        var damageOverTimeQuery = EntityQueryEnumerator<DamageOverTimeComponent>();
        while (damageOverTimeQuery.MoveNext(out var uid, out var damage))
        {
            if (time >= damage.NextDamageAt)
            {
                damage.NextDamageAt = time + damage.DamageEvery;

                var anchoredEnumerator = _rmcMap.GetAnchoredEntitiesEnumerator(uid);
                while (anchoredEnumerator.MoveNext(out var anchored))
                {
                    if (!_barricadeQuery.HasComp(anchored))
                        continue;

                    if (damage.BarricadeDamage == null)
                        continue;

                    var userDamage = EnsureComp<UserDamageOverTimeComponent>(anchored);
                    if (time < userDamage.NextDamageAt)
                        continue;

                    userDamage.NextDamageAt = time;
                    DoDamage((uid, damage), anchored, damage.BarricadeDamage);

                    // EXTREMELY FAITHFUL REMAKE
                    // (i don't want to track this properly right now please god get me out of spitter/boiler code)
                    if (_random.Prob(0.75f))
                        _audio.PlayPvs(damage.BarricadeSound, anchored);
                }
            }

            if (damage.InitDamaged)
                continue;

            damage.InitDamaged = true;
            foreach (var contact in _physics.GetEntitiesIntersectingBody(uid, (int) damage.Collision))
            {
                if (CanDamage((uid, damage), contact))
                    EnsureComp<UserDamageOverTimeComponent>(contact);
            }
        }

        var userDamageOverTimeQuery = EntityQueryEnumerator<UserDamageOverTimeComponent>();
        while (userDamageOverTimeQuery.MoveNext(out var user, out var userDamage))
        {
            if (time < userDamage.NextDamageAt)
                continue;

            var contacts = _physics.GetEntitiesIntersectingBody(user, (int) userDamage.Collision);
            if (contacts.Count == 0)
            {
                RemCompDeferred<UserDamageOverTimeComponent>(user);
                continue;
            }

            foreach (var contact in contacts)
            {
                if (!_damageOverTimeQuery.TryComp(contact, out var damage))
                    continue;

                if (!damage.AffectsDead && _mobState.IsDead(user))
                    continue;

                if (!damage.AffectsCrit && _mobState.IsCritical(user))
                    continue;

                if (!damage.AffectsInfectedNested &&
                    _xenoNestedQuery.HasComp(user) &&
                    _victimInfectedQuery.HasComp(user))
                {
                    continue;
                }

                if (!_entityWhitelist.IsWhitelistPassOrNull(damage.Whitelist, user))
                    continue;

                if (_hive.FromSameHive(contact, user))
                    continue;

                userDamage.NextDamageAt = time + userDamage.DamageEvery;

                if (damage.Damage != null)
                    DoDamage((contact, damage), user, damage.Damage);

                if (damage.ArmorPiercingDamage != null)
                    DoDamage((contact, damage), user, damage.ArmorPiercingDamage, true, acidic: damage.Acidic);

                if (damage.Emotes is { Count: > 0 } emotes)
                {
                    var emote = _random.Pick(emotes);
                    DoEmote(user, emote);
                }

                if (damage.Popup is { } popup && _random.Prob(0.5f))
                    _popup.PopupEntity(popup, user, user, PopupType.SmallCaution);

                _audio.PlayPvs(damage.Sound, user);

                break;
            }
        }
    }
}
