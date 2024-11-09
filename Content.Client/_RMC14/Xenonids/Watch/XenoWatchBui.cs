using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Watch;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client._RMC14.Xenonids.Watch;

[UsedImplicitly]
public sealed class XenoWatchBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [ViewVariables]
    private XenoWatchWindow? _window;

    private readonly SpriteSystem _sprite;

    public XenoWatchBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        EnsureWindow();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not XenoWatchBuiState s)
            return;

        _window = EnsureWindow();
        _window.BurrowedLarvaLabel.Text = $"Burrowed Larva: {s.BurrowedLarva}";
        _window.XenoContainer.DisposeAllChildren();

        foreach (var xeno in s.Xenos)
        {
            Texture? texture = null;
            if (xeno.Id != null &&
                _prototype.TryIndex(xeno.Id.Value, out var evolution))
            {
                texture = _sprite.Frame0(evolution);
            }

            var control = new XenoChoiceControl();
            control.Set(xeno.Name, texture);
            control.Button.OnPressed += _ => SendPredictedMessage(new XenoWatchBuiMsg(xeno.Entity));

            _window.XenoContainer.AddChild(control);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }

    private XenoWatchWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        _window = new XenoWatchWindow();
        _window.OnClose += Close;
        _window.SearchBar.OnTextChanged += OnSearchBarChanged;
        _window.OpenCentered();
        return _window;
    }

    private void OnSearchBarChanged(LineEditEventArgs args)
    {
        if (_window is not { Disposed: false })
            return;

        foreach (var child in _window.XenoContainer.Children)
        {
            if (child is not XenoChoiceControl control)
                continue;

            if (string.IsNullOrWhiteSpace(args.Text))
                control.Visible = true;
            else
                control.Visible = control.NameLabel.GetMessage()?.Contains(args.Text, StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
}
