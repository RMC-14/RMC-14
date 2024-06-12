using System.Numerics;
using Content.Shared._CM14.Xenos.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Server._CM14.Xenos.Animations;

public sealed class PlayLungeAnimationSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public void PlayLungeAnimationEvent(EntityUid entityUid, Vector2 direction)
    {
        var ev = new PlayLungeAnimationEvent(_entityManager.GetNetEntity(entityUid), direction.Normalized());
        RaiseNetworkEvent(ev);
    }
}
