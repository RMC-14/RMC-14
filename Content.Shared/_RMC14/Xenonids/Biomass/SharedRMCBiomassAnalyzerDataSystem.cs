using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Biomass;

public abstract class SharedRMCBiomassAnalyzerDataSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly List<RMCBiomassUpgrade> _upgrades = new();

    public IReadOnlyList<RMCBiomassUpgrade> Upgrades => _upgrades;

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        ReloadUpgrades();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            ReloadUpgrades();
    }

    private void ReloadUpgrades()
    {
        _upgrades.Clear();
        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || !proto.TryGetComponent(out RMCBiomassUpgradeComponent? upgrade, _compFactory))
                continue;

            _upgrades.Add(new RMCBiomassUpgrade(proto, upgrade));
        }
    }

    public bool TryGetAnalyzer([NotNullWhen(true)] out Entity<RMCBiomassAnalyzerComponent>? analyzer)
    {
        var query = EntityQueryEnumerator<RMCBiomassAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            analyzer = (uid, comp);
            return true;
        }

        analyzer = default;
        return false;
    }

    public bool TryGetUpgrade(EntProtoId<RMCBiomassUpgradeComponent> id, [NotNullWhen(true)] out RMCBiomassUpgrade? upgrade)
    {
        upgrade = null;
        if (!_prototype.TryIndex<EntityPrototype>(id.Id, out var proto) ||
            !proto.TryGetComponent(out RMCBiomassUpgradeComponent? comp, _compFactory))
        {
            return false;
        }

        upgrade = new RMCBiomassUpgrade(proto, comp);
        return true;
    }

    public int GetCurrentCost(RMCBiomassUpgrade upgrade, RMCBiomassAnalyzerComponent? analyzer)
    {
        var comp = upgrade.Comp;
        var count = analyzer?.PurchaseCounts.GetValueOrDefault(upgrade.Id) ?? 0;
        return Math.Clamp(comp.Cost + comp.PriceChange * count, comp.MinimumPrice, comp.MaximumPrice);
    }
}

public sealed record RMCBiomassUpgrade(EntityPrototype Prototype, RMCBiomassUpgradeComponent Comp)
{
    public EntProtoId<RMCBiomassUpgradeComponent> Id => Prototype.ID;
}
