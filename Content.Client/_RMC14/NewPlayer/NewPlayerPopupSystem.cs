using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.NewPlayer;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.NewPlayer;

public sealed class NewPlayerPopupSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private NewToJobPopup? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<NewToJobEvent>(OnNewToJob);
    }

    private void OnNewToJob(NewToJobEvent ev)
    {
        OpenNewPlayerPopup();
    }

    private void OpenNewPlayerPopup()
    {
        if (_window != null)
            return;

        _window = new NewToJobPopup();
        _window.OpenCentered();
        _window.OnClose += () => _window = null;
    }
}
