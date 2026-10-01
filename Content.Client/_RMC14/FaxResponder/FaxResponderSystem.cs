using Content.Shared._RMC14.FaxResponder;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.FaxResponder;

public sealed class FaxResponderSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public List<FaxResponderJobStatus> Jobs { get; private set; } = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<FaxResponderStatusEvent>(OnStatusReceived);
    }

    private void OnStatusReceived(FaxResponderStatusEvent ev)
    {
        Jobs = ev.Jobs;
        var changedEv = new FaxResponderStatusChangedEvent(ev.Jobs);
        RaiseLocalEvent(changedEv);
    }

    public void RequestStatus()
    {
        if (_net.IsServer)
            return;

        RaiseNetworkEvent(new FaxResponderStatusRequest());
    }

    public void RequestJoin(ProtoId<JobPrototype> job)
    {
        if (_net.IsServer)
            return;

        RaiseNetworkEvent(new JoinFaxResponderRequest(job));
    }
}
