using System.Numerics;
using Content.Shared._RMC14.PetNaming;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._RMC14.PetNaming;

public sealed class PetNamingBui : BoundUserInterface
{
    private PetNamingWindow? _window;

    public PetNamingBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PetNamingWindow>();
        _window.OnSubmit += name => SendMessage(new PetNamingSetNameBuiMsg(name));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is PetNamingBuiState s)
            _window?.Update(s);
    }
}

public sealed class PetNamingWindow : DefaultWindow
{
    private readonly LineEdit _input;
    private int _maxLength = 32;

    public event Action<string>? OnSubmit;

    public PetNamingWindow()
    {
        Title = Loc.GetString("rmc-name-pet-window-title");
        MinSize = new Vector2(300, 100);

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(8),
            SeparationOverride = 6,
        };

        _input = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("rmc-name-pet-placeholder"),
            IsValid = text => text.Length <= _maxLength,
        };
        _input.OnTextEntered += _ => Submit();

        var confirm = new Button { Text = Loc.GetString("rmc-name-pet-confirm") };
        confirm.OnPressed += _ => Submit();

        box.AddChild(_input);
        box.AddChild(confirm);
        ContentsContainer.AddChild(box);
    }

    public void Update(PetNamingBuiState state)
    {
        _maxLength = state.MaxLength;
        _input.Text = state.CurrentName;
        _input.GrabKeyboardFocus();
        _input.CursorPosition = _input.Text.Length;
    }

    private void Submit()
    {
        var text = _input.Text.Trim();
        if (text.Length == 0 || text.Length > _maxLength)
            return;

        OnSubmit?.Invoke(text);
    }
}