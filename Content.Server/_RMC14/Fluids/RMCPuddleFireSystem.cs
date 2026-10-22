using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Fluids;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.OnCollide;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._RMC14.Fluids;

public sealed class RMCPuddleFireSystem : EntitySystem
{
    private static readonly ProtoId<ReactiveGroupPrototype> FlammableGroup = "Flammable";
    private static readonly TimeSpan SpreadDelay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ExistingFireCheckInterval = TimeSpan.FromSeconds(1);

    private const float MinSpreadAmount = 7.5f;
    private const int MinIntensity = 3;
    private const int MaxIntensity = 20;
    private const int MinDuration = 3;
    private const int MaxDuration = 24;

    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedOnCollideSystem _onCollide = default!;
    [Dependency] private readonly SharedRMCFlamerSystem _flamer = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _nextExistingFireCheck;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TileFireComponent, ComponentStartup>(OnTileFireStartup);
        SubscribeLocalEvent<IgnitionSourceComponent, AfterInteractEvent>(OnIgnitionSourceAfterInteract);
        SubscribeLocalEvent<AlwaysHotComponent, AfterInteractEvent>(OnAlwaysHotAfterInteract);
        SubscribeLocalEvent<IgnitionSourceComponent, ItemToggledEvent>(OnIgnitionSourceToggled);

        SubscribeLocalEvent<PuddleComponent, ComponentStartup>(OnPuddleStartup);
        SubscribeLocalEvent<SmokeComponent, ComponentStartup>(OnSmokeStartup);
        SubscribeLocalEvent<RMCPuddleComponent, SolutionContainerChangedEvent>(OnMonitoredSolutionChanged);
    }

    private void OnPuddleStartup(Entity<PuddleComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<RMCPuddleComponent>(ent.Owner);

        if (_solutionContainer.TryGetSolution((ent.Owner, null), ent.Comp.SolutionName, out _, out var solution))
            UpdateFlammableMarker(ent.Owner, solution);
    }

    private void OnSmokeStartup(Entity<SmokeComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<RMCPuddleComponent>(ent.Owner);

        if (_solutionContainer.TryGetSolution((ent.Owner, null), SmokeComponent.SolutionName, out _, out var solution))
            UpdateFlammableMarker(ent.Owner, solution);
    }

    private void OnMonitoredSolutionChanged(Entity<RMCPuddleComponent> ent, ref SolutionContainerChangedEvent args)
    {
        var isPuddleSolution = TryComp<PuddleComponent>(ent.Owner, out var puddle) && args.SolutionId == puddle.SolutionName;
        var isSmokeSolution = args.SolutionId == SmokeComponent.SolutionName && HasComp<SmokeComponent>(ent.Owner);
        if (!isPuddleSolution && !isSmokeSolution)
            return;

        UpdateFlammableMarker(ent.Owner, args.Solution);
    }

    private void UpdateFlammableMarker(EntityUid uid, Solution solution)
    {
        if (IsFlammable(solution))
            EnsureComp<RMCFlammableAreaEffectComponent>(uid);
        else
            RemCompDeferred<RMCFlammableAreaEffectComponent>(uid);
    }

    private void OnIgnitionSourceAfterInteract(Entity<IgnitionSourceComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        var isHotEvent = new IsHotEvent();
        RaiseLocalEvent(ent.Owner, isHotEvent);
        if (!isHotEvent.IsHot)
            return;

        TryIgniteAt(ref args);
    }

    private void OnAlwaysHotAfterInteract(Entity<AlwaysHotComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        TryIgniteAt(ref args);
    }

    private void OnIgnitionSourceToggled(Entity<IgnitionSourceComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated)
            return;

        TryIgniteAtCoords(Transform(ent.Owner).Coordinates);
    }

    private void TryIgniteAt(ref AfterInteractEvent args)
    {
        if (TryIgniteAtCoords(args.ClickLocation))
            args.Handled = true;
    }

    private bool TryIgniteAtCoords(EntityCoordinates coords)
    {
        if (!TryGetFlammableAreaEffect(coords, null, out _, out _, out var solution, out var isSmoke))
            return false;

        IgniteSolution(coords, solution, isSmoke);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        if (time < _nextExistingFireCheck)
            return;

        _nextExistingFireCheck = time + ExistingFireCheckInterval;
        CheckAreaEffectsNextToExistingFire();
    }

    private void CheckAreaEffectsNextToExistingFire()
    {
        var anyFire = Count<TileFireComponent>() > 0;

        var puddles = EntityQueryEnumerator<RMCFlammableAreaEffectComponent, PuddleComponent>();
        while (puddles.MoveNext(out var uid, out _, out var puddle))
        {
            if (!_solutionContainer.TryGetSolution((uid, null), puddle.SolutionName, out _, out var solution))
                continue;

            var coords = Transform(uid).Coordinates;
            if (anyFire && IsNextToFire(coords) || IsHotSourcePresent(coords))
                IgniteSolution(coords, solution, isSmoke: false);
        }

        var smokes = EntityQueryEnumerator<RMCFlammableAreaEffectComponent, SmokeComponent>();
        while (smokes.MoveNext(out var uid, out _, out _))
        {
            if (!_solutionContainer.TryGetSolution((uid, null), SmokeComponent.SolutionName, out _, out var solution))
                continue;

            var coords = Transform(uid).Coordinates;
            if (anyFire && IsNextToFire(coords) || IsHotSourcePresent(coords))
                IgniteSolution(coords, solution, isSmoke: true);
        }
    }

    private bool IsHotSourcePresent(EntityCoordinates coords)
    {
        if (!_rmcMap.TryGetTileRefForEnt(coords, out var grid, out var tile))
            return false;

        foreach (var uid in _lookup.GetLocalEntitiesIntersecting(grid.Owner, tile.GridIndices))
        {
            if (IsHot(uid))
                return true;

            foreach (var item in _inventory.GetHandOrInventoryEntities(uid, SlotFlags.WITHOUT_POCKET))
            {
                if (IsHot(item))
                    return true;
            }
        }

        return false;
    }

    private bool IsHot(EntityUid uid)
    {
        var isHotEvent = new IsHotEvent();
        RaiseLocalEvent(uid, isHotEvent);
        return isHotEvent.IsHot;
    }

    private bool IsNextToFire(EntityCoordinates coords)
    {
        if (_rmcMap.HasAnchoredEntityEnumerator<TileFireComponent>(coords))
            return true;

        foreach (var direction in _rmcMap.CardinalDirections)
        {
            if (_rmcMap.HasAnchoredEntityEnumerator<TileFireComponent>(coords, offset: direction))
                return true;
        }

        return false;
    }

    private void OnTileFireStartup(Entity<TileFireComponent> fire, ref ComponentStartup args)
    {
        var coords = Transform(fire).Coordinates;
        BurnAreaEffectAt(coords);
        SpreadToNeighbours(coords);
    }

    private bool TryGetFlammableAreaEffect(
        EntityCoordinates coords,
        Direction? offset,
        out EntityUid entity,
        out Entity<SolutionComponent> soln,
        out Solution solution,
        out bool isSmoke)
    {
        entity = default;
        soln = default;
        solution = default!;
        isSmoke = false;

        if (_rmcMap.HasAnchoredEntityEnumerator<PuddleComponent>(coords, out Entity<PuddleComponent> puddle, offset: offset) &&
            _solutionContainer.TryGetSolution((puddle.Owner, null), puddle.Comp.SolutionName, out var puddleSoln, out var puddleSolution) &&
            IsFlammable(puddleSolution))
        {
            entity = puddle.Owner;
            soln = puddleSoln.Value;
            solution = puddleSolution;
            return true;
        }

        if (_rmcMap.HasAnchoredEntityEnumerator<SmokeComponent>(coords, out Entity<SmokeComponent> smoke, offset: offset) &&
            _solutionContainer.TryGetSolution((smoke.Owner, null), SmokeComponent.SolutionName, out var smokeSoln, out var smokeSolution) &&
            IsFlammable(smokeSolution))
        {
            entity = smoke.Owner;
            soln = smokeSoln.Value;
            solution = smokeSolution;
            isSmoke = true;
            return true;
        }

        return false;
    }

    private bool IsFlammable(Solution solution)
    {
        foreach (var reagent in solution.Contents)
        {
            if (TryGetFlammableReagent(reagent, out _))
                return true;
        }

        return false;
    }

    private bool TryGetFlammableReagent(ReagentQuantity reagent, [NotNullWhen(true)] out Reagent? proto)
    {
        proto = null;
        if (reagent.Quantity <= FixedPoint2.Zero)
            return false;

        if (!_rmcReagent.TryIndex(reagent.Reagent.Prototype, out var indexed) ||
            (indexed.ReactiveEffects?.ContainsKey(FlammableGroup) != true &&
             !_rmcReagent.HasProperty<Fueling>(reagent.Reagent.Prototype)))
        {
            return false;
        }

        proto = indexed;
        return true;
    }

    private void BurnAreaEffectAt(EntityCoordinates coords)
    {
        if (!TryGetFlammableAreaEffect(coords, null, out _, out var soln, out var solution, out var isSmoke) || isSmoke)
            return;

        foreach (var reagent in solution.Contents.ToArray())
        {
            if (!TryGetFlammableReagent(reagent, out _))
                continue;

            _solutionContainer.RemoveReagent(soln, reagent);
        }
    }

    private void SpreadToNeighbours(EntityCoordinates coords)
    {
        foreach (var direction in _rmcMap.CardinalDirections)
        {
            if (!TryGetFlammableAreaEffect(coords, direction, out var neighbour, out _, out _, out _))
                continue;

            var target = Transform(neighbour).Coordinates;
            Timer.Spawn(SpreadDelay, () =>
            {
                if (TerminatingOrDeleted(neighbour))
                    return;

                Solution? solution = null;
                var isSmoke = false;
                if (TryComp<PuddleComponent>(neighbour, out var puddleComp))
                {
                    _solutionContainer.TryGetSolution((neighbour, null), puddleComp.SolutionName, out _, out solution);
                }
                else if (HasComp<SmokeComponent>(neighbour))
                {
                    _solutionContainer.TryGetSolution((neighbour, null), SmokeComponent.SolutionName, out _, out solution);
                    isSmoke = true;
                }

                if (solution is null || !IsFlammable(solution))
                    return;

                IgniteSolution(target, solution, isSmoke);
            });
        }
    }

    private bool AlreadyOnFire(EntityCoordinates coords)
    {
        return _rmcMap.HasAnchoredEntityEnumerator<TileFireComponent>(coords);
    }

    private void IgniteSolution(EntityCoordinates coords, Solution solution, bool isSmoke)
    {
        if (AlreadyOnFire(coords))
            return;

        var flammableVolume = FixedPoint2.Zero;
        Reagent? dominant = null;
        var dominantQuantity = FixedPoint2.Zero;
        foreach (var reagent in solution.Contents)
        {
            if (!TryGetFlammableReagent(reagent, out var proto))
                continue;

            flammableVolume += reagent.Quantity;
            if (reagent.Quantity > dominantQuantity)
            {
                dominant = proto;
                dominantQuantity = reagent.Quantity;
            }
        }

        if (flammableVolume <= FixedPoint2.Zero || dominant is not { } fuel)
            return;

        var stats = _flamer.GetFireStats(fuel);

        int intensity;
        int duration;
        if (isSmoke)
        {
            intensity = Math.Clamp(stats.Intensity, MinIntensity, MaxIntensity);
            duration = Math.Clamp(stats.Duration, MinDuration, MaxDuration);
        }
        else
        {
            var pressure = Math.Clamp((flammableVolume.Float() / MinSpreadAmount - 1f) * 10f, -10f, 0f);
            intensity = Math.Clamp(stats.Intensity + (int) MathF.Round(pressure * (float) stats.IntensityMod), MinIntensity, MaxIntensity);
            duration = Math.Clamp(stats.Duration + (int) MathF.Round(pressure * (float) stats.DurationMod), MinDuration, MaxDuration);
        }

        var fireColor = _flammable.GetFireColor(fuel);

        var chain = _onCollide.SpawnChain();
        _flammable.SpawnFire(coords, fuel.FireEntity, chain, 0, intensity, duration, out _, stats.FirePenetrating, fireColor);
        _onCollide.CleanupChain(chain);
    }
}
