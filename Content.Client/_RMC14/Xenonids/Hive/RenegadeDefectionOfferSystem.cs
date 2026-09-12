using Content.Shared._RMC14.Xenonids.Hive;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Xenonids.Hive;

[UsedImplicitly]
public sealed class RenegadeDefectionOfferSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private RenegadeDefectionOfferWindow? _window;
    private NetEntity? _currentXeno;
    private double _expiresAt;

    public override void Initialize()
    {
        SubscribeNetworkEvent<RenegadeDefectionOfferEvent>(OnOfferReceived);
        SubscribeNetworkEvent<RenegadeDefectionOfferExpiredEvent>(OnOfferExpired);
    }

    private void OnOfferReceived(RenegadeDefectionOfferEvent ev)
    {
        CloseWindow();
        _currentXeno = ev.Xeno;
        _expiresAt = ev.ExpiresAt;

        _window = new RenegadeDefectionOfferWindow();
        _window.MessageLabel.Text = Loc.GetString("rmc-xeno-renegade-defect-message", ("faction", ev.Faction));
        UpdateCountdown();

        _window.DefectButton.OnPressed += _ =>
        {
            SendChoice(true);
            CloseWindow();
        };

        _window.StayButton.OnPressed += _ =>
        {
            SendChoice(false);
            CloseWindow();
        };

        _window.OnClose += () =>
        {
            if (_currentXeno != null)
            {
                SendChoice(false);
                _currentXeno = null;
            }
        };

        _window.OpenCentered();
    }

    private void OnOfferExpired(RenegadeDefectionOfferExpiredEvent ev)
    {
        if (_currentXeno != ev.Xeno)
            return;

        _currentXeno = null;
        CloseWindow();
    }

    public override void Update(float frameTime)
    {
        if (_currentXeno != null && _window is { IsOpen: true })
            UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        if (_window == null)
            return;

        var remaining = _expiresAt - _timing.CurTime.TotalSeconds;
        if (remaining <= 0)
        {
            _window.CountdownLabel.Text = Loc.GetString("rmc-xeno-renegade-defect-timed-out");
            return;
        }

        _window.CountdownLabel.Text = Loc.GetString("rmc-dialog-countdown", ("seconds", (int) remaining));
    }

    private void SendChoice(bool defect)
    {
        if (_currentXeno is not { } xeno)
            return;

        RaiseNetworkEvent(new RenegadeDefectionChoiceEvent
        {
            Xeno = xeno,
            Defect = defect,
        });
    }

    private void CloseWindow()
    {
        if (_window == null)
            return;

        _currentXeno = null;
        _window.Close();
        _window = null;
    }
}
