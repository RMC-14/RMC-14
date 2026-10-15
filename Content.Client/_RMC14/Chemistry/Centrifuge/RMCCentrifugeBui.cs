using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Content.Client.Resources;
using Content.Shared._RMC14.Chemistry.Centrifuge;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Chemistry.Centrifuge;

[UsedImplicitly]
public sealed class RMCCentrifugeBui : BoundUserInterface, IRefreshableBui
{
    private const string CycleIcon = "/Textures/_RMC14/Interface/Icons/dark/arrows-rotate.svg.192dpi.png";
    private const string CycleIconWhite = "/Textures/_RMC14/Interface/Icons/white/arrows-rotate.svg.192dpi.png";
    private const string FlaskIcon = "/Textures/_RMC14/Interface/Icons/dark/flask.svg.192dpi.png";
    private const string FlaskIconWhite = "/Textures/_RMC14/Interface/Icons/white/flask.svg.192dpi.png";
    private const string LinkIcon = "/Textures/_RMC14/Interface/Icons/dark/link.svg.192dpi.png";
    private const string LinkIconWhite = "/Textures/_RMC14/Interface/Icons/white/link.svg.192dpi.png";
    private const string EjectIcon = "/Textures/_RMC14/Interface/Icons/dark/eject.svg.192dpi.png";
    private const string EjectIconWhite = "/Textures/_RMC14/Interface/Icons/white/eject.svg.192dpi.png";

    private static readonly Color HeaderColor = Color.FromHex("#12141A");
    private static readonly Color TanColor = Color.FromHex("#ffb950");
    private static readonly Color BlueColor = Color.FromHex("#3A7CC4");
    private static readonly Color RedColor = Color.FromHex("#8B2020");
    private static readonly Color GreyButtonFill = Color.FromHex("#bfbfbf");

    private readonly ContainerSystem _container;
    private readonly IResourceCache _resourceCache;
    private readonly Font _boldItalicFont;

    private RMCCentrifugeWindow? _window;
    private Label? _modeLabel;
    private Label? _sourceLabel;

    public RMCCentrifugeBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _container = EntMan.System<ContainerSystem>();
        _resourceCache = IoCManager.Resolve<IResourceCache>();
        _boldItalicFont = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-BoldItalic.ttf", 12);
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCCentrifugeWindow>();

        Header(_window.ControlsHeader);
        Header(_window.SettingsHeader);

        Toggle(_window.ModeButton, TanColor);
        _modeLabel = Icon(_window.ModeButton, CycleIcon, CycleIconWhite, "Mode: split");

        Toggle(_window.SourceButton, BlueColor);
        _sourceLabel = Icon(_window.SourceButton, FlaskIcon, FlaskIconWhite, "Input: Container");

        Toggle(_window.ConnectButton, BlueColor);
        Icon(_window.ConnectButton, LinkIcon, LinkIconWhite, "Connect a Turing Dispenser");

        GreyButton(_window.EjectInputButton);
        Icon(_window.EjectInputButton, EjectIcon, EjectIconWhite, "Eject");
        GreyButton(_window.EjectOutputButton);
        Icon(_window.EjectOutputButton, EjectIcon, EjectIconWhite, "Eject");

        _window.ModeButton.OnPressed += _ => SendPredictedMessage(new RMCCentrifugeToggleModeBuiMsg());
        _window.SourceButton.OnPressed += _ => SendPredictedMessage(new RMCCentrifugeToggleSourceBuiMsg());
        _window.ConnectButton.OnPressed += _ => SendPredictedMessage(new RMCCentrifugeAttemptConnectionBuiMsg());
        _window.EjectInputButton.OnPressed += _ => SendPredictedMessage(new RMCCentrifugeEjectInputBuiMsg());
        _window.EjectOutputButton.OnPressed += _ => SendPredictedMessage(new RMCCentrifugeEjectOutputBuiMsg());
        _window.LabelEdit.OnTextEntered += args => SendPredictedMessage(new RMCCentrifugeSetLabelBuiMsg(args.Text));

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCCentrifugeComponent? comp))
            return;

        if (_modeLabel != null)
            _modeLabel.Text = comp.Mode == CentrifugeMode.Split ? "Mode: split" : "Mode: distribute";

        var connected = comp.TuringDispenser != null;
        if (_sourceLabel != null)
        {
            _sourceLabel.Text = comp.InputSource == CentrifugeInputSource.Turing
                ? "Input: Turing Dispenser"
                : "Input: Container";
        }

        _window.SourceButton.Disabled = !connected;

        _window.ConnectButton.Visible = !connected;

        var connectionColor = connected ? BlueColor : RedColor;
        _window.ConnectionLabel.Text = connected ? "Turing Dispenser connected!" : "No Turing Dispenser connected!";
        _window.ConnectionLabel.FontColorOverride = Color.White;
        _window.ConnectionLabel.FontOverride = _boldItalicFont;
        _window.ConnectionPanel.PanelOverride = Flat(connectionColor);

        if (comp.Label is { } label)
            _window.LabelEdit.Text = label;

        var hasInput = _container.TryGetContainer(Owner, comp.InputSlotId, out var inputContainer) &&
                       inputContainer.ContainedEntities.Count > 0;
        _window.InputStatusLabel.Text = hasInput ? "Loaded" : "Empty";
        _window.EjectInputButton.Disabled = !hasInput || comp.Spinning;

        var hasOutput = _container.TryGetContainer(Owner, comp.OutputBoxSlotId, out var outputContainer) &&
                        outputContainer.ContainedEntities.Count > 0;
        _window.OutputStatusLabel.Text = hasOutput ? "Loaded" : "Empty";
        _window.EjectOutputButton.Disabled = !hasOutput || comp.Spinning;
    }

    private static void Header(PanelContainer panel)
    {
        panel.PanelOverride = Flat(HeaderColor);
    }

    private static void GreyButton(Button button)
    {
        button.StyleBoxOverride = Flat(GreyButtonFill);
        button.ModulateSelfOverride = Color.White;
    }

    private static void Toggle(Button button, Color color)
    {
        button.StyleBoxOverride = Flat(color);
        button.ModulateSelfOverride = Color.White;
    }

    private static StyleBoxFlat Flat(Color color)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = color,
            ContentMarginLeftOverride = 6,
            ContentMarginRightOverride = 6,
            ContentMarginTopOverride = 1,
            ContentMarginBottomOverride = 1,
        };
    }

    private static Label Icon(Button button, string texturePath, string whiteTexturePath, string text)
    {
        button.Text = null;

        var row = new RMCIconLabelRow
        {
            HorizontalAlignment = Control.HAlignment.Left,
        };
        row.Icon.SetSize = new Vector2(16, 16);
        row.SetIcon(texturePath, whiteTexturePath);
        row.Label.Text = text;

        button.AddChild(row);

        button.OnMouseEntered += _ => row.SetHovered(true);
        button.OnMouseExited += _ => row.SetHovered(false);

        return row.Label;
    }
}
