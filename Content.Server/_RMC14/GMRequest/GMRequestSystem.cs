using Content.Server.GameTicking.Events;

namespace Content.Server._RMC14.GMRequest;

/// <summary>
/// Handles system events the GM Request Manager needs to know about.
/// </summary>
public sealed class GMRequestSystem : EntitySystem
{
    [Dependency] private readonly GMRequestManager _manager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(_ => _manager.ClearGMRequests());
    }
}
