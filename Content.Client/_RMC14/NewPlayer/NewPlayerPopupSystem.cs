using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.NewPlayer;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.NewPlayer;

public sealed class NewPlayerPopupSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _players = default!;


    private NewToJobPopup? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<NewToJobEvent>(OnNewToJob);
    }

    private void OnNewToJob(NewToJobEvent ev)
    {
        if (_cfg.GetCVar(RMCCVars.RMCNewToJobPopup) == false)
            return;

        OpenNewPlayerPopup(GetEntity(ev.Mob), ev.JobInfo, ev.JobName);
    }

    private void OpenNewPlayerPopup(EntityUid mob, string? jobInfo, string jobName)
    {
        if (_window != null)
            return;

        if (jobInfo == null)
            return;

        if (_players.LocalEntity == null || mob != _players.LocalEntity)
            return;

        _window = new NewToJobPopup();
        if (!string.IsNullOrEmpty(jobInfo))
            _window.SetJobInfo(jobInfo, jobName);
        _window.OpenCentered();
        _window.OnClose += () => _window = null;
    }
}
