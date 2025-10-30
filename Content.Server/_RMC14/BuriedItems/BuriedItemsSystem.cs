using System.Linq;
using Content.Shared._RMC14.BuriedItems;
using Content.Shared.Destructible;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server._RMC14.BuriedItems;

/// <summary>
/// Server-side system for handling buried items loot generation and dig completion.
/// </summary>
public sealed class BuriedItemsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BuriedItemsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BuriedItemsComponent, BuriedItemsDigDoAfterEvent>(OnDigDoAfter);
    }

    private void OnMapInit(Entity<BuriedItemsComponent> buried, ref MapInitEvent args)
    {
        // If there are no loot tables, nothing to do
        if (buried.Comp.LootTables == null || buried.Comp.LootTables.Count == 0)
            return;

        // Get the storage container
        if (!_container.TryGetContainer(buried.Owner, Content.Shared.Storage.StorageComponent.ContainerId, out var container))
            return;

        // For each loot table, pick one random item based on weights
        foreach (var table in buried.Comp.LootTables)
        {
            if (table.Entries.Count == 0)
                continue;

            // Build a dictionary of entries to weights for weighted random selection
            var weightedEntries = new Dictionary<BuriedItemsLootEntry, float>();
            foreach (var entry in table.Entries)
            {
                weightedEntries[entry] = entry.Weight;
            }

            // Pick a weighted random entry from this table
            var pickedEntry = _random.Pick(weightedEntries).Key;

            // Spawn the main item and insert it into storage
            var spawned = Spawn(pickedEntry.Id, Transform(buried).Coordinates);
            _storage.Insert(buried, spawned, out _, storageComp: null, playSound: false);

            // Spawn any accompanying items
            if (pickedEntry.Accompanying != null)
            {
                foreach (var accompanyingId in pickedEntry.Accompanying)
                {
                    var accompanyingItem = Spawn(accompanyingId, Transform(buried).Coordinates);
                    _storage.Insert(buried, accompanyingItem, out _, storageComp: null, playSound: false);
                }
            }
        }
    }

    private void OnDigDoAfter(Entity<BuriedItemsComponent> buried, ref BuriedItemsDigDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        _audio.PlayPvs(buried.Comp.RevealSound, buried);
        _popup.PopupEntity(Loc.GetString("rmc-buried-items-dig-success", ("user", args.User)), buried, PopupType.Medium);

        // Destroy the mound entity.
        // StorageComponent automatically empties its container when destroyed, spilling contents to the ground.
        _destructible.DestroyEntity(buried);
    }
}
