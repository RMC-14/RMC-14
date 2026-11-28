using Content.Client._RMC14.UserInterface;
using Content.Client.Eui;
using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Marines.Mutiny;

[UsedImplicitly]
public sealed class MutinySideEui : BaseEui
{
    private readonly ConfirmationWindow _window = new();
    private bool _handled;

    public MutinySideEui()
    {
        _window.Setup(
            Loc.GetString("rmc-mutiny-side-title"),
            Loc.GetString("rmc-mutiny-side-text"),
            Loc.GetString("rmc-mutiny-side-mutineer"),
            Loc.GetString("rmc-mutiny-side-refuse"),
            Loc.GetString("rmc-mutiny-side-loyalist"));

        _window.AcceptButton.OnPressed += _ => SendOnce(MutinySide.Mutineer);
        _window.ExtraButton.OnPressed += _ => SendOnce(MutinySide.Loyalist);
        _window.DenyButton.OnPressed += _ => SendOnce(MutinySide.NonCombatant);
        _window.OnClose += () => SendOnce(MutinySide.NonCombatant, closeWindow: false);
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is MutinySideEuiState mutiny)
            _window.AcceptButton.Visible = mutiny.CanJoinMutineers;
    }

    public override void Closed()
    {
        _handled = true;
        _window.Close();
    }

    private void SendOnce(MutinySide side, bool closeWindow = true)
    {
        if (_handled)
            return;

        _handled = true;
        SendMessage(new MutinySideChoiceMessage(side));
        if (closeWindow)
            _window.Close();
    }
}
