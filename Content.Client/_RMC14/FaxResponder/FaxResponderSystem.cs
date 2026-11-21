using Content.Shared._RMC14.FaxResponder;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.FaxResponder;

public sealed class FaxResponderSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public List<FaxResponderJobStatus> Jobs { get; private set; } = new();

    private FaxResponderRulesWindow? _rulesWindow;

    public override void Initialize()
    {
        SubscribeNetworkEvent<FaxResponderStatusEvent>(OnStatusReceived);
        SubscribeNetworkEvent<FaxResponderRulesEvent>(OnRulesReceived);
    }

    private void OnStatusReceived(FaxResponderStatusEvent ev)
    {
        Jobs = ev.Jobs;
        var changedEv = new FaxResponderStatusChangedEvent(ev.Jobs);
        RaiseLocalEvent(changedEv);
    }

    private void OnRulesReceived(FaxResponderRulesEvent ev)
    {
        if (_rulesWindow is { IsOpen: true })
            return;

        _rulesWindow = new FaxResponderRulesWindow();
        _rulesWindow.OnClose += () => _rulesWindow = null;
        _rulesWindow.OpenCentered();
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
