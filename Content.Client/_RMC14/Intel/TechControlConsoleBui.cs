using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Intel.Tech;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Intel;

[UsedImplicitly]
public sealed class TechControlConsoleBui : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;

    private TechControlConsoleWindow? _window;
    private TechControlConsoleOptionWindow? _optionWindow;

    private readonly SharedGameTicker _ticker;
    public TechControlConsoleBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _ticker = _entities.System<SharedGameTicker>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<TechControlConsoleWindow>();
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out TechControlConsoleComponent? console))
            return;

        _window.Options.DisposeAllChildren();
        for (var i = console.Tree.Options.Count - 1; i >= 0; i--)
        {
            var header = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };
            header.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("rmc-ui-tech-tier-header", ("tier", i)),
            });

            if (i == console.Tree.Options.Count - 1)
            {
                header.AddChild(new Control { HorizontalExpand = true });
                header.AddChild(new RichTextLabel { Text = Loc.GetString("rmc-ui-tech-points", ("points", console.Tree.Points)) });
            }

            _window.Options.AddChild(header);
            _window.Options.AddChild(new BlueHorizontalSeparator());

            var optionContainer = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };
            _window.Options.AddChild(optionContainer);

            var options = console.Tree.Options[i];
            var spriteSystem = _entities.System<SpriteSystem>();
            var addedOption = false;
            for (var j = 0; j < options.Count; j++)
            {
                var option = options[j];
                if (option.Disabled)
                    continue;

                var optionControl = new Control();
                var texture = spriteSystem.RsiStateLike(option.Icon).Default;
                optionControl.AddChild(new TextureButton
                {
                    TextureNormal = texture,
                    SetSize = new Vector2(64, 64),
                });

                var overlay = option.Purchased ? console.UnlockedRsi : console.LockedRsi;
                var optionButton = new TextureButton
                {
                    TextureNormal = spriteSystem.RsiStateLike(overlay).Default,
                    Scale = new Vector2(2, 2),
                };
                optionControl.AddChild(optionButton);

                var tier = i;
                var optionIndex = j;
                optionButton.OnPressed += _ =>
                {
                    OpenOptionWindow(option, tier, optionIndex, console.Tree.Points, console.Tree.Tier);
                };
                optionButton.ToolTip = Localize(option.Name);
                optionButton.TooltipDelay = 0.1f;

                optionContainer.AddChild(new Control { HorizontalExpand = true });
                optionContainer.AddChild(optionControl);
                addedOption = true;

            }

            if (addedOption)
            {
                optionContainer.AddChild(new Control { HorizontalExpand = true });
            }
            else
            {
                _window.Options.RemoveChild(optionContainer);
            }
        }
    }

    private void OpenOptionWindow(TechOption option, int tier, int optionIndex, FixedPoint2 points, int currentTier)
    {
        if (_optionWindow is { IsOpen: true })
        {
            _optionWindow.Close();
            _optionWindow = null;
        }

        _optionWindow = new TechControlConsoleOptionWindow();
        _optionWindow.OpenCentered();
        _optionWindow.OnClose += () => _optionWindow = null;
        var name = Localize(option.Name);
        _optionWindow.Title = name;
        _optionWindow.CurrentPointsLabel.Text = Loc.GetString("rmc-ui-tech-points-value", ("value", points.Double().ToString("F1")));
        _optionWindow.NameLabel.Text = name;
        _optionWindow.DescriptionLabel.Text = Localize(option.Description);
        _optionWindow.CostLabel.Text = $"{option.CurrentCost}";

        _optionWindow.Statistics.DisposeAllChildren();
        var hasStats = false;

        if (option.Repurchasable)
        {
            hasStats = true;
            _optionWindow.Statistics.AddChild(new Label
            {
                Text = Loc.GetString("rmc-ui-tech-repurchasable")
            });
        }

        if (option.Increase != 0)
        {
            hasStats = true;
            _optionWindow.Statistics.AddChild(new Label
            {
                Text = Loc.GetString("rmc-ui-tech-incremental-price", ("increase", option.Increase)),
            });
        }

        _optionWindow.StatisticsContainer.Visible = hasStats;

        var canPurchase = points >= option.CurrentCost &&
                          currentTier >= tier &&
                          !option.Disabled &&
                          (!option.Purchased || option.Repurchasable) &&
                          option.TimeLock < _ticker.RoundDuration();

        _optionWindow.PurchaseButton.Text = Loc.GetString("rmc-ui-tech-purchase-button");
        _optionWindow.PurchaseButton.Disabled = !canPurchase;

        _optionWindow.PurchaseButton.OnPressed += _ =>
        {
            SendPredictedMessage(new TechPurchaseOptionBuiMsg(tier, optionIndex));
            _optionWindow.Close();
        };
    }

    private static string Localize(string text)
    {
        return Loc.TryGetString(text, out var localized) ? localized : text;
    }
}
