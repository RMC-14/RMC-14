using Content.Shared._RMC14.Marines;
using Content.Shared.CrewManifest;
using Content.Shared.Station.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.CrewManifest;

public sealed class RMCCrewManifestSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MarineComponent, OpenCrewManifestAlertEvent>(OnUserOpenAlert);
    }

    private void OnUserOpenAlert(Entity<MarineComponent> ent, ref OpenCrewManifestAlertEvent args)
    {
        if (args.Handled)
            return;

        var stations = EntityQueryEnumerator<StationMemberComponent>();
        while (stations.MoveNext(out var stationId, out var stationComp))
        {
            if (!HasComp<AlmayerComponent>(stationId))
                continue;

            args.Handled = true;

            var stationNetEntity = GetNetEntity(stationComp.Station);

            if (_net.IsClient && ent.Owner == _playerManager.LocalEntity)
                RaiseNetworkEvent(new RequestCrewManifestMessage(stationNetEntity));
        }
    }
}
