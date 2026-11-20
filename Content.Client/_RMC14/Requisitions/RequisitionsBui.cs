using System.Linq;
using System.Numerics;
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

namespace Content.Client._RMC14.Requisitions;

[UsedImplicitly]
public sealed partial class RequisitionsBui : BoundUserInterface, IRefreshableBui
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
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly SpriteSystem _sprite;
    private readonly Dictionary<CartKey, RequisitionsProductCard> _productCards = new();

    private RequisitionsComputerComponent? _state;
    private RequisitionsStockPlan? _stockPlan;
    private RequisitionsWindow? _window;
    private int? _selectedCategory;
    private int? _selectedBlackMarketCategory;
    private bool _blackMarketMode;
    private bool _productCardsBlackMarket;
    private RequisitionsBlackMarketCategoryHeader? _blackMarketCategoryHeader;
    private (bool Mode, int? Category, bool RequestOnly, bool Unlocked,
        RequisitionsBlackMarketStatus Status, int Revision, string? Culture)? _categoryView;
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
        _prototypes.PrototypesReloaded += OnCatalogPrototypesReloaded;
    }

    protected override void Open()
    {
        if (_uiDisposed)
            return;
        base.Open();
        Refresh();
    }

    private void EnsureWindow()
    {
        if (_window != null)
            return;

        _window = this.CreateWindow<RequisitionsWindow>();

        _window.SearchBar.OnTextChanged += _ => RefreshVisibleEntries();
        _window.ShopTabs.OnTabChanged += _ => RefreshShop();
        _window.PreviousProductPage.OnPressed += _ => ChangeProductPage(CurrentCatalog.Page - 1);
        _window.NextProductPage.OnPressed += _ => ChangeProductPage(CurrentCatalog.Page + 1);
        _window.ProductPageNumber.IsValid = value => value > 0;
        _window.ProductPageNumber.ValueChanged += args => ChangeProductPage(args.Value - 1);
        _window.PlatformButton.OnPressed += _ => PressPlatformButton();
        _window.ClearCartButton.OnPressed += _ =>
        {
            GetCurrentCart().Clear();
            RefreshVisibleEntries();
        };
        _window.BuyButton.OnPressed += _ => BuyCart();
        _window.RequestReason.IsValid = text => text.Length <= (_state?.MaxRequestReasonLength ?? 0);
        _window.RequestReason.OnTextChanged += _ => RefreshCart();
    }

    public void Refresh()
    {
        if (_uiDisposed)
            return;
        EnsureWindow();
        RefreshShop();

        if (_window is { IsOpen: false })
            _window.OpenCentered();
    }

    private void RefreshShop()
    {
        if (_refreshingShop || _uiDisposed)
            return;
        _refreshingShop = true;
        try
        {
            RefreshShopContents();
        }
        finally
        {
            _refreshingShop = false;
        }
    }

    private void RefreshShopContents()
    {
        _entities.TryGetComponent(Owner, out _state);
        _stockPlan = _state == null ? null : new RequisitionsStockPlan(_state.Stock);
        UpdateRequestMode();
        SelectInitialRequestCategory();

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
        EnsureCatalogIndex();
        PopulateCategories();
        PopulateProducts();
        RefreshCart();
        PopulatePendingOrders();
        PopulateRequests();
    }

    private void RefreshVisibleEntries()
    {
        RefreshShop();
    }

    private void UpdatePlatformButtons()
    {
        if (_window == null)
            return;

        _window.PlatformButton.Disabled = true;

        if (_state == null)
        {
            SetText(_window.PlatformButton.Label, Loc.GetString("rmc-requisitions-platform-missing"));
            return;
        }

        if (_state.Busy || _state.PlatformLowered is Preparing or Lowering or Raising)
        {
            SetText(_window.PlatformButton.Label, Loc.GetString("rmc-requisitions-platform-busy"));
            return;
        }

        switch (_state.PlatformLowered)
        {
            case Lowered:
                SetText(_window.PlatformButton.Label, Loc.GetString("rmc-requisitions-platform-raise"));
                _window.PlatformButton.Disabled = false;
                break;
            case Raised:
                SetText(_window.PlatformButton.Label, Loc.GetString("rmc-requisitions-platform-lower"));
                _window.PlatformButton.Disabled = false;
                break;
            default:
                SetText(_window.PlatformButton.Label, Loc.GetString("rmc-requisitions-platform-missing"));
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

        SetText(_window.BudgetLabel, Loc.GetString(
            "rmc-requisitions-balance",
            ("balance", _state.Balance)), markup: true);

        SetText(_window.CapacityLabel, Loc.GetString(
            "rmc-requisitions-capacity",
            ("count", _state.OrderCount),
            ("capacity", _state.Capacity)), markup: true);
    }

    private void PopulateCategories()
    {
        if (_window == null)
            return;

        var view = (_blackMarketMode, SelectedCategory, RequestOnly, _state?.BlackMarketUnlocked ?? false,
            _state?.BlackMarketStatus ?? RequisitionsBlackMarketStatus.LockedOut,
            CurrentCatalog.Revision, _localization.DefaultCulture?.Name);
        if (_categoryView == view)
            return;
        _categoryView = view;
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

            if (!RequestOnly && _state is
                ({ BlackMarketUnlocked: true } or { BlackMarketStatus: RequisitionsBlackMarketStatus.LockedOut }))
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
        return new RequisitionsCategoryButton
        {
            Text = text,
            Disabled = disabled,
        };
    }

    private static Button CreateBlackMarketButton(bool disabled)
    {
        var button = new RequisitionsCategoryButton
        {
            Text = Loc.GetString("rmc-requisitions-black-market-button"),
            Disabled = disabled,
        };
        button.ApplyBlackMarketStyle();
        return button;
    }

    private void PopulateProducts()
    {
        if (_window == null || !_window.ProductsTab.Visible)
            return;

        if (!_entities.TryGetComponent(Owner, out RequisitionsComputerComponent? computer))
            return;

        var search = _window.SearchBar.Text.Trim();
        _window.ProductPages.Visible = true;
        if (_blackMarketMode)
        {
            if (!CanUseBlackMarket() || SelectedCategory == null && string.IsNullOrWhiteSpace(search))
            {
                _window.ProductsContainer.DisposeAllChildren();
                _blackMarketCategoryHeader = null;
                _productCards.Clear();
                _renderedCatalog = null;
                _window.ProductPages.Visible = false;
                if (!CanUseBlackMarket())
                    PopulateBlackMarketStatus();
                else
                    PopulateBlackMarketHome(computer);
                return;
            }
        }

        FilterCatalog();
        UpdateProductPages();
        var index = CurrentCatalog;
        if (_renderedCatalog == index && _renderedFilter == index.FilterRevision && _renderedPage == index.Page)
        {
            UpdateBlackMarketCategoryBalance();
            UpdateVisibleProductCards();
            return;
        }

        CartKey? focused = null;
        var cursor = 0;
        if (_productCardsBlackMarket == _blackMarketMode)
        {
            foreach (var (key, card) in _productCards)
            {
                if (!card.Quantity.LineEditControl.HasKeyboardFocus())
                    continue;
                focused = key;
                cursor = card.Quantity.LineEditControl.CursorPosition;
            }
        }
        _window.ProductsContainer.DisposeAllChildren();
        _blackMarketCategoryHeader = null;
        _productCards.Clear();
        _productCardsBlackMarket = _blackMarketMode;
        _renderedCatalog = index;
        _renderedFilter = index.FilterRevision;
        _renderedPage = index.Page;
        _window.ProductsScroll.SetScrollValue(Vector2.Zero);
        if (_blackMarketMode && SelectedCategory is { } selectedCategory)
            PopulateBlackMarketCategoryHeader(GetCurrentCategories(computer)[selectedCategory]);

        foreach (var item in index.Filtered.Skip(index.Page * ProductPageSize).Take(ProductPageSize))
        {
            var key = item.Key;
            if (!TryGetEntry(key, out var entry))
                continue;
            var display = GetDisplay(entry);
            var card = new RequisitionsProductCard
            {
                Icon =
                {
                    Textures = display.IconTextures,
                    Modulate = display.IconModulate,
                },
            };
            SetText(card.ProductName, display.Name, color: Color.White);
            SetText(card.Description, display.Description);
            card.AddButton.OnPressed += _ => AddToCart(key);
            card.RemoveButton.OnPressed += _ => RemoveFromCart(key);
            card.Quantity.IsValid = amount => amount >= 0;
            card.Quantity.ValueChanged += args =>
            {
                SetCartAmount(key, args.Value);
                if (card.Quantity.Value != GetCartAmount(key))
                    card.Quantity.OverrideValue(GetCartAmount(key));
            };
            _productCards[key] = card;
            UpdateProductCard(card, key);
            _window.ProductsContainer.AddChild(card);
            if (focused == key)
            {
                card.Quantity.LineEditControl.GrabKeyboardFocus();
                card.Quantity.LineEditControl.CursorPosition = Math.Min(cursor, card.Quantity.LineEditControl.Text.Length);
            }
        }

        if (index.Filtered.Count == 0)
        {
            AddProductsLabel(Loc.GetString("rmc-requisitions-products-empty"));
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

        home.DialoguePanel.Visible = PopulateBlackMarketDialogue(home.DialogueContents);

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

        var header = new RequisitionsBlackMarketCategoryHeader();
        _blackMarketCategoryHeader = header;
        header.BackButton.OnPressed += _ =>
        {
            _selectedBlackMarketCategory = null;
            _mendozaDialogueRolled = false;
            ClearSearch();
            RefreshShop();
        };
        SetText(header.Title, Loc.GetString(
            "rmc-requisitions-products-header",
            ("category", Loc.GetString(category.Name))), markup: true);
        UpdateBlackMarketCategoryBalance();

        _window.ProductsContainer.AddChild(header);
    }

    private void UpdateBlackMarketCategoryBalance()
    {
        if (_blackMarketCategoryHeader is { } header)
            SetText(header.Balance, Loc.GetString("rmc-requisitions-black-market-balance",
                ("wy", _state?.BlackMarketBalance ?? 0)), markup: true);
    }

    private void PopulateBlackMarketCategoryButtons(BoxContainer contents, List<RequisitionsCategory> categories)
    {
        if (categories.Count == 0)
        {
            AddProductsLabel(contents, Loc.GetString("rmc-requisitions-black-market-no-categories"));
            return;
        }

        for (var i = 0; i < categories.Count; i += 2)
        {
            var row = new RequisitionsCategoryRow();

            BindBlackMarketCategoryButton(row.FirstButton, categories, i);
            if (i + 1 < categories.Count)
                BindBlackMarketCategoryButton(row.SecondButton, categories, i + 1);
            else
            {
                row.SecondButton.Visible = false;
                row.Spacer.Visible = true;
            }

            contents.AddChild(row);
        }
    }

    private void BindBlackMarketCategoryButton(Button button, List<RequisitionsCategory> categories, int categoryIndex)
    {
        button.Text = Loc.GetString(categories[categoryIndex].Name);
        button.OnPressed += _ =>
        {
            _selectedBlackMarketCategory = categoryIndex;
            _mendozaDialogueRolled = false;
            ClearSearch();
            RefreshShop();
        };
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
        var label = new RequisitionsMessage();
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
        var text = GetDisplayText(entry);
        _prototypes.TryIndex<EntityPrototype>(entry.Crate, out var prototype);
        var icon = GetDisplayIcon(entry.Icon, prototype);
        return new ProductDisplay(text.Name, text.Description, icon.Textures, icon.Modulate);
    }

    private (string Name, string Description) GetDisplayText(RequisitionsEntry entry)
    {
        _prototypes.TryIndex<EntityPrototype>(entry.Crate, out var prototype);

        var name = prototype?.Name ?? entry.Crate.ToString();
        if (entry.Name is { } nameOverride && Loc.TryGetString(nameOverride, out var localizedName))
            name = localizedName;
        if (entry.Unit)
            name = RequisitionsUnitCatalog.GetUnitName(entry.Crate, name, _prototypes, _entities.ComponentFactory);

        var description = prototype?.Description ?? string.Empty;
        if (entry.Description is { } descriptionOverride && Loc.TryGetString(descriptionOverride, out var localizedDescription))
            description = localizedDescription;
        if (string.IsNullOrWhiteSpace(description))
            description = Loc.GetString("rmc-requisitions-card-no-description");

        if (entry.NoReturnReward && entry.Entities.Count > 0)
            description = GetShipmentContents(entry);

        return (name, description);
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
        return MatchesSearch(display.Name, search);
    }

    private static bool MatchesSearch(string name, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        return name.Contains(search, StringComparison.CurrentCultureIgnoreCase);
    }

    private void UpdateVisibleProductCards()
    {
        if (_window == null || !_window.ProductsTab.Visible)
            return;
        foreach (var (key, card) in _productCards)
        {
            UpdateProductCard(card, key);
        }
    }

    private void UpdateProductCard(RequisitionsProductCard card, CartKey key)
    {
        var amount = GetCartAmount(key);
        if (card.Quantity.Value != amount)
            card.Quantity.OverrideValue(amount);
        if (TryGetEntry(key, out var product))
        {
            SetText(card.Cost, GetCostText(product));
            card.Stock.Visible = product.Unit;
            var stock = _stockPlan?.GetAvailable(product.Crate) ?? 0;
            SetText(card.Stock, Loc.GetString("rmc-requisitions-unit-stock", ("amount", StockText(stock))));
            card.Quantity.LineEditControl.Editable = !_submittingRequest && (RequestOnly || !product.Unit || product.Cost > 0);
        }
        card.RemoveButton.Disabled = amount <= 0 || _submittingRequest;
        card.AddButton.Disabled = !TryGetEntry(key, out var entry) || !CanAdd(key, entry);
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

        if (RequestOnly)
        {
            SubmitRequest(items);
            return;
        }

        if (_blackMarketMode)
            SendMessage(new RequisitionsBuyBlackMarketCartMsg(items));
        else
        {
            _submittingRequest = true;
            SendMessage(new RequisitionsBuyCartMsg(items));
            RefreshCart();
            UpdateVisibleProductCards();
            return;
        }

        cart.Clear();
        RefreshCart();
        UpdateVisibleProductCards();
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
        if (entry.Unit)
            return entry.Cost > 0
                ? Loc.GetString("rmc-requisitions-unit-cost", ("cost", entry.Cost))
                : Loc.GetString("rmc-requisitions-stock-only");
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
            return Loc.GetString("rmc-requisitions-cart-row-cost", ("cost", (long) entry.Cost * amount));

        if (entry.Cost > 0)
        {
            return Loc.GetString(
                "rmc-requisitions-cart-row-cost-dual",
                ("cost", (long) entry.Cost * amount),
                ("wy", (long) entry.BlackMarketCost * amount));
        }

        return Loc.GetString("rmc-requisitions-cart-row-cost-wy", ("cost", (long) entry.BlackMarketCost * amount));
    }

    private string GetCartTotalText(long supplyTotal, long blackMarketTotal)
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
