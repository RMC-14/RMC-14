using Content.Shared.Paper;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Client._RMC14.Paper;

public sealed class BusinessCardBoundUserInterface : BoundUserInterface
{
    private BusinessCardWindow? _window;

    public BusinessCardBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BusinessCardWindow>();
        _window.OnSaved += OnSaved;

        if (EntMan.TryGetComponent<PaperComponent>(Owner, out var paper))
        {
            _window.MaxInputLength = paper.ContentSize;
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window?.Populate((PaperBoundUserInterfaceState)state);
    }

    private void OnSaved(string text)
    {
        SendMessage(new PaperInputTextMessage(text));
        _window?.ClearInput();
    }
}
