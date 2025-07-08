using Content.Client.Message;
using Content.Shared._RMC14.Mobs;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Mobs;

[UsedImplicitly]
public sealed class CMGhostActionBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private CMGhostActionWindow? _window;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<CMGhostActionWindow>();
        _window.Text.SetMarkupPermissive(Loc.GetString("cm-ghost-window-text"));
        _window.Stay.OnPressed += _ => Close();
        _window.Ghost.OnPressed += _ => SendPredictedMessage(new CMGhostActionBuiMsg());
    }
}
