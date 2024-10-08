using System.Threading;
using System.Threading.Tasks;
using Content.Server._RMC14.NPC.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server._RMC14.NPC.HTN;

public sealed partial class LeapOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    [DataField("targetKey", required: true)]
    public string TargetKey = default!;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
    CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
            return (false, null);

        return (true, null);
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var leap = _entManager.EnsureComponent<NPCLeapComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
        leap.Target = blackboard.GetValue<EntityUid>(TargetKey);
    }

    public void ConditionalShutdown(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        _entManager.RemoveComponent<NPCLeapComponent>(owner);
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        base.Update(blackboard, frameTime);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        HTNOperatorStatus status;

        if (_entManager.TryGetComponent<NPCLeapComponent>(owner, out var leap) &&
            blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
        {
            leap.Target = target;

            // Success
            switch (leap.Status)
            {
                case LeapStatus.TargetUnreachable:
                case LeapStatus.NotInSight:
                case LeapStatus.TargetOutOfRange:
                case LeapStatus.TargetBadAngle:
                    status = HTNOperatorStatus.Failed;
                    break;
                case LeapStatus.Normal:
                    status = HTNOperatorStatus.Continuing;
                    break;
                case LeapStatus.Finished:
                    status = HTNOperatorStatus.Finished;
                    break;
                default:
                    status = HTNOperatorStatus.Failed;
                    break;
            }
        }
        else
        {
            status = HTNOperatorStatus.Failed;
        }

        // Mark it as finished to continue the plan.
        if (status == HTNOperatorStatus.Continuing && ShutdownState == HTNPlanState.PlanFinished)
        {
            status = HTNOperatorStatus.Finished;
        }

        return status;
    }
}
