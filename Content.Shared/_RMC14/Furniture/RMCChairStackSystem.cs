using Content.Shared._RMC14.Xenonids;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Foldable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Furniture;

public sealed class RMCChairStackSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly FoldableSystem _foldable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const string ContainerId = "rmc_chair_stack";
    private const float SpeedFast = 6.67f;
    private static readonly ProtoId<ToolQualityPrototype> WrenchQuality = "Anchoring";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCChairStackableComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<RMCChairStackableComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(AnchorableSystem)]);
        SubscribeLocalEvent<RMCChairStackableComponent, InteractHandEvent>(OnInteractHand, before: [typeof(SharedBuckleSystem)]);
        SubscribeLocalEvent<RMCChairStackableComponent, AfterInteractEvent>(OnAfterInteract, after: [typeof(DeployFoldableSystem)]);
        SubscribeLocalEvent<RMCChairStackableComponent, FoldAttemptEvent>(OnFoldAttempt);

        SubscribeLocalEvent<RMCChairStackableComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<RMCChairStackableComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<RMCChairStackableComponent, ThrowDoHitEvent>(OnThrowDoHit);
        SubscribeLocalEvent<RMCChairStackableComponent, ThrowHitByEvent>(OnThrowHitBy);
    }

    private void OnMapInit(Entity<RMCChairStackableComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ContainerId);
    }

    private void OnInteractUsing(Entity<RMCChairStackableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;
        if (_tool.HasQuality(used, WrenchQuality) && ent.Comp.CurrentStackSize > 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-chair-stack-wrench-blocked"), ent, args.User);
            args.Handled = true;
            return;
        }

        // The chair being USED FOR stacking must be folded.
        if (!TryComp<FoldableComponent>(used, out var foldable) || !foldable.IsFolded)
            return;

        if (!HasComp<RMCChairStackableComponent>(used))
            return;

        if (TryComp<WieldableComponent>(used, out var wieldable) && wieldable.Wielded)
            return;

        // You can't stack ONTO a folded chair. It needs to be an unfolded chair.
        if (TryComp<FoldableComponent>(ent, out var entFoldable) && entFoldable.IsFolded)
            return;

        if (TryComp<StrapComponent>(ent, out var strap) && strap.BuckledEntities.Count > 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-chair-stack-blocked"), ent, args.User);
            args.Handled = true;
            return;
        }

        if (_net.IsClient)
            return;

        var container = _container.EnsureContainer<Container>(ent, ContainerId);

        if (!_hands.TryDrop(args.User, used))
            return;

        if (!_container.Insert(used, container))
        {
            _hands.TryPickupAnyHand(args.User, used);
            return;
        }

        ent.Comp.CurrentStackSize++;
        Dirty(ent);
        UpdateStackState(ent);
        args.Handled = true;

        if (ent.Comp.CurrentStackSize > ent.Comp.MaxStableStack)
        {
            _popup.PopupClient(Loc.GetString("rmc-chair-stack-unstable"), ent, args.User);

            var collapseChance = (float) Math.Sqrt(50 * ent.Comp.CurrentStackSize) / 100;
            if (_random.Prob(collapseChance))
                StackCollapse(ent);
        }
    }

    private void OnInteractHand(Entity<RMCChairStackableComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.CurrentStackSize <= 0)
            return;

        if (_net.IsClient)
            return;

        var container = _container.EnsureContainer<Container>(ent, ContainerId);
        if (container.ContainedEntities.Count == 0)
            return;

        var last = container.ContainedEntities[^1];
        if (!_container.Remove(last, container))
            return;

        _hands.TryPickupAnyHand(args.User, last);

        ent.Comp.CurrentStackSize--;
        Dirty(ent);
        UpdateStackState(ent);

        args.Handled = true;
    }

    private void OnAfterInteract(Entity<RMCChairStackableComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.Handled || !HasComp<DeployFoldableComponent>(ent))
            return;

        var userDir = Transform(args.User).LocalRotation.GetCardinalDir();
        _transform.SetLocalRotation(ent, userDir.ToAngle());
    }

    private static void OnFoldAttempt(Entity<RMCChairStackableComponent> ent, ref FoldAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.CurrentStackSize > 0)
            args.Cancelled = true;
    }

    private void OnDestruction(Entity<RMCChairStackableComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.CurrentStackSize > 0)
            StackCollapse(ent);
    }

    private void OnDamageChanged(Entity<RMCChairStackableComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (ent.Comp.CurrentStackSize > 0)
            StackCollapse(ent);
    }

    private void OnThrowDoHit(Entity<RMCChairStackableComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        if (_net.IsServer)
            _audio.PlayPvs(ent.Comp.ThrownHitSound, ent);
    }

    private void OnThrowHitBy(Entity<RMCChairStackableComponent> ent, ref ThrowHitByEvent args)
    {
        if (ent.Comp.CurrentStackSize <= 0)
            return;

        if (HasComp<MobStateComponent>(args.Thrown))
        {
            StackCollapse(ent);

            if (!HasComp<XenoComponent>(args.Thrown))
            {
                _stun.TryStun(args.Thrown, ent.Comp.ThrownMobStatusDuration, true);
                _stun.TryKnockdown(args.Thrown, ent.Comp.ThrownMobStatusDuration, true);
            }

            return;
        }

        if (ent.Comp.CurrentStackSize > ent.Comp.MaxStableStack && _random.Prob(0.5f))
            StackCollapse(ent);
    }

    private void UpdateStackState(Entity<RMCChairStackableComponent> ent)
    {
        var stackFixture = _fixture.GetFixtureOrNull(ent, ent.Comp.StackFixtureId);

        if (ent.Comp.CurrentStackSize > 0)
        {
            var total = ent.Comp.CurrentStackSize + 1;
            _metaData.SetEntityName(ent, Loc.GetString("rmc-chair-stack-name"));
            _metaData.SetEntityDescription(ent, Loc.GetString("rmc-chair-stack-description", ("count", total)));
            _buckle.StrapSetEnabled(ent, false);

            if (stackFixture == null)
            {
                _fixture.TryCreateFixture(
                    ent,
                    new PhysShapeCircle(ent.Comp.StackFixtureRadius),
                    ent.Comp.StackFixtureId,
                    hard: true,
                    collisionLayer: (int) CollisionGroup.MidImpassable);

                stackFixture = _fixture.GetFixtureOrNull(ent, ent.Comp.StackFixtureId);
            }

            if (stackFixture != null)
            {
                _physics.SetHard(ent, stackFixture, true);
                _physics.AddCollisionLayer(ent, ent.Comp.StackFixtureId, stackFixture, (int) CollisionGroup.MidImpassable);
            }
        }
        else
        {
            var meta = MetaData(ent.Owner);
            if (meta.EntityPrototype != null)
            {
                _metaData.SetEntityName(ent, meta.EntityPrototype.Name);
                _metaData.SetEntityDescription(ent, meta.EntityPrototype.Description);
            }

            _buckle.StrapSetEnabled(ent, true);

            if (stackFixture != null)
                _fixture.DestroyFixture(ent, ent.Comp.StackFixtureId, stackFixture);
        }

        _appearance.SetData(ent.Owner, RMCChairStackVisuals.StackSize, ent.Comp.CurrentStackSize);
    }

    private void StackCollapse(Entity<RMCChairStackableComponent> ent)
    {
        if (_net.IsClient)
            return;

        _popup.PopupEntity(Loc.GetString("rmc-chair-stack-collapse"), ent);
        _audio.PlayPvs(ent.Comp.CollapseSound, ent);

        var container = _container.EnsureContainer<Container>(ent, ContainerId);
        var coords = Transform(ent).Coordinates;

        // Dump and throw the stacked chairs
        var contained = new List<EntityUid>(container.ContainedEntities);
        var remainingStack = contained.Count;
        foreach (var child in contained)
        {
            remainingStack--;
            _container.Remove(child, container);
            _transform.SetCoordinates(child, coords);

            var scatterRadius = MathF.Floor(remainingStack / 2f);
            var throwRange = _random.NextFloat(2f, 5f);
            var effectiveDistance = MathF.Max(1f, MathF.Min(scatterRadius, throwRange));
            var direction = _random.NextAngle().ToVec() * effectiveDistance;
            _throwing.TryThrow(child, direction, SpeedFast);
        }

        ent.Comp.CurrentStackSize = 0;
        Dirty(ent);
        UpdateStackState(ent);

        if (TryComp<FoldableComponent>(ent, out var foldable) &&
            _foldable.TrySetFolded(ent, foldable, true))
        {
            var lastChairRange = _random.NextFloat(2f, 5f);
            var lastChairDirection = _random.NextAngle().ToVec() * lastChairRange;
            _throwing.TryThrow(ent, lastChairDirection, SpeedFast);
        }
    }
}
