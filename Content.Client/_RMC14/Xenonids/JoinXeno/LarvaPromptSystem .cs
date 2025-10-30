using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._RMC14.Xenonids.JoinXeno;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Xenonids.JoinXeno;

public sealed class LarvaPromptSystem : SharedJoinXenoSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private LarvaPromptWindow? _promptWindow;
    private NetEntity? _currentPromptLarva;
    private bool _isClosingDueToTimeout;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);
    }

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent ev)
    {
        ClosePrompt();
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        ClosePrompt();
    }

    protected override void OnLarvaPrompt(LarvaPromptEvent ev)
    {
        if (_promptWindow != null)
        {
            ClosePrompt();
        }

        _currentPromptLarva = ev.Larva;
        _isClosingDueToTimeout = false;

        var timeoutDuration = ev.TimeoutAt - _timing.CurTime;
        if (timeoutDuration <= TimeSpan.Zero)
        {
            return;
        }

        _promptWindow = new LarvaPromptWindow(timeoutDuration);
        _promptWindow.AcceptButtonPressed += OnAcceptPressed;
        _promptWindow.DeclineButtonPressed += OnDeclinePressed;
        _promptWindow.OnClose += OnPromptClosed;

        _audio.PlayGlobal("/Audio/_RMC14/Xeno/alien_distantroar_3.ogg", Filter.Local(), false);
    }

    protected override void OnLarvaPromptCancelled(LarvaPromptCancelledEvent ev)
    {
        if (_currentPromptLarva == ev.Larva)
        {
            ClosePrompt();
        }
    }

    private void OnAcceptPressed()
    {
        if (_currentPromptLarva != null)
        {
            AcceptLarvaPrompt(_currentPromptLarva.Value);
        }
        ClosePrompt();
    }

    private void OnDeclinePressed()
    {
        if (_currentPromptLarva != null)
        {
            DeclineLarvaPrompt(_currentPromptLarva.Value);
        }
        ClosePrompt();
    }

    private void OnPromptClosed()
    {
        if (_currentPromptLarva != null &&
            !_isClosingDueToTimeout &&
            (_promptWindow == null || !_promptWindow.TimedOut))
        {
            DeclineLarvaPrompt(_currentPromptLarva.Value);
        }
        ClosePrompt();
    }

    private void ClosePrompt()
    {
        if (_promptWindow != null)
        {
            _promptWindow.AcceptButtonPressed -= OnAcceptPressed;
            _promptWindow.DeclineButtonPressed -= OnDeclinePressed;
            _promptWindow.OnClose -= OnPromptClosed;
            _promptWindow.Close();
            _promptWindow = null;
        }
        _currentPromptLarva = null;
        _isClosingDueToTimeout = false;
    }
}
