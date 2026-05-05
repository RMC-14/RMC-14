using System.Numerics;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Content.Shared._RMC14.Requisitions.Components.RequisitionsElevatorMode;

namespace Content.Client._RMC14.Requisitions;

[UsedImplicitly]
public sealed class RequisitionsBui : BoundUserInterface
{
    private const float CategoryMinWidth = 180f;
    private const float CategoryPanelPadding = 12f;

    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly SpriteSystem _sprite;
    private readonly Dictionary<CartKey, int> _cart = new();
    private readonly Dictionary<CartKey, RequisitionsProductCard> _productCards = new();

    private RequisitionsBuiState? _state;
    private RequisitionsWindow? _window;
    private int? _selectedCategory;

    public RequisitionsBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();
        EnsureWindow();
        RefreshShop();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not RequisitionsBuiState uiState)
            return;

        _state = uiState;
        EnsureWindow();
        RefreshShop();

        if (_window is { IsOpen: false })
            _window.OpenCentered();
    }

    private void EnsureWindow()
    {
        if (_window != null)
            return;

        _window = this.CreateWindow<RequisitionsWindow>();

        _window.SearchBar.OnTextChanged += _ => RefreshVisibleEntries();
        _window.PlatformButton.OnPressed += _ => PressPlatformButton();
        _window.ClearCartButton.OnPressed += _ =>
        {
            _cart.Clear();
            RefreshVisibleEntries();
        };
        _window.BuyButton.OnPressed += _ => BuyCart();
    }

    private void RefreshShop()
    {
        if (_entities.TryGetComponent(Owner, out RequisitionsComputerComponent? computer) &&
            _selectedCategory >= computer.Categories.Count)
        {
            _selectedCategory = null;
        }

        UpdatePlatformButtons();
        UpdateBalance();
        PopulateCategories();
        PopulateProducts();
        RefreshCart();
        PopulatePendingOrders();
    }

    private void RefreshVisibleEntries()
    {
        PopulateProducts();
        RefreshCart();
        PopulatePendingOrders();
    }

    private void UpdatePlatformButtons()
    {
        if (_window == null)
            return;

        _window.PlatformButton.Disabled = true;

        if (_state == null)
        {
            _window.PlatformButton.Text = Loc.GetString("rmc-requisitions-platform-missing");
            return;
        }

        if (_state.Busy || _state.PlatformLowered is Preparing or Lowering or Raising)
        {
            _window.PlatformButton.Text = Loc.GetString("rmc-requisitions-platform-busy");
            return;
        }

        switch (_state.PlatformLowered)
        {
            case Lowered:
                _window.PlatformButton.Text = Loc.GetString("rmc-requisitions-platform-raise");
                _window.PlatformButton.Disabled = false;
                break;
            case Raised:
                _window.PlatformButton.Text = Loc.GetString("rmc-requisitions-platform-lower");
                _window.PlatformButton.Disabled = false;
                break;
            default:
                _window.PlatformButton.Text = Loc.GetString("rmc-requisitions-platform-missing");
                break;
        }
    }

    private void PressPlatformButton()
    {
        if (_state == null || _state.Busy)
            return;

        switch (_state.PlatformLowered)
        {
            case Lowered:
                SendMessage(new RequisitionsPlatformMsg(true));
                break;
            case Raised:
                SendMessage(new RequisitionsPlatformMsg(false));
                break;
        }
    }

    private void UpdateBalance()
    {
        if (_window == null || _state == null)
            return;

        _window.BudgetLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString(
            "rmc-requisitions-balance",
            ("balance", _state.Balance))));

        _window.CapacityLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString(
            "rmc-requisitions-capacity",
            ("count", _state.OrderCount),
            ("capacity", _state.Capacity))));
    }

    private void PopulateCategories()
    {
        if (_window == null)
            return;

        _window.CategoriesContainer.DisposeAllChildren();

        _entities.TryGetComponent(Owner, out RequisitionsComputerComponent? computer);

        var buttons = new List<Button>();
        var allButton = CreateCategoryButton(Loc.GetString("rmc-requisitions-category-all"), _selectedCategory == null);
        allButton.OnPressed += _ =>
        {
            _selectedCategory = null;
            RefreshShop();
        };
        buttons.Add(allButton);

        if (computer != null)
        {
            for (var i = 0; i < computer.Categories.Count; i++)
            {
                var categoryIndex = i;
                var button = CreateCategoryButton(computer.Categories[i].Name, _selectedCategory == categoryIndex);
                button.OnPressed += _ =>
                {
                    _selectedCategory = categoryIndex;
                    RefreshShop();
                };
                buttons.Add(button);
            }
        }

        var buttonWidth = CategoryMinWidth;
        foreach (var button in buttons)
        {
            button.Measure(new Vector2(float.PositiveInfinity, float.PositiveInfinity));
            buttonWidth = Math.Max(buttonWidth, MathF.Ceiling(button.DesiredSize.X));
        }

        _window.CategoryPanel.MinWidth = buttonWidth + CategoryPanelPadding;
        foreach (var button in buttons)
        {
            button.MinWidth = buttonWidth;
            _window.CategoriesContainer.AddChild(button);
        }
    }

    private static Button CreateCategoryButton(string text, bool disabled)
    {
        return new Button
        {
            Text = text,
            Disabled = disabled,
            ClipText = false,
            HorizontalExpand = true,
            StyleClasses = { "ButtonSquare" },
        };
    }

    private void PopulateProducts()
    {
        if (_window == null)
            return;

        _window.ProductsContainer.DisposeAllChildren();
        _productCards.Clear();

        if (!_entities.TryGetComponent(Owner, out RequisitionsComputerComponent? computer))
            return;

        var search = _window.SearchBar.Text.Trim();
        var added = 0;

        for (var categoryIndex = 0; categoryIndex < computer.Categories.Count; categoryIndex++)
        {
            if (_selectedCategory != null && _selectedCategory != categoryIndex)
                continue;

            var category = computer.Categories[categoryIndex];
            for (var entryIndex = 0; entryIndex < category.Entries.Count; entryIndex++)
            {
                var entry = category.Entries[entryIndex];
                var display = GetDisplay(entry);
                if (!MatchesSearch(display, search))
                    continue;

                var key = new CartKey(categoryIndex, entryIndex);
                var card = new RequisitionsProductCard
                {
                    CategoryIndex = categoryIndex,
                    EntryIndex = entryIndex,
                    UnitCost = entry.Cost,
                    Cost = { Text = Loc.GetString("rmc-requisitions-card-cost", ("cost", entry.Cost)) },
                    Icon = { Texture = display.Icon },
                };
                card.ProductName.SetMessage(FormattedMessage.FromUnformatted(display.Name), defaultColor: Color.White);
                card.Description.SetMessage(FormattedMessage.FromUnformatted(display.Description));

                card.AddButton.OnPressed += _ => AddToCart(key);
                card.RemoveButton.OnPressed += _ => RemoveFromCart(key);

                _productCards[key] = card;
                UpdateProductCard(card, key);
                _window.ProductsContainer.AddChild(card);
                added++;
            }
        }

        if (added == 0)
        {
            _window.ProductsContainer.AddChild(new Label
            {
                Text = Loc.GetString("rmc-requisitions-products-empty"),
            });
        }
    }

    private ProductDisplay GetDisplay(RequisitionsEntry entry)
    {
        _prototypes.TryIndex<EntityPrototype>(entry.Crate, out var prototype);

        var name = prototype?.Name ?? entry.Crate.ToString();
        if (!string.IsNullOrWhiteSpace(entry.Name))
            name = entry.Name;
        if (entry.NameLocId != null && Loc.TryGetString(entry.NameLocId, out var localizedName))
            name = localizedName;

        var description = prototype?.Description ?? string.Empty;
        if (entry.DescriptionLocId != null && Loc.TryGetString(entry.DescriptionLocId, out var localizedDescription))
            description = localizedDescription;
        if (string.IsNullOrWhiteSpace(description))
            description = Loc.GetString("rmc-requisitions-card-no-description");

        var icon = prototype != null
            ? _sprite.GetPrototypeIcon(prototype).GetFrame(RsiDirection.South, 0)
            : null;

        return new ProductDisplay(name, description, icon);
    }

    private static bool MatchesSearch(ProductDisplay display, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        return display.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase);
    }

    private void AddToCart(CartKey key)
    {
        if (!TryGetEntry(key, out var entry))
            return;

        if (!CanAdd(entry.Cost))
            return;

        _cart[key] = GetCartAmount(key) + 1;
        RefreshCart();
        UpdateVisibleProductCards();
    }

    private void RemoveFromCart(CartKey key)
    {
        var amount = GetCartAmount(key);
        if (amount <= 0)
            return;

        if (amount == 1)
            _cart.Remove(key);
        else
            _cart[key] = amount - 1;

        RefreshCart();
        UpdateVisibleProductCards();
    }

    private bool CanAdd(int cost)
    {
        if (_state == null)
            return false;

        return GetCartTotal() + cost <= _state.Balance &&
               GetCartAmount() + 1 <= GetRemainingCapacity();
    }

    private void RefreshCart()
    {
        if (_window == null)
            return;

        _window.CartContainer.DisposeAllChildren();

        var search = _window.SearchBar.Text.Trim();
        var scopedItems = 0;
        var visibleItems = 0;
        var items = new List<(CartKey Key, int Amount)>(_cart.Count);
        foreach (var (key, amount) in _cart)
        {
            items.Add((key, amount));
        }

        items.Sort((a, b) =>
        {
            var categoryComparison = a.Key.Category.CompareTo(b.Key.Category);
            return categoryComparison != 0
                ? categoryComparison
                : a.Key.Entry.CompareTo(b.Key.Entry);
        });

        foreach (var (key, amount) in items)
        {
            if (_selectedCategory != null && _selectedCategory != key.Category)
                continue;

            if (!TryGetEntry(key, out var entry))
                continue;

            scopedItems++;
            var display = GetDisplay(entry);
            if (!MatchesSearch(display, search))
                continue;

            visibleItems++;
            var row = new RequisitionsCartRow
            {
                CategoryIndex = key.Category,
                EntryIndex = key.Entry,
                UnitCost = entry.Cost,
                Quantity = { Text = amount.ToString() },
                Cost = { Text = Loc.GetString("rmc-requisitions-cart-row-cost", ("cost", entry.Cost * amount)) },
                Icon = { Texture = display.Icon },
            };
            row.ProductName.SetMessage(FormattedMessage.FromUnformatted(display.Name), defaultColor: Color.White);
            row.Description.SetMessage(FormattedMessage.FromUnformatted(display.Description));

            row.AddButton.OnPressed += _ => AddToCart(key);
            row.RemoveButton.OnPressed += _ => RemoveFromCart(key);
            row.AddButton.Disabled = !CanAdd(entry.Cost);

            _window.CartContainer.AddChild(row);
        }

        var total = GetCartTotal();
        var cartAmount = GetCartAmount();
        var remainingCapacity = GetRemainingCapacity();

        _window.CartTotalLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString(
            "rmc-requisitions-cart-total",
            ("total", total))));

        var status = string.Empty;
        if (_cart.Count == 0)
            status = Loc.GetString("rmc-requisitions-cart-empty");
        else if (visibleItems == 0 && !string.IsNullOrWhiteSpace(search))
            status = Loc.GetString("rmc-requisitions-cart-filter-empty");
        else if (scopedItems == 0)
            status = Loc.GetString("rmc-requisitions-cart-category-empty");
        else if (_state != null && total > _state.Balance)
            status = Loc.GetString("rmc-requisitions-cart-insufficient-funds");
        else if (cartAmount > remainingCapacity)
            status = Loc.GetString("rmc-requisitions-cart-insufficient-capacity");

        _window.CartStatusLabel.SetMessage(FormattedMessage.FromUnformatted(status));

        _window.ClearCartButton.Disabled = _cart.Count == 0;
        _window.BuyButton.Disabled = _cart.Count == 0 ||
                                     _state == null ||
                                     total > _state.Balance ||
                                     cartAmount > remainingCapacity;
    }

    private void PopulatePendingOrders()
    {
        if (_window == null)
            return;

        _window.PendingContainer.DisposeAllChildren();

        if (_state == null || _state.PendingOrders.Count == 0)
        {
            _window.PendingStatusLabel.SetMessage(FormattedMessage.FromUnformatted(Loc.GetString("rmc-requisitions-pending-empty")));
            return;
        }

        var search = _window.SearchBar.Text.Trim();
        var visibleOrders = new List<(RequisitionsPendingOrder Order, int? Category, ProductDisplay Display)>();
        foreach (var order in _state.PendingOrders)
        {
            var category = GetPendingOrderCategory(order.Entry);
            if (_selectedCategory != null && category != _selectedCategory)
                continue;

            var display = GetDisplay(order.Entry);
            if (!MatchesSearch(display, search))
                continue;

            visibleOrders.Add((order, category, display));
        }

        visibleOrders.Sort((a, b) =>
        {
            var categoryComparison = (a.Category ?? int.MaxValue).CompareTo(b.Category ?? int.MaxValue);
            if (categoryComparison != 0)
                return categoryComparison;

            return string.Compare(a.Display.Name, b.Display.Name, StringComparison.CurrentCultureIgnoreCase);
        });

        foreach (var (order, _, display) in visibleOrders)
        {
            var card = new RequisitionsPendingOrderCard
            {
                Cost = { Text = Loc.GetString("rmc-requisitions-card-cost", ("cost", order.Entry.Cost)) },
                Quantity = { Text = Loc.GetString("rmc-requisitions-pending-quantity", ("amount", order.Amount)) },
                Icon = { Texture = display.Icon },
            };
            card.ProductName.SetMessage(FormattedMessage.FromUnformatted(display.Name), defaultColor: Color.White);
            card.Description.SetMessage(FormattedMessage.FromUnformatted(display.Description));

            _window.PendingContainer.AddChild(card);
        }

        var status = (visibleOrders.Count, string.IsNullOrWhiteSpace(search)) switch
        {
            (0, false) => Loc.GetString("rmc-requisitions-pending-filter-empty"),
            (0, true) => Loc.GetString("rmc-requisitions-pending-category-empty"),
            _ => string.Empty,
        };
        _window.PendingStatusLabel.SetMessage(FormattedMessage.FromUnformatted(status));
    }

    private void UpdateVisibleProductCards()
    {
        foreach (var (key, card) in _productCards)
        {
            UpdateProductCard(card, key);
        }
    }

    private void UpdateProductCard(RequisitionsProductCard card, CartKey key)
    {
        var amount = GetCartAmount(key);
        card.Quantity.Text = amount.ToString();
        card.RemoveButton.Disabled = amount <= 0;
        card.AddButton.Disabled = !TryGetEntry(key, out var entry) || !CanAdd(entry.Cost);
    }

    private void BuyCart()
    {
        if (_cart.Count == 0)
            return;

        var items = new List<RequisitionsCartItem>();
        foreach (var (key, amount) in _cart)
        {
            if (amount > 0)
                items.Add(new RequisitionsCartItem(key.Category, key.Entry, amount));
        }

        if (items.Count == 0)
            return;

        SendMessage(new RequisitionsBuyCartMsg(items));
        _cart.Clear();
        RefreshCart();
        UpdateVisibleProductCards();
    }

    private int? GetPendingOrderCategory(RequisitionsEntry entry)
    {
        if (!_entities.TryGetComponent(Owner, out RequisitionsComputerComponent? computer))
            return null;

        for (var categoryIndex = 0; categoryIndex < computer.Categories.Count; categoryIndex++)
        {
            var category = computer.Categories[categoryIndex];
            foreach (var categoryEntry in category.Entries)
            {
                if (SamePendingEntry(categoryEntry, entry))
                    return categoryIndex;
            }
        }

        return null;
    }

    private static bool SamePendingEntry(RequisitionsEntry a, RequisitionsEntry b)
    {
        if (a.Crate != b.Crate ||
            a.Cost != b.Cost ||
            a.Name != b.Name ||
            a.NameLocId != b.NameLocId ||
            a.DescriptionLocId != b.DescriptionLocId ||
            a.Entities.Count != b.Entities.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Entities.Count; i++)
        {
            if (a.Entities[i] != b.Entities[i])
                return false;
        }

        return true;
    }

    private bool TryGetEntry(CartKey key, out RequisitionsEntry entry)
    {
        entry = default!;

        if (!_entities.TryGetComponent(Owner, out RequisitionsComputerComponent? computer) ||
            key.Category < 0 ||
            key.Category >= computer.Categories.Count)
        {
            return false;
        }

        var category = computer.Categories[key.Category];
        if (key.Entry < 0 || key.Entry >= category.Entries.Count)
            return false;

        entry = category.Entries[key.Entry];
        return true;
    }

    private int GetCartAmount(CartKey key)
    {
        return _cart.GetValueOrDefault(key);
    }

    private int GetCartAmount()
    {
        var amount = 0;
        foreach (var count in _cart.Values)
        {
            amount += count;
        }

        return amount;
    }

    private int GetCartTotal()
    {
        var total = 0;
        foreach (var (key, amount) in _cart)
        {
            if (TryGetEntry(key, out var entry))
                total += entry.Cost * amount;
        }

        return total;
    }

    private int GetRemainingCapacity()
    {
        if (_state == null)
            return 0;

        return Math.Max(0, _state.Capacity - _state.OrderCount);
    }

    private readonly record struct CartKey(int Category, int Entry);

    private readonly record struct ProductDisplay(string Name, string Description, Robust.Client.Graphics.Texture? Icon);
}
