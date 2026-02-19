using Content.Shared._RMC14.Attachable.Events;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Attachable;

public sealed class AttachableIFFDebugSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _players = default!;

    private readonly HashSet<ICommonSession> _observers = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableIFFDebugSampleEvent>(OnDebugSample);
    }

    public bool ToggleObserver(ICommonSession observer)
    {
        if (_observers.Remove(observer))
        {
            RaiseNetworkEvent(new AttachableIFFDebugToggledEvent(false), observer.Channel);
            return false;
        }

        _observers.Add(observer);
        RaiseNetworkEvent(new AttachableIFFDebugToggledEvent(true), observer.Channel);
        return true;
    }

    private void OnDebugSample(ref AttachableIFFDebugSampleEvent args)
    {
        if (!args.IsServerSample)
            return;

        if (!_players.TryGetSessionByEntity(args.User, out var session))
            return;

        if (!_observers.Contains(session))
            return;

        RaiseNetworkEvent(
            new AttachableIFFServerDebugSampleEvent(
                args.From,
                args.To,
                args.HasHit,
                args.Hit,
                args.BlockedByFriendly),
            session.Channel);
    }
}
