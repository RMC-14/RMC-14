using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Fishing;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Water;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Fishing;

public abstract class SharedRMCFishingSystem : EntitySystem
{
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCWaterSystem _water = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly SharedTransformSystem _transform = default!;
    [Dependency] protected readonly ThrowingSystem _throwing = default!;
    [Dependency] protected readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCFishingSpearComponent, RMCFishingSpearDoAfterEvent>(OnSpearDoAfter);
    }

    private void OnSpearDoAfter(Entity<RMCFishingSpearComponent> ent, ref RMCFishingSpearDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.Busy = false;
        Dirty(ent);

        if (args.Cancelled)
            return;

        var coordinates = GetCoordinates(args.Coordinates);

        if (!IsFishableWater(coordinates, args.User))
        {
            _popup.PopupClient(Loc.GetString("rmc-fishing-invalid-water"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (TrySpearFish(args.User, coordinates, ent.Comp.FailChance, ent.Comp.Loot, ent.Comp.CommonWeight, ent.Comp.UncommonWeight, ent.Comp.RareWeight, ent.Comp.UltraRareWeight, true, out var caught) && caught != null)
            _throwing.TryThrow(caught.Value, _transform.GetMoverCoordinates(args.User), 2f, args.User, compensateFriction: true);

    }

    public bool DoXenoFish(Entity<XenoFishingComponent> xeno, EntityCoordinates coords, out EntityUid? caught, bool doThrow = true, bool pickup = true)
    {
        caught = null;
        if (!IsFishableWater(coords, xeno))
            return false;

        var fishAttempt = TrySpearFish(xeno, coords, xeno.Comp.FailChance, xeno.Comp.Loot, xeno.Comp.CommonWeight, xeno.Comp.UncommonWeight, xeno.Comp.RareWeight, xeno.Comp.UltraRareWeight, pickup, out caught);

        if (caught != null && doThrow)
            _throwing.TryThrow(caught.Value, _transform.GetMoverCoordinates(xeno), 2f, xeno, compensateFriction: true);

        return fishAttempt;
    }

    private bool TrySpearFish(EntityUid user, EntityCoordinates coords, float failChance, ProtoId<RMCFishingLootPrototype> lootpool, int common, int uncommon, int rare, int ultra, bool pickup, out EntityUid? caught)
    {
        caught = null;

        var xeno = HasComp<XenoComponent>(user);

        if (_random.Prob(failChance) ||
            !TryPickLoot(coords, lootpool, common, uncommon, rare, ultra, null, out var loot))
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString(xeno ? "rmc-fishing-spear-fail-xeno" : "rmc-fishing-spear-fail"), user, user, PopupType.SmallCaution);
            return false;
        }

        if (_net.IsClient)
            return true;

        caught = Spawn(loot, coords);

        if (pickup && _hands.TryPickupAnyHand(user, caught.Value))
        {
            _popup.PopupEntity(Loc.GetString(xeno ? "rmc-fishing-spear-success-hand-xeno" : "rmc-fishing-spear-success-hand", ("item", caught)), user, user);
            return true;
        }

        _popup.PopupEntity(Loc.GetString(xeno ? "rmc-fishing-spear-success-water-xeno" : "rmc-fishing-spear-success-water", ("item", caught)), user, user);

        return true;
    }

    private bool TryPick(IReadOnlyList<EntProtoId> entries, out EntProtoId picked)
    {
        picked = default;
        if (entries.Count == 0)
            return false;

        picked = entries[_random.Next(entries.Count)];
        return true;
    }

    private static float ClampChance(int chance)
    {
        return Math.Clamp(chance, 0, 100) / 100f;
    }

    protected TimeSpan RandomTime(TimeSpan min, TimeSpan max)
    {
        if (max <= min)
            return min;

        return TimeSpan.FromSeconds(_random.NextDouble(min.TotalSeconds, max.TotalSeconds));
    }

    protected bool TryPickLoot(
        EntityCoordinates coordinates,
        ProtoId<RMCFishingLootPrototype> fallback,
        int commonWeight,
        int uncommonWeight,
        int rareWeight,
        int ultraRareWeight,
        RMCFishBaitComponent? bait,
        out EntProtoId loot)
    {
        loot = default;
        var tableId = fallback;
        if (_area.TryGetArea(coordinates, out var areaEnt, out _) &&
            areaEnt.Value.Comp.FishingLoot is { } areaLoot)
        {
            tableId = areaLoot;
        }

        if (!_prototype.TryIndex(tableId, out var table))
            return false;

        var common = ClampChance(commonWeight + (bait?.CommonModifier ?? 0));
        var uncommon = ClampChance(uncommonWeight + (bait?.UncommonModifier ?? 0));
        var rare = ClampChance(rareWeight + (bait?.RareModifier ?? 0));
        var ultraRare = ClampChance(ultraRareWeight + (bait?.UltraRareModifier ?? 0));

        // CMSS13 used sequential prob() checks, so these chances intentionally do not normalize.
        if (_random.Prob(common) && TryPick(table.Common, out loot))
            return true;
        if (_random.Prob(uncommon) && TryPick(table.Uncommon, out loot))
            return true;
        if (_random.Prob(rare) && TryPick(table.Rare, out loot))
            return true;
        if (_random.Prob(ultraRare) && TryPick(table.UltraRare, out loot))
            return true;

        return TryPick(table.Common, out loot);
    }


    public bool IsFishableWater(EntityCoordinates coordinates, EntityUid user)
    {
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(coordinates);
        while (anchored.MoveNext(out var uid))
        {
            if (!TryComp(uid, out RMCWaterComponent? water))
                continue;

            return _water.CanCollide((uid, water), user);
        }

        return false;
    }
}
