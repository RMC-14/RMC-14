using System.Linq;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.ScreenDeployAnnounce;
using Content.Shared._RMC14.Marines.ScreenAnnounce;
using Content.Shared.Coordinates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Marines.ScreenDeployAnnounce;

public sealed class SharedDeployAnnounceSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipLaunchedFromWarshipEvent>(OnDropshipLaunchedFromWarship);
    }

    private void OnDropshipLaunchedFromWarship(ref DropshipLaunchedFromWarshipEvent ev)
    {
        if (_net.IsClient)
            return;

        if (ev.Dropship.Comp.Destination is not { } destination)
            return;

        var coordinates = destination.ToCoordinates();
        if (!_area.TryGetArea(coordinates, out var lzArea, out _) ||
            string.IsNullOrWhiteSpace(lzArea.Value.Comp.LinkedLz))
        {
            return;
        }

        if (!TryComp<DeployAnnounceDropshipComponent>(ev.Dropship, out var deployComp))
            return;

        if (deployComp.Deployed)
            return;

        DeployAnnounce(ev.Dropship, deployComp);
    }

    public void DeployAnnounce(EntityUid dropship, DeployAnnounceDropshipComponent deployComp)
    {
        var map = _transform.GetMapId(dropship);
        var mapFilter = Filter.BroadcastMap(map);
        mapFilter.RemoveWhereAttachedEntity(ent => !HasComp<MarineComponent>(ent));

        var rawText = Loc.GetString(deployComp.AnnounceText);
        var text = rawText.Split('\n')
                          .Select(line => line.Trim())
                          .Where(line => !string.IsNullOrWhiteSpace(line))
                          .ToArray();

        var ev = new ScreenAnnounceMessage(text, ScreenAnnounceTarget.FirstDeploy, ScreenAnnounceArgs.Default, string.Empty, null);
        RaiseNetworkEvent(ev, mapFilter);

        if (deployComp.AfterAnnounceSound is not null)
            _audio.PlayGlobal(deployComp.AfterAnnounceSound, mapFilter, true, null);

        deployComp.Deployed = true;
    }
}
