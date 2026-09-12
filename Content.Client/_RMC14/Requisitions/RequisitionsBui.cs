using System.Numerics;
using System.Linq;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Content.Shared._RMC14.Requisitions.Components.RequisitionsElevatorMode;
using static Robust.Client.UserInterface.Control;

namespace Content.Client._RMC14.Requisitions;

[UsedImplicitly]
public sealed class RequisitionsBui : BoundUserInterface, IRefreshableBui
{
    private const float CategoryMinWidth = 180f;
    private const float CategoryPanelPadding = 12f;
    private static readonly string[] MendozaDialogueLocIds =
    [
        "rmc-requisitions-black-market-mendoza-random-1",
        "rmc-requisitions-black-market-mendoza-random-2",
        "rmc-requisitions-black-market-mendoza-random-3",
        "rmc-requisitions-black-market-mendoza-random-4",
        "rmc-requisitions-black-market-mendoza-random-5",
        "rmc-requisitions-black-market-mendoza-random-6",
        "rmc-requisitions-black-market-mendoza-random-7",
    ];

    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly SpriteSystem _sprite;
    private readonly Dictionary<CartKey, int> _cart = new();
    private readonly Dictionary<CartKey, int> _blackMarketCart = new();
    private readonly Dictionary<CartKey, RequisitionsProductCard> _productCards = new();

    private RequisitionsComputerComponent? _state;
    private RequisitionsWindow? _window;
    private int? _selectedCategory;
    private int? _selectedBlackMarketCategory;
    private bool _blackMarketMode;
    private bool _mendozaIntroSeen;
    private bool _mendozaBriefingExpanded;
    private bool _mendozaDialogueRolled;
    private string? _mendozaDialogue;

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
        Refresh();
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

    public void Refresh()
    {
        EnsureWindow();
        RefreshShop();

        if (_window is { IsOpen: false })
            _window.OpenCentered();
    }

    private void RefreshShop()
    {
        _entities.TryGetComponent(Owner, out _state);

        if (_state is
            {
                BlackMarketUnlocked: false,
                BlackMarketStatus: not RequisitionsBlackMarketStatus.LockedOut,
            })
        {
            _blackMarketMode = false;
            _mendozaDialogueRolled = false;
        }

        if (_state is { } computer &&
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

        var buttons = new List<Button>();
        if (_blackMarketMode)
        {
            var returnButton = CreateCategoryButton(Loc.GetString("rmc-requisitions-black-market-return"), false);
            returnButton.OnPressed += _ =>
            {
                _blackMarketMode = false;
                _selectedBlackMarketCategory = null;
                _mendozaDialogueRolled = false;
                ClearSearch();
                RefreshShop();
            };
            buttons.Add(returnButton);

            buttons.Add(CreateBlackMarketButton(true));
        }
        else
        {
            var allButton = CreateCategoryButton(Loc.GetString("rmc-requisitions-category-all"), SelectedCategory == null);
            allButton.OnPressed += _ =>
            {
                SelectedCategory = null;
                RefreshShop();
            };
            buttons.Add(allButton);

            if (_entities.TryGetComponent(Owner, out RequisitionsComputerComponent? computer))
            {
                for (var i = 0; i < computer.Categories.Count; i++)
                {
                    var categoryIndex = i;
                    var button = CreateCategoryButton(Loc.GetString(computer.Categories[i].Name), SelectedCategory == categoryIndex);
                    button.OnPressed += _ =>
                    {
                        SelectedCategory = categoryIndex;
                        RefreshShop();
                    };
                    buttons.Add(button);
                }
            }

            if (_state is { BlackMarketUnlocked: true } or { BlackMarketStatus: RequisitionsBlackMarketStatus.LockedOut })
            {
                var blackMarketButton = CreateBlackMarketButton(false);
                blackMarketButton.OnPressed += _ =>
                {
                    _blackMarketMode = true;
                    _selectedBlackMarketCategory = null;
                    _mendozaDialogueRolled = false;
                    ClearSearch();
                    RefreshShop();
                };
                buttons.Add(blackMarketButton);
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

        var search = _window.SearchBar.Text.Trim();
        if (_blackMarketMode)
        {
            if (!CanUseBlackMarket())
            {
                PopulateBlackMarketStatus();
                return;
            }

            if (SelectedCategory == null && string.IsNullOrWhiteSpace(search))
            {
                PopulateBlackMarketHome(computer);
                return;
            }
        }

        var categories = GetCurrentCategories(computer);
        if (_blackMarketMode &&
            SelectedCategory is { } selectedCategory &&
            selectedCategory < categories.Count)
        {
            PopulateBlackMarketCategoryHeader(categories[selectedCategory]);
        }

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
                    Icon =
                    {
                        Textures = display.IconTextures,
                        Modulate = display.IconModulate,
                    },
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

    private void PopulateBlackMarketStatus()
    {
        if (_state?.BlackMarketStatus == RequisitionsBlackMarketStatus.LockedOut)
        {
            PopulateBlackMarketSeizureNotice();
            return;
        }

        if (_state?.BlackMarketStatus == RequisitionsBlackMarketStatus.MendozaDead)
        {
            AddProductsLabel(Loc.GetString("rmc-requisitions-black-market-mendoza-dead-dialogue"));
            return;
        }

        AddProductsLabel(GetBlackMarketStatusText());
    }

    private void PopulateBlackMarketHome(RequisitionsComputerComponent computer)
    {
        if (_window == null || _state == null)
            return;

        var home = new RequisitionsBlackMarketHome();
        home.BalanceLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString(
            "rmc-requisitions-black-market-balance",
            ("wy", _state.BlackMarketBalance))));

        var showBriefingButton = !_mendozaIntroSeen || _mendozaBriefingExpanded;
        home.DialoguePanel.Visible = PopulateBlackMarketDialogue(home.DialogueContents);

        if (showBriefingButton)
        {
            home.BriefingButton.Visible = true;
            home.BriefingButton.Text = Loc.GetString(_mendozaBriefingExpanded
                ? "rmc-requisitions-black-market-briefing-hide"
                : "rmc-requisitions-black-market-briefing-show");
            home.BriefingButton.OnPressed += _ =>
            {
                _mendozaBriefingExpanded = !_mendozaBriefingExpanded;
                _mendozaDialogueRolled = false;
                RefreshShop();
            };
        }

        PopulateBlackMarketCategoryButtons(home.CategoryButtons, computer.BlackMarketCategories);

        _window.ProductsContainer.AddChild(home);
    }

    private bool PopulateBlackMarketDialogue(BoxContainer container)
    {
        if (_mendozaBriefingExpanded)
        {
            PopulateMendozaFullBriefing(container);
            return true;
        }

        if (!_mendozaIntroSeen)
        {
            AddProductsLabel(container, Loc.GetString("rmc-requisitions-black-market-mendoza-intro-1"));
            AddProductsLabel(container, Loc.GetString("rmc-requisitions-black-market-mendoza-intro-2"));
            AddProductsLabel(container, Loc.GetString("rmc-requisitions-black-market-mendoza-instructions"), Color.White);
            _mendozaIntroSeen = true;
            return true;
        }

        if (!_mendozaDialogueRolled)
        {
            _mendozaDialogueRolled = true;
            _mendozaDialogue = Random.Shared.Next(100) < 30
                ? MendozaDialogueLocIds[Random.Shared.Next(MendozaDialogueLocIds.Length)]
                : null;
        }

        if (_mendozaDialogue == null)
            return false;

        AddProductsLabel(container, Loc.GetString(_mendozaDialogue));
        return true;
    }

    private static void PopulateMendozaFullBriefing(BoxContainer container)
    {
        AddProductsLabel(container, Loc.GetString("rmc-requisitions-black-market-mendoza-intro-1"));
        AddProductsLabel(container, Loc.GetString("rmc-requisitions-black-market-mendoza-intro-2"));
        AddProductsLabel(container, Loc.GetString("rmc-requisitions-black-market-mendoza-intro-3"));
        AddProductsLabel(container, Loc.GetString("rmc-requisitions-black-market-mendoza-intro-4"));
        AddProductsLabel(container, Loc.GetString("rmc-requisitions-black-market-mendoza-intro-5"));
        AddProductsLabel(container, Loc.GetString("rmc-requisitions-black-market-mendoza-instructions"), Color.White);
    }

    private void PopulateBlackMarketCategoryHeader(RequisitionsCategory category)
    {
        if (_window == null)
            return;

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 6),
        };

        var backButton = CreateBlackMarketButton(false);
        backButton.Text = Loc.GetString("rmc-requisitions-black-market-home");
        backButton.MinWidth = 120;
        backButton.HorizontalExpand = false;
        backButton.OnPressed += _ =>
        {
            _selectedBlackMarketCategory = null;
            _mendozaDialogueRolled = false;
            ClearSearch();
            RefreshShop();
        };
        header.AddChild(backButton);

        var title = new RichTextLabel
        {
            HorizontalExpand = true,
            Margin = new Thickness(8, 4, 0, 0),
        };
        title.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString(
            "rmc-requisitions-products-header",
            ("category", Loc.GetString(category.Name)))));
        header.AddChild(title);

        header.AddChild(CreateBlackMarketBalanceLabel());

        _window.ProductsContainer.AddChild(header);
    }

    private RichTextLabel CreateBlackMarketBalanceLabel()
    {
        var balance = new RichTextLabel
        {
            HorizontalAlignment = HAlignment.Right,
        };
        balance.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString(
            "rmc-requisitions-black-market-balance",
            ("wy", _state?.BlackMarketBalance ?? 0))));

        return balance;
    }

    private void PopulateBlackMarketCategoryButtons(BoxContainer contents, List<RequisitionsCategory> categories)
    {
        if (categories.Count == 0)
        {
            contents.AddChild(new Label
            {
                Text = Loc.GetString("rmc-requisitions-black-market-no-categories"),
            });
            return;
        }

        for (var i = 0; i < categories.Count; i += 2)
        {
            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                Margin = new Thickness(0, 2),
            };

            AddBlackMarketCategoryButton(row, categories, i);
            if (i + 1 < categories.Count)
                AddBlackMarketCategoryButton(row, categories, i + 1);
            else
                row.AddChild(new Control { HorizontalExpand = true });

            contents.AddChild(row);
        }
    }

    private void AddBlackMarketCategoryButton(BoxContainer row, List<RequisitionsCategory> categories, int categoryIndex)
    {
        var button = CreateCategoryButton(Loc.GetString(categories[categoryIndex].Name), false);
        button.HorizontalExpand = true;
        button.Margin = new Thickness(2, 0);
        button.OnPressed += _ =>
        {
            _selectedBlackMarketCategory = categoryIndex;
            _mendozaDialogueRolled = false;
            ClearSearch();
            RefreshShop();
        };
        row.AddChild(button);
    }

    private void PopulateBlackMarketSeizureNotice()
    {
        if (_window == null)
            return;

        _window.ProductsContainer.AddChild(new RequisitionsBlackMarketSeizureNotice());
    }

    private void AddProductsLabel(string text, Color? color = null)
    {
        if (_window == null)
            return;

        _window.ProductsContainer.AddChild(CreateProductsLabel(text, color));
    }

    private static void AddProductsLabel(BoxContainer container, string text, Color? color = null)
    {
        container.AddChild(CreateProductsLabel(text, color));
    }

    private static RichTextLabel CreateProductsLabel(string text, Color? color = null)
    {
        var label = new RichTextLabel();
        label.SetMessage(FormattedMessage.FromUnformatted(text), defaultColor: color ?? Color.LightGray);
        return label;
    }

    private void ClearSearch()
    {
        if (_window == null || string.IsNullOrEmpty(_window.SearchBar.Text))
            return;

        _window.SearchBar.Text = string.Empty;
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

        var icon = GetDisplayIcon(entry.Icon, prototype);

        return new ProductDisplay(name, description, icon.Textures, icon.Modulate);
    }

    private DisplayIcon GetDisplayIcon(SpriteSpecifier? icon, EntityPrototype? fallbackPrototype)
    {
        if (icon is SpriteSpecifier.EntityPrototype entityIcon &&
            _prototypes.TryIndex<EntityPrototype>(entityIcon.EntityPrototypeId, out var iconPrototype))
        {
            return GetPrototypeDisplayIcon(iconPrototype);
        }

        if (icon != null)
            return new DisplayIcon(new List<Texture> { _sprite.Frame0(icon) }, Color.White);

        if (fallbackPrototype != null)
            return GetPrototypeDisplayIcon(fallbackPrototype);

        return new DisplayIcon(new List<Texture>(), Color.White);
    }

    private DisplayIcon GetPrototypeDisplayIcon(EntityPrototype prototype)
    {
        var textures = _sprite.GetPrototypeTextures(prototype)
            .Select(texture => texture.Default)
            .ToList();

        var modulate = Color.White;
        if (prototype.TryGetComponent<SpriteComponent>("Sprite", out var sprite) &&
            sprite.AllLayers.Any())
        {
            modulate = sprite.AllLayers.First().Color;
        }

        return new DisplayIcon(textures, modulate);
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
            if (!_blackMarketMode && SelectedCategory != null && SelectedCategory != key.Category)
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
                Icon =
                {
                    Textures = display.IconTextures,
                    Modulate = display.IconModulate,
                },
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
                Icon =
                {
                    Textures = display.IconTextures,
                    Modulate = display.IconModulate,
                },
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

            return true;
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

    private readonly record struct DisplayIcon(List<Texture> Textures, Color Modulate);

    private readonly record struct ProductDisplay(string Name, string Description, List<Texture> IconTextures, Color IconModulate);
}
