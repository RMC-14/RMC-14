using Content.Shared._RMC14.Teleporter;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Teleporter;

public sealed class RMCTeleporterSystem : SharedRMCTeleporterSystem
{
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    protected override void AddViewer(Entity<RMCTeleporterViewerComponent> viewer, ICommonSession player)
    {
        base.AddViewer(viewer, player);
        _viewSubscriber.AddViewSubscriber(viewer, player);
    }

    protected override void RemoveViewer(Entity<RMCTeleporterViewerComponent> viewer, ICommonSession player)
    {
        base.RemoveViewer(viewer, player);
        _viewSubscriber.RemoveViewSubscriber(viewer, player);
    }
}
