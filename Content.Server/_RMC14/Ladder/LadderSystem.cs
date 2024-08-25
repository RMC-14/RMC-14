using Content.Shared._RMC14.Ladder;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Ladder;

public sealed class LadderSystem : SharedLadderSystem
{
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    protected override void AddViewer(Entity<LadderComponent> ent, ICommonSession player)
    {
        base.AddViewer(ent, player);
        _viewSubscriber.AddViewSubscriber(ent, player);
    }

    protected override void RemoveViewer(Entity<LadderComponent> ent, ICommonSession player)
    {
        base.RemoveViewer(ent, player);
        _viewSubscriber.RemoveViewSubscriber(ent, player);
    }
}
