using System.Linq;
using Content.Shared._RMC14.Inventory;
using Content.Shared.Access.Components;
using Content.Shared.Containers;
using Content.Shared.Item;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Requisitions;

/// <summary>
/// Builds unit prices from homogeneous deliveries and explicit prices, with floors for fully priced mixed kits.
/// Unknown contents of mixed kits never receive an inferred price.
/// </summary>
public static class RequisitionsUnitCatalog
{
    private static readonly ProtoId<TagPrototype> AmmoBox = "RMCAmmoBox";

    public static string GetUnitName(
        EntProtoId product, string name, IPrototypeManager prototypes, IComponentFactory factory)
    {
        if (prototypes.TryIndex(product, out var prototype) &&
            prototype.TryGetComponent<StackComponent>(out var stack, factory) && stack.Count > 1)
            return Loc.GetString("rmc-requisitions-stack-unit", ("name", name), ("amount", stack.Count));
        return name;
    }

    public static Dictionary<EntProtoId, int> GetPrices(
        IEnumerable<RequisitionsCategory> categories,
        IPrototypeManager prototypes,
        IComponentFactory factory,
        out Dictionary<EntProtoId, LocId> productCategories,
        IReadOnlyDictionary<EntProtoId, int>? configuredPrices = null)
    {
        var prices = new Dictionary<EntProtoId, int>();
        productCategories = new Dictionary<EntProtoId, LocId>();
        var mixed = new List<(int Cost, Dictionary<EntProtoId, int> Contents)>();
        foreach (var category in categories)
        {
            foreach (var entry in category.Entries)
            {
                if (entry.Unit || entry.BlackMarket || entry.BlackMarketCost != 0 || entry.Cost <= 0)
                    continue;

                // Keep full ammunition boxes alongside their individual contents.
                for (var unpack = 0; unpack < 2; unpack++)
                {
                    var contents = new Dictionary<EntProtoId, int>();
                    if (!ReadPackage(entry.Crate, 1, contents, prototypes, factory, 0, true, unpack != 0))
                        continue;

                    foreach (var extra in entry.Entities)
                        Add(contents, extra, 1);

                    foreach (var containedProduct in contents.Keys)
                        productCategories.TryAdd(containedProduct, category.Name);

                    if (contents.Count != 1)
                    {
                        if (contents.Count > 1)
                            mixed.Add((entry.Cost, contents));
                        continue;
                    }

                    var (product, count) = contents.Single();
                    if (count <= 0 || !prototypes.Index(product).HasComponent<ItemComponent>(factory))
                        continue;

                    // Keep bulk discounts: buying a package as individual products must not cost less.
                    var price = (int) (((long) entry.Cost + count - 1) / count);
                    prices[product] = Math.Max(prices.GetValueOrDefault(product), price);
                }
            }
        }

        if (configuredPrices != null)
        {
            foreach (var (product, price) in configuredPrices)
            {
                if (price > 0 && prototypes.TryIndex(product, out var prototype) &&
                    prototype.HasComponent<ItemComponent>(factory))
                    prices[product] = price;
            }
        }

        // Evaluate every floor against the same base prices, so catalog ordering cannot amplify prices.
        var factors = new Dictionary<EntProtoId, (int Cost, long Value)>();
        foreach (var bundle in mixed)
        {
            long value = 0;
            var known = true;
            foreach (var (product, count) in bundle.Contents)
            {
                if (!prices.TryGetValue(product, out var price))
                {
                    known = false;
                    break;
                }
                value += (long) price * count;
            }
            if (!known || value <= 0 || value >= bundle.Cost)
                continue;

            foreach (var product in bundle.Contents.Keys)
            {
                if (!factors.TryGetValue(product, out var previous) ||
                    (long) bundle.Cost * previous.Value > (long) previous.Cost * value)
                    factors[product] = (bundle.Cost, value);
            }
        }
        foreach (var (product, factor) in factors)
            prices[product] = (int) (((long) prices[product] * factor.Cost + factor.Value - 1) / factor.Value);
        return prices;
    }

    private static bool ReadPackage(
        EntProtoId id, int amount, Dictionary<EntProtoId, int> contents,
        IPrototypeManager prototypes, IComponentFactory factory, int depth, bool outer, bool unpackBoxes)
    {
        if (depth > 8 || amount <= 0 || amount > 10000 || !prototypes.TryIndex(id, out var prototype))
            return false;

        if (unpackBoxes && prototype.TryGetComponent<TagComponent>(out var tags, factory) &&
            Enumerable.Contains(tags.Tags, AmmoBox) &&
            prototype.TryGetComponent<CMItemSlotsComponent>(out var slots, factory) &&
            slots.StartingItem is { } item && slots.Count is > 0 and <= 1000)
        {
            return ReadPackage(item, amount * slots.Count.Value, contents, prototypes, factory, depth + 1, false, true);
        }

        if (outer && prototype.Components.ContainsKey("EntityStorage"))
        {
            // Keep the original locked container and its access requirements intact.
            if (prototype.TryGetComponent<AccessReaderComponent>(out var access, factory) &&
                access.Enabled && access.AccessLists.Count > 0)
                return false;

            if (prototype.TryGetComponent<StorageFillComponent>(out var fill, factory))
            {
                foreach (var spawn in fill.Contents)
                {
                    if (spawn.PrototypeId is not { } child || spawn.SpawnProbability != 1 ||
                        spawn.GroupId != null || spawn.MaxAmount > spawn.Amount || spawn.Amount <= 0 ||
                        !ReadPackage(child, amount * spawn.Amount, contents, prototypes, factory, depth + 1,
                            false, unpackBoxes))
                        return false;
                }
                return contents.Count > 0;
            }

            if (prototype.TryGetComponent<ContainerFillComponent>(out var containers, factory))
            {
                foreach (var child in containers.Containers.Values.SelectMany(value => value))
                {
                    if (!ReadPackage(child, amount, contents, prototypes, factory, depth + 1, false, unpackBoxes))
                        return false;
                }
                return contents.Count > 0;
            }

            return false;
        }

        Add(contents, id, amount);
        return true;
    }

    private static void Add(Dictionary<EntProtoId, int> contents, EntProtoId id, int amount)
    {
        contents[id] = Math.Min(10000, contents.GetValueOrDefault(id) + amount);
    }
}
