using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Intel.Tech;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
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
            for (var j = 0; j < options.Count; j++)
            {
                var option = options[j];
                var optionControl = new Control();
                var texture = option.Icon.DirFrame0().TextureFor(Direction.South);
                optionControl.AddChild(new TextureButton
                {
                    TextureNormal = texture,
                    SetSize = new Vector2(64, 64),
                });

                var overlay = option.Purchased ? console.UnlockedRsi : console.LockedRsi;
                var optionButton = new TextureButton
                {
                    TextureNormal = overlay.DirFrame0().TextureFor(Direction.South),
                    Scale = new Vector2(2, 2),
                };
                optionControl.AddChild(optionButton);

                var tier = i;
                var optionIndex = j;
                optionButton.OnPressed += _ =>
                {
                    OpenOptionWindow(option, tier, optionIndex, console.Tree.Points, console.Tree.Tier);
                };
                optionButton.ToolTip = option.Name;
                optionButton.TooltipDelay = 0.1f;

                optionContainer.AddChild(new Control { HorizontalExpand = true });
                optionContainer.AddChild(optionControl);

                if (j == options.Count - 1)
                    optionContainer.AddChild(new Control { HorizontalExpand = true });
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
        _optionWindow.Title = option.Name;
        _optionWindow.CurrentPointsLabel.Text = Loc.GetString("rmc-ui-tech-points-value", ("value", points.Double().ToString("F1")));
        _optionWindow.NameLabel.Text = option.Name;
        _optionWindow.DescriptionLabel.Text = option.Description;
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
                          (!option.Purchased || option.Repurchasable) &&
                          option.TimeLock  < _ticker.RoundDuration();

        _optionWindow.PurchaseButton.Text = Loc.GetString("rmc-ui-tech-purchase-button");
        _optionWindow.PurchaseButton.Disabled = !canPurchase;

        _optionWindow.PurchaseButton.OnPressed += _ =>
        {
            SendPredictedMessage(new TechPurchaseOptionBuiMsg(tier, optionIndex));
            _optionWindow.Close();
        };
    }
}
