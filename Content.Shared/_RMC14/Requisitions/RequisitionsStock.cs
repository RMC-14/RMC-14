using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Requisitions;

/// <summary>
/// Vendor counters and the products that consume them. Packages share their counter with individual items.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequisitionsStock
{
    public List<int> Amounts = new();
    public List<RequisitionsStockOffer> Offers = new();
}

[Serializable, NetSerializable]
public readonly record struct RequisitionsStockOffer(EntProtoId Product, int Pool, int Units);

public readonly record struct RequisitionsStockAllocation(int Offer, int Amount);

/// <summary>
/// Reserves shared vendor stock without changing the vendor. Used by both the preview and approval.
/// </summary>
public sealed class RequisitionsStockPlan
{
    private readonly RequisitionsStock _stock;
    private readonly int[] _remaining;
    private readonly Dictionary<EntProtoId, List<int>> _offers = new();
    private readonly Dictionary<int, List<int>> _packageSizes = new();

    public RequisitionsStockPlan(RequisitionsStock stock)
    {
        _stock = stock;
        _remaining = stock.Amounts.ToArray();
        for (var i = 0; i < stock.Offers.Count; i++)
        {
            var offer = stock.Offers[i];
            var product = offer.Product;
            if (!_offers.TryGetValue(product, out var offers))
                _offers[product] = offers = new List<int>();
            offers.Add(i);
            if (offer.Units <= 1)
                continue;
            if (!_packageSizes.TryGetValue(offer.Pool, out var sizes))
                _packageSizes[offer.Pool] = sizes = new List<int>();
            if (!sizes.Contains(offer.Units))
                sizes.Add(offer.Units);
        }
    }

    public Dictionary<EntProtoId, int> GetAmounts()
    {
        var amounts = new Dictionary<EntProtoId, int>();
        foreach (var product in _offers.Keys)
            amounts[product] = GetAvailable(product);
        return amounts;
    }

    public int GetAvailable(EntProtoId product)
    {
        if (!_offers.TryGetValue(product, out var offers))
            return 0;

        long amount = 0;
        foreach (var index in offers)
        {
            var offer = _stock.Offers[index];
            var remaining = _remaining[offer.Pool];
            if (remaining == int.MaxValue)
                return int.MaxValue;
            amount += remaining / offer.Units;
        }
        return (int) Math.Min(int.MaxValue, amount);
    }

    public int Reserve(EntProtoId product, int amount, List<RequisitionsStockAllocation>? allocations = null)
    {
        if (amount <= 0 || !_offers.TryGetValue(product, out var offers))
            return 0;

        var reserved = 0;
        // Use loose stock first, preserving complete packages in other vendors.
        for (var pass = 0; pass < 2; pass++)
        {
            foreach (var index in offers)
            {
                var offer = _stock.Offers[index];
                var remaining = _remaining[offer.Pool];
                var available = remaining;
                if (remaining != int.MaxValue)
                {
                    if (pass == 0 && _packageSizes.TryGetValue(offer.Pool, out var sizes))
                    {
                        foreach (var size in sizes)
                        {
                            if (size > offer.Units)
                                available = Math.Min(available, remaining % size);
                        }
                    }
                    available /= offer.Units;
                }
                var take = Math.Min(amount - reserved, available);
                if (take <= 0)
                    continue;

                if (remaining != int.MaxValue)
                    _remaining[offer.Pool] -= take * offer.Units;
                allocations?.Add(new RequisitionsStockAllocation(index, take));
                reserved += take;
                if (reserved == amount)
                    return reserved;
            }
        }
        return reserved;
    }
}
