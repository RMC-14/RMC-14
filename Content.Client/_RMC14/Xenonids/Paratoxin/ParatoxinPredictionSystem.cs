using Content.Shared._RMC14.Xenonids.Paratoxin;
using Content.Shared.GameTicking;
using Robust.Client.Timing;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Xenonids.Paratoxin;

// TODO RMC14 this system exists because of how predicted projectiles behave. Predicted projectile hits
// don't get re-applied during prediction. This is normally not a problem, because most projectile hits
// don't have effects that need to be predicted, but paratoxin is an exception because sentinel's abilities
// rely on the current paratoxin stacks. Not predicting them will cause the other abilities to mispredict.
//
// To resolve this, we have this client side system reapply paratoxin stacks for predicted ticks.
// If predicted projectiles are fixed to get re-applied during prediction, this system should be deleted entirely.
public sealed class ParatoxinPredictionSystem : EntitySystem
{
    [Dependency] private readonly ParatoxinSystem _paratoxinSystem = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;

    private Queue<(GameTick Tick, EntityUid Target, int Stacks)> _predictedParatoxinApplications = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<ParatoxinApplicationEvent>(HandleParatoxinApplicationEvent);
    }

    public void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _predictedParatoxinApplications.Clear();
    }

    private void HandleParatoxinApplicationEvent(ParatoxinApplicationEvent msg)
    {
        if (_timing.IsFirstTimePredicted)
        {
            _predictedParatoxinApplications.Enqueue((_timing.CurTick, msg.Target, msg.StacksToApply));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        while (_predictedParatoxinApplications.Count > 0
            && _predictedParatoxinApplications.Peek().Tick <= _timing.LastRealTick)
        {
            _predictedParatoxinApplications.Dequeue();
        }

        foreach (var (tick, target, stacks) in _predictedParatoxinApplications)
        {
            if (tick == _timing.CurTick)
            {
                _paratoxinSystem.TryChangeStacks(target, null, stacks);
            }
            else if (tick > _timing.CurTick)
            {
                // no more relevant applications this tick
                break;
            }
        }
    }
}
