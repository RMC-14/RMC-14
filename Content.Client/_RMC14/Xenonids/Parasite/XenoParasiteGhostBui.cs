using Content.Shared._RMC14.Xenonids.Egg;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Xenonids.Parasite;

[UsedImplicitly]
public sealed class XenoParasiteGhostBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private XenoParasiteGhostWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<XenoParasiteGhostWindow>();
        _window.DenyButton.OnPressed += _ => _window.Close();
        _window.ConfirmButton.OnPressed += _ => SendPredictedMessage(new XenoParasiteGhostBuiMsg());
    }
}
