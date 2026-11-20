using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Requisitions;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Requisitions;

public sealed partial class RequisitionsBui
{
    private const int ProductPageSize = 20;

    private readonly CatalogIndex _supplyCatalog = new();
    private readonly CatalogIndex _blackMarketCatalog = new();
    private CatalogIndex? _renderedCatalog;
    private int _renderedFilter = -1;
    private int _renderedPage = -1;
    private bool _updatingPageSelector;
    private bool _refreshingShop;
    private bool _uiDisposed;

    private CatalogIndex CurrentCatalog => _blackMarketMode ? _blackMarketCatalog : _supplyCatalog;

    private void OnCatalogPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (_uiDisposed)
            return;
        _supplyCatalog.Invalid = true;
        _blackMarketCatalog.Invalid = true;
        if (_window != null)
            RefreshShop();
    }

    private void EnsureCatalogIndex()
    {
        if (_state == null)
            return;

        var index = CurrentCatalog;
        var categories = GetCurrentCategories(_state);
        var culture = _localization.DefaultCulture?.Name;
        var matches = !index.Invalid && index.Culture == culture && index.Categories.Count == categories.Count;
        var ordinal = 0;
        for (var category = 0; matches && category < categories.Count; category++)
        {
            matches = index.Categories[category] == categories[category].Name;
            for (var entry = 0; matches && entry < categories[category].Entries.Count; entry++)
            {
                matches = ordinal < index.Items.Count && index.Items[ordinal].Key == new CartKey(category, entry) &&
                          index.Items[ordinal].Matches(categories[category].Entries[entry]);
                ordinal++;
            }
        }
        if (matches && ordinal == index.Items.Count)
            return;

        var cart = GetCurrentCart();
        foreach (var key in cart.Keys.ToArray())
        {
            if (!TryGetEntry(key, out var entry) ||
                index.ByKey.TryGetValue(key, out var previous) &&
                (previous.Appearance.Crate != entry.Crate || previous.Appearance.Unit != entry.Unit))
                cart.Remove(key);
        }

        index.Categories.Clear();
        index.Items.Clear();
        index.ByKey.Clear();
        for (var category = 0; category < categories.Count; category++)
        {
            index.Categories.Add(categories[category].Name);
            for (var entry = 0; entry < categories[category].Entries.Count; entry++)
            {
                var product = categories[category].Entries[entry];
                var text = GetDisplayText(product);
                var item = new CatalogItem(new CartKey(category, entry), product, text.Name, text.Description);
                index.Items.Add(item);
                index.ByKey.Add(item.Key, item);
            }
        }
        index.Culture = culture;
        index.Invalid = false;
        index.Revision++;
        index.FilteredRevision = -1;
    }

    private void FilterCatalog()
    {
        var index = CurrentCatalog;
        var search = _window!.SearchBar.Text.Trim();
        var changed = index.Search != search || index.Category != SelectedCategory;
        if (!changed && index.FilteredRevision == index.Revision)
            return;

        if (changed)
            index.Page = 0;
        index.Search = search;
        index.Category = SelectedCategory;
        index.Filtered.Clear();
        foreach (var item in index.Items)
        {
            if ((SelectedCategory == null || item.Key.Category == SelectedCategory) &&
                MatchesSearch(item.Name, search))
                index.Filtered.Add(item);
        }
        index.Page = Math.Clamp(index.Page, 0, GetProductPageCount() - 1);
        index.FilteredRevision = index.Revision;
        index.FilterRevision++;
    }

    private int GetProductPageCount()
    {
        return Math.Max(1, (CurrentCatalog.Filtered.Count + ProductPageSize - 1) / ProductPageSize);
    }

    private void ChangeProductPage(int page)
    {
        if (_window == null || _updatingPageSelector)
            return;
        EnsureCatalogIndex();
        FilterCatalog();
        var index = CurrentCatalog;
        index.Page = Math.Clamp(page, 0, GetProductPageCount() - 1);
        PopulateProducts();
        _window.ProductsScroll.SetScrollValue(Vector2.Zero);
    }

    private void UpdateProductPages()
    {
        var index = CurrentCatalog;
        var pages = GetProductPageCount();
        _updatingPageSelector = true;
        if (_window!.ProductPageNumber.Value != index.Page + 1)
            _window.ProductPageNumber.OverrideValue(index.Page + 1);
        _updatingPageSelector = false;
        _window.ProductPageNumber.LineEditControl.Editable = pages > 1;
        _window.PreviousProductPage.Disabled = index.Page == 0;
        _window.NextProductPage.Disabled = index.Page + 1 >= pages;
        SetText(_window.ProductPageCount, Loc.GetString("rmc-requisitions-page-count",
            ("pages", pages), ("count", index.Filtered.Count)));
    }

    private (string Name, string Description) GetCatalogText(CartKey key, RequisitionsEntry entry)
    {
        return CurrentCatalog.ByKey.TryGetValue(key, out var item)
            ? (item.Name, item.Description)
            : GetDisplayText(entry);
    }

    private readonly record struct EntryAppearance(
        EntProtoId Crate, LocId? Name, LocId? Description, SpriteSpecifier? Icon, bool Unit, bool NoReturnReward)
    {
        public EntryAppearance(RequisitionsEntry entry)
            : this(entry.Crate, entry.Name, entry.Description, entry.Icon, entry.Unit, entry.NoReturnReward)
        {
        }
    }

    private sealed class CatalogItem(CartKey key, RequisitionsEntry entry, string name, string description)
    {
        public readonly CartKey Key = key;
        public readonly EntryAppearance Appearance = new(entry);
        private readonly EntProtoId[]? _contents = entry.NoReturnReward ? entry.Entities.ToArray() : null;
        public readonly string Name = name;
        public readonly string Description = description;

        public bool Matches(RequisitionsEntry product)
        {
            return Appearance == new EntryAppearance(product) &&
                   (_contents == null || _contents.SequenceEqual(product.Entities));
        }
    }

    private sealed class CatalogIndex
    {
        public readonly List<LocId> Categories = new();
        public readonly List<CatalogItem> Items = new();
        public readonly Dictionary<CartKey, CatalogItem> ByKey = new();
        public readonly List<CatalogItem> Filtered = new();
        public string? Culture;
        public string? Search;
        public int? Category;
        public bool Invalid = true;
        public int Revision;
        public int FilteredRevision = -1;
        public int FilterRevision;
        public int Page;

        public void Clear()
        {
            Categories.Clear();
            Categories.TrimExcess();
            Items.Clear();
            Items.TrimExcess();
            ByKey.Clear();
            ByKey.TrimExcess();
            Filtered.Clear();
            Filtered.TrimExcess();
            Culture = null;
            Search = null;
            Category = null;
            Invalid = true;
            Revision = 0;
            FilteredRevision = -1;
            FilterRevision = 0;
            Page = 0;
        }
    }
}
