using Content.Shared._RMC14.Movement;
using Content.Shared.Movement.Components;

namespace Content.Server._RMC14.Movement;
public sealed class LinearMoverController : SharedLinearMoverController
{
    private HashSet<EntityUid> _moverAdded = new();
    private List<Entity<LinearInputMoverComponent>> _movers = new();

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        _moverAdded.Clear();
        _movers.Clear();
        var inputQueryEnumerator = AllEntityQuery<LinearInputMoverComponent>();

        // Need to order mob movement so that movers don't run before their relays.
        while (inputQueryEnumerator.MoveNext(out var uid, out var mover))
        {
            InsertMover((uid, mover));
        }

        foreach (var mover in _movers)
        {
            HandleLinearMovement(mover, frameTime);
        }
    }

    private void InsertMover(Entity<LinearInputMoverComponent> source)
    {
        if (TryComp(source, out MovementRelayTargetComponent? relay))
        {
            if (TryComp(relay.Source, out LinearInputMoverComponent? relayMover))
            {
                InsertMover((relay.Source, relayMover));
            }
        }

        // Already added
        if (!_moverAdded.Add(source.Owner))
            return;

        _movers.Add(source);
    }
}
