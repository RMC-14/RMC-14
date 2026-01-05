using Content.Shared._RMC14.Marines;
using Content.Shared.CrewManifest;
using Content.Shared.Station.Components;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.CrewManifest;

public sealed class RMCCrewManifestSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MarineComponent, OpenCrewManifestAlertEvent>(OnUserOpenAlert);
    }

    private void OnUserOpenAlert(Entity<MarineComponent> ent, ref OpenCrewManifestAlertEvent args)
    {
        var stations = EntityQueryEnumerator<StationMemberComponent>();
        while (stations.MoveNext(out var stationId, out var stationComp))
        {
            if (!HasComp<AlmayerComponent>(stationId))
                continue;

            var stationNetEntity = GetNetEntity(stationComp.Station);

            if (_net.IsClient)
                RaiseNetworkEvent(new RequestCrewManifestMessage(stationNetEntity));
        }
    }
}
