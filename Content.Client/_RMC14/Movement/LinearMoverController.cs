using Content.Shared._RMC14.Movement;
using Robust.Client.Player;

namespace Content.Client._RMC14.Movement;
public sealed class LinearMoverController : SharedLinearMoverController
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        if (_playerManager.LocalEntity is not { Valid: true } player)
            return;

        if (RelayQuery.TryGetComponent(player, out var relayMover))
            HandleClientsideMovement(relayMover.RelayEntity, frameTime);

        HandleClientsideMovement(player, frameTime);
    }

    private void HandleClientsideMovement(EntityUid player, float frameTime)
    {
        if (!MoverQuery.TryGetComponent(player, out var mover))
        {
            return;
        }

        // Server-side should just be handled on its own so we'll just do this shizznit
        HandleLinearMovement((player, mover), frameTime);
    }
}
