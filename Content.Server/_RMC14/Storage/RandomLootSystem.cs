using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Humanoid.Systems;
using Content.Shared._RMC14.Crate;
using Content.Shared._RMC14.Projectiles;
using Content.Shared.Destructible;
using Content.Shared.EntityTable;
using Content.Shared.Explosion.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Storage;

public sealed class RandomLootSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoid = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RandomLootComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RandomLootComponent, CrateUnpackedEvent>(OnUnpacked);
        SubscribeLocalEvent<RandomLootComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnMapInit(Entity<RandomLootComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ReplaceSpawnOnTerminate)
            RemComp<SpawnOnTerminateComponent>(ent);
    }

    private void OnDestruction(Entity<RandomLootComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.GenerateOnDestruction)
            TryGenerate(ent);
    }

    private void OnUnpacked(Entity<RandomLootComponent> ent, ref CrateUnpackedEvent args)
    {
        if (ent.Comp.GenerateOnUnpack)
            TryGenerate(ent, args.User);
    }

    /// <summary>
    /// Generates one outcome once without removing the source entity.
    /// </summary>
    public bool TryGenerate(Entity<RandomLootComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.Generated || ent.Comp.Outcomes.Count == 0 || TerminatingOrDeleted(ent))
            return false;

        var weights = new Dictionary<RandomLootOutcome, float>();
        foreach (var outcome in ent.Comp.Outcomes)
        {
            if (outcome.Weight > 0)
                weights.Add(outcome, outcome.Weight);
        }

        if (weights.Count == 0)
            return false;

        ent.Comp.Generated = true;
        var selected = _random.Pick(weights);
        var coordinates = Transform(ent).Coordinates;
        if (selected.Corpse is { } corpse)
        {
            var name = selected.CorpseName is { } nameId ? Loc.GetString(nameId) : MetaData(ent).EntityName;
            var spawned = _randomHumanoid.SpawnRandomHumanoid(corpse, coordinates, name);
            var spawnedEvent = new SpawnedFromEntityEvent(spawned);
            RaiseLocalEvent(ent, ref spawnedEvent);
        }

        var trapped = false;
        foreach (var prototype in _entityTable.GetSpawns(selected.Contents))
        {
            var spawnCoordinates = coordinates;
            if (selected.Scatter > 0)
            {
                var offset = new Vector2(
                    _random.NextFloat(-selected.Scatter, selected.Scatter),
                    _random.NextFloat(-selected.Scatter, selected.Scatter));
                var candidate = coordinates.Offset(offset);
                if (_turf.TryGetTileRef(candidate, out var tile) && !tile.Value.Tile.IsEmpty &&
                    !_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
                    spawnCoordinates = candidate;
            }

            var spawned = SpawnAtPosition(prototype, spawnCoordinates);
            if (selected.StackAmount is { } amount &&
                TryComp<StackComponent>(spawned, out var stack) && stack.Count == 1)
                _stack.SetCount(spawned, _random.Next(amount.X, amount.Y + 1), stack);

            var spawnedEvent = new SpawnedFromEntityEvent(spawned);
            RaiseLocalEvent(ent, ref spawnedEvent);

            if (selected.PrimeChance <= 0 || !_random.Prob(selected.PrimeChance) ||
                !TryComp<OnUseTimerTriggerComponent>(spawned, out var timer))
                continue;

            _trigger.StartTimer((spawned, timer), user);
            trapped = true;
        }

        var popup = trapped && ent.Comp.PrimedPopup != null ? ent.Comp.PrimedPopup : selected.Popup;
        var popupType = trapped && ent.Comp.PrimedPopup != null ? ent.Comp.PrimedPopupType : selected.PopupType;
        if (popup is { } popupId)
            _popup.PopupCoordinates(Loc.GetString(popupId), coordinates, popupType);

        return true;
    }
}
