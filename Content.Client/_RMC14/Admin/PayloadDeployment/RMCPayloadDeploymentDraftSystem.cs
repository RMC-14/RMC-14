using System.Linq;
using Content.Shared._RMC14.Admin.PayloadDeployment;
using Content.Shared.GameTicking;
using Robust.Shared.Network;

namespace Content.Client._RMC14.Admin.PayloadDeployment;

public sealed class RMCPayloadDeploymentDraftSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    private readonly List<RMCPayloadDeploymentManifest> _manifests = [];
    private SharedGameTicker _ticker = default!;
    private RMCPayloadDeliveryType _deliveryType;
    private int _activeManifest;
    private int _roundId;
    private bool _canSave;

    public override void Initialize()
    {
        base.Initialize();
        _ticker = EntityManager.System<SharedGameTicker>();

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        _net.Disconnect += OnDisconnect;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _net.Disconnect -= OnDisconnect;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        Clear();
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs args)
    {
        foreach (var manifest in _manifests)
        {
            manifest.Entities.Clear();
        }

        _canSave = false;
    }

    public bool TryRestore(out RMCPayloadDeliveryType deliveryType, out int activeManifest, out List<RMCPayloadDeploymentManifest> manifests)
    {
        if (_roundId != _ticker.RoundId || _manifests.Count == 0)
        {
            Clear();
            _canSave = true;
            deliveryType = default;
            activeManifest = 0;
            manifests = [];
            return false;
        }

        _canSave = true;
        deliveryType = _deliveryType;
        activeManifest = Math.Clamp(_activeManifest, 0, _manifests.Count - 1);
        manifests = _manifests.Select(manifest => manifest.Clone()).ToList();
        return true;
    }

    public void Save(RMCPayloadDeliveryType deliveryType, int activeManifest, IReadOnlyList<RMCPayloadDeploymentManifest> manifests)
    {
        if (!_canSave || !_net.IsConnected)
            return;

        _deliveryType = deliveryType;
        _activeManifest = activeManifest;
        _roundId = _ticker.RoundId;
        _manifests.Clear();
        _manifests.AddRange(manifests.Select(manifest => manifest.Clone()));
    }

    private void Clear()
    {
        _manifests.Clear();
        _roundId = 0;
        _canSave = false;
    }
}
