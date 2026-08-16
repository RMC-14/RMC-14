using System.Numerics;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Folded;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.PowerLoader;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Random;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Foldable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chair;

public sealed class SharedRMCChairStackSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCFoldableSystem _rmcFoldable = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly HashSet<Entity<MobStateComponent>> _nearbyMobs = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCChairStackComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCChairStackComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCChairStackComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<RMCChairStackComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<RMCChairStackComponent, EntityTerminatingEvent>(OnTerminating);

        SubscribeLocalEvent<RMCChairStackComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCChairStackComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<RMCChairStackComponent, FoldAttemptEvent>(OnFoldAttempt);
        SubscribeLocalEvent<RMCChairStackComponent, StrapAttemptEvent>(OnStrapAttempt);

        SubscribeLocalEvent<RMCChairStackComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<RMCChairStackComponent, ExplosionReceivedEvent>(OnExplosionReceived);
        SubscribeLocalEvent<RMCChairStackComponent, ThrowHitByEvent>(OnThrowHitBy);
        SubscribeLocalEvent<RMCChairStackComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<RMCChairStackComponent, RMCBeforeProjectileAccuracyEvent>(OnBeforeProjectileAccuracy);

        SubscribeLocalEvent<RMCChairStackComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUp,
            after: new[] { typeof(PowerLoaderSystem) });
        SubscribeLocalEvent<RMCChairStackComponent, AfterInteractEvent>(OnAfterInteract,
            after: new[] { typeof(PowerLoaderSystem) });
    }

    private void OnStartup(Entity<RMCChairStackComponent> ent, ref ComponentStartup args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    private void OnMapInit(Entity<RMCChairStackComponent> ent, ref MapInitEvent args)
    {
        RefreshState(ent);
    }

    private void OnContainerChanged(Entity<RMCChairStackComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.ContainerId && !ent.Comp.Collapsing)
            RefreshState(ent);
    }

    private void OnContainerChanged(Entity<RMCChairStackComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.ContainerId && !ent.Comp.Collapsing)
            RefreshState(ent);
    }

    private void OnTerminating(Entity<RMCChairStackComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!_net.IsServer || ent.Comp.Collapsing || ent.Comp.StackedCount == 0)
            return;

        var parent = Transform(ent).ParentUid;
        if (TryComp(parent, out MetaDataComponent? parentMetadata) &&
            parentMetadata.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            return;
        }

        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            _container.EmptyContainer(container, true, _transform.GetMoverCoordinates(ent));
    }

    private void OnInteractUsing(Entity<RMCChairStackComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.Used == ent.Owner)
            return;

        if (!TryComp(args.Used, out RMCChairStackComponent? usedStack))
            return;

        args.Handled = true;
        TryStack(ent, (args.Used, usedStack), args.User, true);
    }

    private void OnInteractHand(Entity<RMCChairStackComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || ent.Comp.StackedCount == 0)
            return;

        args.Handled = TryUnstack(ent, args.User);
    }

    private void OnFoldAttempt(Entity<RMCChairStackComponent> ent, ref FoldAttemptEvent args)
    {
        if (ent.Comp.StackedCount == 0)
            return;

        args.Cancelled = true;
        _popup.PopupClient(Loc.GetString("rmc-chair-stack-cant-fold"), ent, PopupType.SmallCaution);
    }

    private void OnStrapAttempt(Entity<RMCChairStackComponent> ent, ref StrapAttemptEvent args)
    {
        if (ent.Comp.StackedCount == 0)
            return;

        args.Cancelled = true;
        if (args.Popup && args.User != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-chair-stack-cant-buckle"), ent, args.User.Value,
                PopupType.SmallCaution);
        }
    }

    private void OnAttacked(Entity<RMCChairStackComponent> ent, ref AttackedEvent args)
    {
        if (ent.Comp.StackedCount > 0 && HasComp<XenoComponent>(args.User))
            Collapse(ent);
    }

    private void OnExplosionReceived(Entity<RMCChairStackComponent> ent, ref ExplosionReceivedEvent args)
    {
        if (ent.Comp.StackedCount > 0)
            Collapse(ent);
    }

    private void OnThrowHitBy(Entity<RMCChairStackComponent> ent, ref ThrowHitByEvent args)
    {
        if (ent.Comp.StackedCount == 0)
            return;

        if (TryComp(args.Thrown, out MobStateComponent? mobState))
        {
            if (mobState.CurrentState == MobState.Dead)
                return;

            if (HasComp<HumanoidAppearanceComponent>(args.Thrown))
            {
                _stun.TryStun(args.Thrown, ent.Comp.CollisionStun, true);
                _stun.TryKnockdown(args.Thrown, ent.Comp.CollisionStun, true);
            }

            Collapse(ent);
            return;
        }

        if (IsUnstable(ent.Comp) && _net.IsServer && _random.Prob(ent.Comp.ThrownItemCollapseChance))
            Collapse(ent);
    }

    private void OnDestruction(Entity<RMCChairStackComponent> ent, ref DestructionEventArgs args)
    {
        if (!_net.IsServer)
            return;

        if (ent.Comp.StackedCount > 0)
        {
            Collapse(ent);
            return;
        }

        _audio.PlayPvs(ent.Comp.DestructionSound, ent);
        Spawn(ent.Comp.DestructionDrop, _transform.GetMoverCoordinates(ent));
    }

    private void OnBeforeProjectileAccuracy(Entity<RMCChairStackComponent> ent,
        ref RMCBeforeProjectileAccuracyEvent args)
    {
        if (ent.Comp.StackedCount == 0 ||
            !TryComp(args.Projectile, out RMCProjectileAccuracyComponent? projectile))
        {
            return;
        }

        var targetSeed = (long) projectile.Tick << 32 | GetNetEntity(ent).Id;
        var roll = new Xoshiro128P(projectile.GunSeed, targetSeed).NextFloat();
        if (roll >= ent.Comp.ProjectileCoverage)
            args.GuaranteedMiss = true;
    }

    private void OnGettingPickedUp(Entity<RMCChairStackComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled || !IsUnstable(ent.Comp) || !HasComp<PowerLoaderComponent>(args.User))
            return;

        if (!_net.IsServer)
            return;

        if (_random.Prob(GetPowerLoaderCollapseChance(ent.Comp, args.User)))
        {
            args.Cancel();
            Collapse(ent);
            return;
        }

        _audio.PlayPvs(ent.Comp.PowerLoaderPickupSound, ent);
    }

    private void OnAfterInteract(Entity<RMCChairStackComponent> ent, ref AfterInteractEvent args)
    {
        if (!_net.IsServer ||
            args.Used != ent.Owner ||
            !HasComp<PowerLoaderComponent>(args.User) ||
            _hands.IsHolding(args.User, ent.Owner))
        {
            return;
        }

        _transform.SetWorldRotation(ent, _transform.GetWorldRotation(args.User));
        _audio.PlayPvs(ent.Comp.PowerLoaderDropSound, ent);

        if (IsUnstable(ent.Comp) && _random.Prob(GetPowerLoaderCollapseChance(ent.Comp, args.User)))
            Collapse(ent);
    }

    public bool TryStack(Entity<RMCChairStackComponent> target,
        Entity<RMCChairStackComponent> folded,
        EntityUid user,
        bool popup = false)
    {
        if (!CanStack(target, folded, user, out var message))
        {
            if (popup && message != null)
                _popup.PopupClient(Loc.GetString(message), target, user, PopupType.SmallCaution);

            return false;
        }

        var container = _container.EnsureContainer<Container>(target, target.Comp.ContainerId);
        if (!_container.Insert(folded.Owner, container))
            return false;

        RefreshState(target);
        _popup.PopupPredicted(Loc.GetString("rmc-chair-stack-add", ("chair", folded.Owner)), target, user);

        if (!IsUnstable(target.Comp))
            return true;

        _popup.PopupPredicted(Loc.GetString("rmc-chair-stack-unstable"), target, user, PopupType.MediumCaution);
        if (_net.IsServer &&
            _random.Prob(GetStackCollapseChance(target.Comp.StackedCount, target.Comp.StackCollapseChanceFactor)))
        {
            Collapse(target);
        }

        return true;
    }

    public bool TryUnstack(Entity<RMCChairStackComponent> ent, EntityUid user)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var baseContainer) ||
            baseContainer is not Container container ||
            container.ContainedEntities.Count == 0)
        {
            return false;
        }

        var chair = container.ContainedEntities[^1];
        var activeHand = _hands.GetActiveHand(user);
        if (activeHand == null ||
            !_hands.CanPickupToHand(user, chair, activeHand, checkActionBlocker: false))
        {
            return false;
        }

        if (!_container.Remove(chair, container, destination: _transform.GetMoverCoordinates(ent)))
            return false;

        if (!_hands.TryPickup(user, chair, activeHand, checkActionBlocker: false))
        {
            _container.Insert(chair, container, force: true);
            return false;
        }

        RefreshState(ent);
        _popup.PopupPredicted(Loc.GetString("rmc-chair-stack-remove", ("chair", chair)), ent, user);
        return true;
    }

    public void Collapse(Entity<RMCChairStackComponent> ent)
    {
        if (!_net.IsServer || ent.Comp.Collapsing || ent.Comp.StackedCount == 0)
            return;

        ent.Comp.Collapsing = true;
        var coords = _transform.GetMoverCoordinates(ent);
        var chairs = new List<EntityUid>();

        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            chairs.AddRange(_container.EmptyContainer(container, true, coords));

        var baseChair = Spawn(ent.Comp.CollapsedPrototype, coords);
        _transform.Unanchor(baseChair);

        _popup.PopupEntity(Loc.GetString("rmc-chair-stack-collapse", ("tower", ent.Owner)), ent,
            Robust.Shared.Player.Filter.Pvs(ent), true, PopupType.LargeCaution);
        _audio.PlayPvs(ent.Comp.CollapseSound, coords);

        var remainingChairs = chairs.Count;
        foreach (var chair in chairs)
        {
            remainingChairs--;
            var radius = GetCollapseScatterRadius(remainingChairs, ent.Comp.ScatterDivisor);
            if (radius == 0)
                continue;

            var direction = GetCollapseThrowDirection(radius, ent.Comp.MinThrowRange, ent.Comp.MaxThrowRange);
            _throwing.TryThrow(chair, direction, ent.Comp.ThrowSpeed, compensateFriction: true,
                playSound: true, unanchor: true);
        }

        QueueDel(ent);
    }

    public static bool IsUnstable(RMCChairStackComponent component)
    {
        return IsUnstable(component.StackedCount, component.UnstableThreshold);
    }

    public static bool IsUnstable(int stackedCount, int unstableThreshold = 8)
    {
        return stackedCount > unstableThreshold;
    }

    public static float GetStackCollapseChance(int stackedCount, float factor = 50)
    {
        return Math.Clamp(MathF.Sqrt(factor * stackedCount) / 100f, 0, 1);
    }

    public static int GetCollapseScatterRadius(int remainingChairs, int divisor = 2)
    {
        return Math.Max(0, remainingChairs) / Math.Max(1, divisor);
    }

    private Vector2 GetCollapseThrowDirection(int radius, int minRange, int maxRange)
    {
        int x;
        int y;
        do
        {
            x = _random.Next(-radius, radius + 1);
            y = _random.Next(-radius, radius + 1);
        } while (x == 0 && y == 0);

        var direction = new Vector2(x, y);
        var throwRange = _random.Next(minRange, maxRange + 1);
        var targetRange = Math.Max(Math.Abs(x), Math.Abs(y));
        if (targetRange > throwRange)
            direction *= throwRange / (float) targetRange;

        return direction;
    }

    private bool CanStack(Entity<RMCChairStackComponent> target,
        Entity<RMCChairStackComponent> folded,
        EntityUid user,
        out string? message)
    {
        message = null;

        if (target.Owner == folded.Owner ||
            target.Comp.Collapsing ||
            folded.Comp.Collapsing ||
            TerminatingOrDeleted(target) ||
            TerminatingOrDeleted(folded) ||
            folded.Comp.StackedCount != 0 ||
            !TryComp(target, out FoldableComponent? targetFoldable) ||
            targetFoldable.IsFolded ||
            !TryComp(folded, out FoldableComponent? foldedFoldable) ||
            !foldedFoldable.IsFolded ||
            TryComp(folded, out WieldableComponent? wieldable) && wieldable.Wielded)
        {
            message = "rmc-chair-stack-invalid";
            return false;
        }

        if (TryComp(target, out StrapComponent? strap) && strap.BuckledEntities.Count > 0)
        {
            message = "rmc-chair-stack-occupied";
            return false;
        }

        var coordinates = Transform(target).Coordinates;
        var tile = _turf.GetTileRef(coordinates);
        var mapCoordinates = _transform.ToMapCoordinates(coordinates);
        _nearbyMobs.Clear();
        _lookup.GetEntitiesInRange(mapCoordinates, 1, _nearbyMobs);
        var occupied = false;
        foreach (var mob in _nearbyMobs)
        {
            var mobTile = _turf.GetTileRef(Transform(mob).Coordinates);
            if (mob.Comp.CurrentState == MobState.Dead ||
                tile?.GridUid != mobTile?.GridUid ||
                tile?.GridIndices != mobTile?.GridIndices)
                continue;

            occupied = true;
            break;
        }
        _nearbyMobs.Clear();

        if (!occupied)
            return true;

        message = "rmc-chair-stack-occupied";
        return false;
    }

    private float GetPowerLoaderCollapseChance(RMCChairStackComponent component, EntityUid loader)
    {
        var highestSkill = 1;
        if (TryComp(loader, out StrapComponent? strap))
        {
            foreach (var operatorUid in strap.BuckledEntities)
            {
                highestSkill = Math.Max(highestSkill, _skills.GetSkill(operatorUid, component.PowerLoaderSkill));
            }
        }

        return GetPowerLoaderCollapseChance(component.PowerLoaderCollapseChance, highestSkill);
    }

    public static float GetPowerLoaderCollapseChance(float baseChance, int skill)
    {
        return Math.Clamp(baseChance / Math.Max(1, skill), 0, 1);
    }

    private void RefreshState(Entity<RMCChairStackComponent> ent)
    {
        var container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        var count = container.Count;
        if (ent.Comp.StackedCount != count)
        {
            ent.Comp.StackedCount = count;
            Dirty(ent);
        }

        _appearance.SetData(ent, RMCChairStackVisuals.Count, count);

        if (_net.IsClient)
            return;

        SetStackPhysics(ent, count > 0);

        if (TryComp(ent, out FoldableComponent? foldable))
        {
            _rmcFoldable.TryLockFold(ent, count > 0, foldable);
            Dirty(ent, foldable);
        }

        if (TryComp(ent, out StrapComponent? strap))
        {
            var enableStrap = count == 0 &&
                              (!TryComp(ent, out FoldableComponent? foldableForStrap) ||
                               !foldableForStrap.IsFolded);
            _buckle.StrapSetEnabled(ent, enableStrap, strap);
        }

        if (count > 0)
        {
            var grabbable = EnsureComp<PowerLoaderGrabbableComponent>(ent);
            grabbable.VirtualLeft = ent.Comp.PowerLoaderVirtualLeft;
            grabbable.VirtualRight = ent.Comp.PowerLoaderVirtualRight;
            Dirty(ent, grabbable);

            _metaData.SetEntityName(ent, Loc.GetString("rmc-chair-stack-name"));
            _metaData.SetEntityDescription(ent, Loc.GetString("rmc-chair-stack-description", ("count", count + 1)));
        }
        else
        {
            RemComp<PowerLoaderGrabbableComponent>(ent);
            var prototype = MetaData(ent).EntityPrototype;
            if (prototype != null)
            {
                _metaData.SetEntityName(ent, prototype.Name);
                _metaData.SetEntityDescription(ent, prototype.Description);
            }
        }
    }

    private void SetStackPhysics(Entity<RMCChairStackComponent> ent, bool enabled)
    {
        if (!TryComp(ent, out FixturesComponent? fixtures))
            return;

        if (enabled)
        {
            var shape = ent.Comp.FixtureShape;
            if (shape is PhysShapeAabb aabb)
                shape = (PolygonShape) aabb;

            _fixture.TryCreateFixture(ent,
                shape,
                ent.Comp.FixtureId,
                density: ent.Comp.FixtureDensity,
                hard: true,
                collisionLayer: (int) ent.Comp.CollisionLayer,
                collisionMask: (int) ent.Comp.CollisionMask,
                manager: fixtures);
            return;
        }

        if (fixtures.Fixtures.ContainsKey(ent.Comp.FixtureId))
            _fixture.DestroyFixture(ent, ent.Comp.FixtureId, manager: fixtures);
    }
}
