using Content.Client.Audio;
using Content.Shared._RMC14.Xenonids.JoinXeno;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Xenonids.JoinXeno;

[UsedImplicitly]
public sealed class LarvaQueueOfferSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly SoundPathSpecifier OfferSound = new("/Audio/Effects/newplayerping.ogg");

    private LarvaQueueOfferWindow? _window;
    private LarvaQueueOfferEvent? _currentOffer;
    private string _offeredEntityName = string.Empty;
    private double _offerExpiredAt;

    public override void Initialize()
    {
        SubscribeNetworkEvent<LarvaQueueOfferEvent>(OnOfferReceived);
        SubscribeNetworkEvent<LarvaQueueOfferExpiredEvent>(OnOfferExpired);
    }

    private void OnOfferReceived(LarvaQueueOfferEvent ev)
    {
        _offerExpiredAt = 0;
        CloseWindow();
        _currentOffer = ev;
        _offeredEntityName = ev.TargetEntityName;

        _window = new LarvaQueueOfferWindow();

        _window.OfferTypeLabel.Text = ev.OfferType switch
        {
            "Burst Victim" => Loc.GetString("rmc-larva-queue-offer-burst-victim"),
            "Infector"     => Loc.GetString("rmc-larva-queue-offer-infector"),
            _              => Loc.GetString("rmc-larva-queue-offer-available"),
        };

        _window.PositionLabel.Text = Loc.GetString("rmc-larva-queue-offer-position", ("position", ev.QueuePosition));
        _window.HiveLabel.Text = Loc.GetString("rmc-larva-queue-offer-hive", ("hive", ev.HiveName));
        UpdateCountdown();

        _window.AcceptButton.OnPressed += _ =>
        {
            RaiseNetworkEvent(new LarvaQueueAcceptOfferEvent());
            CloseWindow();
        };

        _window.DeclineButton.OnPressed += _ =>
        {
            RaiseNetworkEvent(new LarvaQueueDeclineOfferEvent());
            CloseWindow();
        };

        _window.FollowButton.Visible = ev.TargetEntity != null;
        _window.FollowButton.OnPressed += _ => RaiseNetworkEvent(new LarvaQueueFollowTargetEvent());

        _window.OnClose += () =>
        {
            if (_currentOffer != null)
            {
                RaiseNetworkEvent(new LarvaQueueDeclineOfferEvent());
                _currentOffer = null;
            }
        };

        _window.OpenCentered();
        _audio.PlayGlobal(OfferSound, Filter.Local(), false);
    }

    private void OnOfferExpired(LarvaQueueOfferExpiredEvent ev)
    {
        _currentOffer = null;

        if (_window == null || !_window.IsOpen)
            return;

        if (ev.LarvaDied)
        {
            _offerExpiredAt = _timing.CurTime.TotalSeconds;
            _window.CountdownLabel.Text = Loc.GetString("rmc-larva-queue-offer-larva-died", ("larva", _offeredEntityName));
            _window.AcceptButton.Visible = false;
            _window.DeclineButton.Visible = false;
            _window.FollowButton.Visible = false;
        }
        else
        {
            CloseWindow();
        }
    }

    public override void Update(float frameTime)
    {
        if (_currentOffer != null && _window != null && _window.IsOpen)
            UpdateCountdown();

        if (_offerExpiredAt > 0)
        {
            if (_window == null || !_window.IsOpen)
            {
                _offerExpiredAt = 0;
            }
            else if (_timing.CurTime.TotalSeconds - _offerExpiredAt > 5.0)
            {
                _offerExpiredAt = 0;
                CloseWindow();
            }
        }
    }

    private void UpdateCountdown()
    {
        if (_currentOffer == null || _window == null)
            return;

        var remaining = _currentOffer.ExpiresAt - _timing.CurTime.TotalSeconds;
        if (remaining <= 0)
        {
            _window.CountdownLabel.Text = Loc.GetString("rmc-larva-queue-offer-expired");
            return;
        }

        _window.CountdownLabel.Text = Loc.GetString("rmc-larva-queue-offer-time-remaining",
            ("seconds", (int) Math.Round(remaining, MidpointRounding.AwayFromZero)));
    }

    private void CloseWindow()
    {
        if (_window == null)
            return;

        _offerExpiredAt = 0;
        _currentOffer = null;
        _offeredEntityName = string.Empty;
        _window.Close();
        _window = null;
    }
}
