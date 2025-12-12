using Content.Shared._RMC14.Hands;
using Content.Server.Hands.Systems;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Hands;

public sealed class ServerRMCHandsSystem : RMCHandsSystem
{
    [Dependency] private readonly HandsSystem _hands = default!;

    public override void ThrowHeldItem(EntityUid player, EntityCoordinates coordinates, float minDistance = 0.1f)
    {
        _hands.ThrowHeldItem(player, coordinates, minDistance);
    }
}
