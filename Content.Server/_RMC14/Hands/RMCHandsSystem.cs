using Content.Server.Hands.Systems;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Hands;

public sealed class ServerRMCHandsSystem : RMCHandsSystem
{
    [Dependency] private readonly HandsSystem _hands = default!;

    public override bool ThrowHeldItem(EntityUid player, EntityCoordinates coordinates, float minDistance = 0.1f)
    {
        return _hands.ThrowHeldItem(player, coordinates, minDistance);
    }
}
