using System.Numerics;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
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
    private readonly Dictionary<CartKey, int> _blackMarketCart = new();
    private readonly Dictionary<CartKey, RequisitionsProductCard> _productCards = new();

    private RequisitionsBuiState? _state;
    private RequisitionsWindow? _window;
    private int? _selectedCategory;
    private int? _selectedBlackMarketCategory;
    private bool _blackMarketMode;

    private int? SelectedCategory
    {
        get => _blackMarketMode ? _selectedBlackMarketCategory : _selectedCategory;
        set
        {
            if (_blackMarketMode)
                _selectedBlackMarketCategory = value;
            else
                _selectedCategory = value;
        }
    }

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
            GetCurrentCart().Clear();
            RefreshVisibleEntries();
        };
        _window.BuyButton.OnPressed += _ => BuyCart();
    }

    private void RefreshShop()
    {
        if (_state is { BlackMarketUnlocked: false })
            _blackMarketMode = false;

        if (_entities.TryGetComponent(Owner, out RequisitionsComputerComponent? computer) &&
            SelectedCategory is { } selectedCategory &&
            selectedCategory >= GetCurrentCategories(computer).Count)
        {
            SelectedCategory = null;
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

        var balance = _blackMarketMode
            ? Loc.GetString(
                "rmc-requisitions-black-market-balance",
                ("balance", _state.Balance),
                ("wy", _state.BlackMarketBalance),
                ("heat", _state.BlackMarketHeat))
            : Loc.GetString("rmc-requisitions-balance", ("balance", _state.Balance));

        _window.BudgetLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(balance));

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
        if (_blackMarketMode)
        {
            var returnButton = CreateCategoryButton(Loc.GetString("rmc-requisitions-black-market-return"), false);
            returnButton.OnPressed += _ =>
            {
                _blackMarketMode = false;
                RefreshShop();
            };
            buttons.Add(returnButton);
        }

        var allButton = CreateCategoryButton(Loc.GetString("rmc-requisitions-category-all"), SelectedCategory == null);
        allButton.OnPressed += _ =>
        {
            SelectedCategory = null;
            RefreshShop();
        };
        buttons.Add(allButton);

        if (computer != null)
        {
            var categories = GetCurrentCategories(computer);
            for (var i = 0; i < categories.Count; i++)
            {
                var categoryIndex = i;
                var button = CreateCategoryButton(Loc.GetString(categories[i].Name), SelectedCategory == categoryIndex);
                button.OnPressed += _ =>
                {
                    SelectedCategory = categoryIndex;
                    RefreshShop();
                };
                buttons.Add(button);
            }
        }

        if (!_blackMarketMode && _state is { BlackMarketUnlocked: true })
        {
            var blackMarketButton = CreateBlackMarketButton(_blackMarketMode);
            blackMarketButton.OnPressed += _ =>
            {
                _blackMarketMode = true;
                _selectedBlackMarketCategory = null;
                RefreshShop();
            };
            buttons.Add(blackMarketButton);
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

    private static Button CreateBlackMarketButton(bool disabled)
    {
        var button = CreateCategoryButton(Loc.GetString("rmc-requisitions-black-market-button"), disabled);
        button.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#311218"),
            BorderColor = Color.FromHex("#ce3349"),
            BorderThickness = new Thickness(2),
        };
        return button;
    }

    private void PopulateProducts()
    {
        if (_window == null)
            return;

        _window.ProductsContainer.DisposeAllChildren();
        _productCards.Clear();

        if (!_entities.TryGetComponent(Owner, out RequisitionsComputerComponent? computer))
            return;

        if (_blackMarketMode && !CanUseBlackMarket())
        {
            _window.ProductsContainer.AddChild(new Label
            {
                Text = GetBlackMarketStatusText(),
            });
            return;
        }

        var categories = GetCurrentCategories(computer);
        var search = _window.SearchBar.Text.Trim();
        var added = 0;

        for (var categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
        {
            if (SelectedCategory != null && SelectedCategory != categoryIndex)
                continue;

            var category = categories[categoryIndex];
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
                    UnitCost = entry.BlackMarket ? entry.BlackMarketCost : entry.Cost,
                    Cost = { Text = GetCostText(entry) },
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
        if (entry.Name is { } nameOverride && Loc.TryGetString(nameOverride, out var localizedName))
            name = localizedName;

        var description = prototype?.Description ?? string.Empty;
        if (entry.Description is { } descriptionOverride && Loc.TryGetString(descriptionOverride, out var localizedDescription))
            description = localizedDescription;
        if (string.IsNullOrWhiteSpace(description))
            description = Loc.GetString("rmc-requisitions-card-no-description");

        var icon = entry.Icon != null
            ? _sprite.Frame0(entry.Icon)
            : prototype != null
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

        if (!CanAdd(entry))
            return;

        var cart = GetCurrentCart();
        cart[key] = GetCartAmount(key) + 1;
        RefreshCart();
        UpdateVisibleProductCards();
    }

    private void RemoveFromCart(CartKey key)
    {
        var cart = GetCurrentCart();
        var amount = GetCartAmount(key);
        if (amount <= 0)
            return;

        if (amount == 1)
            cart.Remove(key);
        else
            cart[key] = amount - 1;

        RefreshCart();
        UpdateVisibleProductCards();
    }

    private bool CanAdd(RequisitionsEntry entry)
    {
        if (_state == null)
            return false;

        var supplyTotal = GetCartSupplyTotal() + entry.Cost;
        var blackMarketTotal = GetCartBlackMarketTotal() + entry.BlackMarketCost;
        if (entry.BlackMarket &&
            (!CanUseBlackMarket() || blackMarketTotal > _state.BlackMarketBalance))
        {
            return false;
        }

        return supplyTotal <= _state.Balance &&
               GetCartAmount() + 1 <= GetRemainingCapacity();
    }

    private void RefreshCart()
    {
        if (_window == null)
            return;

        _window.CartContainer.DisposeAllChildren();

        var cart = GetCurrentCart();
        var search = _window.SearchBar.Text.Trim();
        var scopedItems = 0;
        var visibleItems = 0;
        var items = new List<(CartKey Key, int Amount)>(cart.Count);
        foreach (var (key, amount) in cart)
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
            if (SelectedCategory != null && SelectedCategory != key.Category)
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
                UnitCost = entry.BlackMarket ? entry.BlackMarketCost : entry.Cost,
                Quantity = { Text = amount.ToString() },
                Cost = { Text = GetLineCostText(entry, amount) },
                Icon = { Texture = display.Icon },
            };
            row.ProductName.SetMessage(FormattedMessage.FromUnformatted(display.Name), defaultColor: Color.White);
            row.Description.SetMessage(FormattedMessage.FromUnformatted(display.Description));

            row.AddButton.OnPressed += _ => AddToCart(key);
            row.RemoveButton.OnPressed += _ => RemoveFromCart(key);
            row.AddButton.Disabled = !CanAdd(entry);

            _window.CartContainer.AddChild(row);
        }

        var supplyTotal = GetCartSupplyTotal();
        var blackMarketTotal = GetCartBlackMarketTotal();
        var cartAmount = GetCartAmount();
        var remainingCapacity = GetRemainingCapacity();

        _window.CartTotalLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(GetCartTotalText(supplyTotal, blackMarketTotal)));

        var status = string.Empty;
        if (cart.Count == 0)
            status = Loc.GetString("rmc-requisitions-cart-empty");
        else if (_blackMarketMode && !CanUseBlackMarket())
            status = GetBlackMarketStatusText();
        else if (visibleItems == 0 && !string.IsNullOrWhiteSpace(search))
            status = Loc.GetString("rmc-requisitions-cart-filter-empty");
        else if (scopedItems == 0)
            status = Loc.GetString("rmc-requisitions-cart-category-empty");
        else if (_state != null && supplyTotal > _state.Balance)
            status = Loc.GetString("rmc-requisitions-cart-insufficient-funds");
        else if (_state != null && blackMarketTotal > _state.BlackMarketBalance)
            status = Loc.GetString("rmc-requisitions-cart-insufficient-wy");
        else if (cartAmount > remainingCapacity)
            status = Loc.GetString("rmc-requisitions-cart-insufficient-capacity");

        _window.CartStatusLabel.SetMessage(FormattedMessage.FromUnformatted(status));

        _window.ClearCartButton.Disabled = cart.Count == 0;
        _window.BuyButton.Disabled = cart.Count == 0 ||
                                     _state == null ||
                                     (_blackMarketMode && !CanUseBlackMarket()) ||
                                     supplyTotal > _state.Balance ||
                                     blackMarketTotal > _state.BlackMarketBalance ||
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
            if (!ShouldShowPendingOrder(order.Entry, category))
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
                Cost = { Text = GetCostText(order.Entry) },
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
        card.AddButton.Disabled = !TryGetEntry(key, out var entry) || !CanAdd(entry);
    }

    private void BuyCart()
    {
        var cart = GetCurrentCart();
        if (cart.Count == 0)
            return;

        var items = new List<RequisitionsCartItem>();
        foreach (var (key, amount) in cart)
        {
            if (amount > 0)
                items.Add(new RequisitionsCartItem(key.Category, key.Entry, amount));
        }

        if (items.Count == 0)
            return;

        if (_blackMarketMode)
            SendMessage(new RequisitionsBuyBlackMarketCartMsg(items));
        else
            SendMessage(new RequisitionsBuyCartMsg(items));

        cart.Clear();
        RefreshCart();
        UpdateVisibleProductCards();
    }

    private int? GetPendingOrderCategory(RequisitionsEntry entry)
    {
        if (!_entities.TryGetComponent(Owner, out RequisitionsComputerComponent? computer))
            return null;

        var categories = entry.BlackMarket ? computer.BlackMarketCategories : computer.Categories;
        for (var categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
        {
            var category = categories[categoryIndex];
            foreach (var categoryEntry in category.Entries)
            {
                if (SamePendingEntry(categoryEntry, entry))
                    return categoryIndex;
            }
        }

        return null;
    }

    private bool ShouldShowPendingOrder(RequisitionsEntry entry, int? category)
    {
        if (_blackMarketMode)
        {
            if (!entry.BlackMarket)
                return false;

            return SelectedCategory == null || category == SelectedCategory;
        }

        if (SelectedCategory == null)
            return true;

        return !entry.BlackMarket && category == SelectedCategory;
    }

    private static bool SamePendingEntry(RequisitionsEntry a, RequisitionsEntry b)
    {
        if (a.Crate != b.Crate ||
            a.Cost != b.Cost ||
            a.BlackMarket != b.BlackMarket ||
            a.BlackMarketCost != b.BlackMarketCost ||
            a.BlackMarketHeat != b.BlackMarketHeat ||
            a.Name != b.Name ||
            a.Description != b.Description ||
            !Equals(a.Icon, b.Icon) ||
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
            key.Category < 0)
        {
            return false;
        }

        var categories = GetCurrentCategories(computer);
        if (key.Category >= categories.Count)
            return false;

        var category = categories[key.Category];
        if (key.Entry < 0 || key.Entry >= category.Entries.Count)
            return false;

        entry = category.Entries[key.Entry];
        return true;
    }

    private List<RequisitionsCategory> GetCurrentCategories(RequisitionsComputerComponent computer)
    {
        return _blackMarketMode ? computer.BlackMarketCategories : computer.Categories;
    }

    private Dictionary<CartKey, int> GetCurrentCart()
    {
        return _blackMarketMode ? _blackMarketCart : _cart;
    }

    private bool CanUseBlackMarket()
    {
        return _state is { BlackMarketUnlocked: true, BlackMarketStatus: RequisitionsBlackMarketStatus.Available };
    }

    private string GetBlackMarketStatusText()
    {
        if (_state == null)
            return Loc.GetString("rmc-requisitions-black-market-unavailable");

        return _state.BlackMarketStatus switch
        {
            RequisitionsBlackMarketStatus.LockedOut => Loc.GetString("rmc-requisitions-black-market-locked-out"),
            RequisitionsBlackMarketStatus.MendozaDead => Loc.GetString("rmc-requisitions-black-market-mendoza-dead"),
            _ => Loc.GetString("rmc-requisitions-black-market-unavailable"),
        };
    }

    private string GetCostText(RequisitionsEntry entry)
    {
        if (!entry.BlackMarket)
            return Loc.GetString("rmc-requisitions-card-cost", ("cost", entry.Cost));

        if (entry.Cost > 0)
        {
            return Loc.GetString(
                "rmc-requisitions-card-cost-dual",
                ("cost", entry.Cost),
                ("wy", entry.BlackMarketCost));
        }

        return Loc.GetString("rmc-requisitions-card-cost-wy", ("cost", entry.BlackMarketCost));
    }

    private string GetLineCostText(RequisitionsEntry entry, int amount)
    {
        if (!entry.BlackMarket)
            return Loc.GetString("rmc-requisitions-cart-row-cost", ("cost", entry.Cost * amount));

        if (entry.Cost > 0)
        {
            return Loc.GetString(
                "rmc-requisitions-cart-row-cost-dual",
                ("cost", entry.Cost * amount),
                ("wy", entry.BlackMarketCost * amount));
        }

        return Loc.GetString("rmc-requisitions-cart-row-cost-wy", ("cost", entry.BlackMarketCost * amount));
    }

    private string GetCartTotalText(int supplyTotal, int blackMarketTotal)
    {
        if (!_blackMarketMode)
            return Loc.GetString("rmc-requisitions-cart-total", ("total", supplyTotal));

        if (supplyTotal > 0)
        {
            return Loc.GetString(
                "rmc-requisitions-cart-total-dual",
                ("total", supplyTotal),
                ("wy", blackMarketTotal));
        }

        return Loc.GetString("rmc-requisitions-cart-total-wy", ("total", blackMarketTotal));
    }

    private int GetCartAmount(CartKey key)
    {
        return GetCurrentCart().GetValueOrDefault(key);
    }

    private int GetCartAmount()
    {
        var amount = 0;
        foreach (var count in GetCurrentCart().Values)
        {
            amount += count;
        }

        return amount;
    }

    private int GetCartSupplyTotal()
    {
        var total = 0;
        foreach (var (key, amount) in GetCurrentCart())
        {
            if (TryGetEntry(key, out var entry))
                total += entry.Cost * amount;
        }

        return total;
    }

    private int GetCartBlackMarketTotal()
    {
        var total = 0;
        foreach (var (key, amount) in GetCurrentCart())
        {
            if (TryGetEntry(key, out var entry))
                total += entry.BlackMarketCost * amount;
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
