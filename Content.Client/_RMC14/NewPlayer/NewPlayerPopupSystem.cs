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
        SubscribeLocalEvent<NewToJobEvent>(OnNewToJob);
    }

    private void OnNewToJob(ref NewToJobEvent ev)
    {
        if (_cfg.GetCVar(RMCCVars.RMCNewToJobPopup) == false)
            return;

        OpenNewPlayerPopup(ev.jobInfo, ev.jobName);
    }

    private void OpenNewPlayerPopup(string? jobInfo, string jobName)
    {
        if (_window != null)
            return;

        if (jobInfo == null)
            return;

        _window = new NewToJobPopup();
        if (!string.IsNullOrEmpty(jobInfo))
            _window.SetJobInfo(jobInfo, jobName);
        _window.OpenCentered();
        _window.OnClose += () => _window = null;
    }
}
