using Content.Server.DoAfter;
using Content.Server.Interaction;
using Content.Server._RMC14.NPC.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.NPC.Systems;

public sealed partial class NPCLeapSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DoAfterSystem _doafter = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NPCLeapComponent, ComponentShutdown>(OnShutdown);
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    private void OnShutdown(Entity<NPCLeapComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.CurrentDoAfter != null && TryComp<DoAfterComponent>(ent, out var after))
        {
            _doafter.Cancel(after.DoAfters[ent.Comp.CurrentDoAfter.Value].Id);
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<NPCLeapComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Status == LeapStatus.Unspecified || comp.Status == LeapStatus.Finished)
                continue;

            if (!_xformQuery.TryGetComponent(comp.Target, out var targetXform))
            {
                comp.Status = LeapStatus.TargetUnreachable;
                continue;
            }

            if (targetXform.MapID != xform.MapID)
            {
                comp.Status = LeapStatus.TargetUnreachable;
                continue;
            }

            if (!TryComp<DoAfterComponent>(uid, out var after))
            {
                comp.Status = LeapStatus.Unspecified;
                continue;
            }

            if (comp.CurrentDoAfter != null)
            {
                var status = _doafter.GetStatus(uid, comp.CurrentDoAfter.Value, after);
                comp.Status = status switch
                {
                    DoAfterStatus.Running => LeapStatus.Normal,
                    DoAfterStatus.Finished => LeapStatus.Finished,
                    _ => LeapStatus.Unspecified
                };

                if (!(comp.Status == LeapStatus.Normal))
                {
                    comp.CurrentDoAfter = null;
                    continue;
                }

                var worldPos = _transform.GetMoverCoordinates(uid);
                var targetPos = _transform.GetMoverCoordinates(comp.Target);

                var destinationPos = comp.Destination;
;
                if (!worldPos.TryDistance(EntityManager, targetPos, out var range))
                {
                    comp.Status = LeapStatus.Unspecified;
                    continue;
                }

                if (!_interaction.InRangeUnobstructed(uid, comp.Target, range, comp.Mask))
                {
                    _doafter.Cancel(after.DoAfters[comp.CurrentDoAfter.Value].Id);
                    comp.CurrentDoAfter = null;
                    comp.Status = LeapStatus.TargetOutOfRange;
                    continue;
                }

                var targetDir = (targetPos.Position - worldPos.Position).Normalized();
                var destDir = (destinationPos.Position - worldPos.Position).Normalized();
                var angle = Angle.ShortestDistance(new Angle(targetDir), new Angle(destDir));

                if (angle > Angle.FromDegrees(comp.MaxAngleDegrees))
                {
                    _doafter.Cancel(after.DoAfters[comp.CurrentDoAfter.Value].Id);
                    comp.CurrentDoAfter = null;
                    comp.Status = LeapStatus.TargetBadAngle;
                    continue;
                }

                // Nothing here if it gets this far
            }
            else
            {
                if (!TryComp<XenoComponent>(uid, out var xeno) || !TryComp<WorldTargetActionComponent>(xeno.Actions[comp.ActionId], out var action))
                {
                    comp.Status = LeapStatus.Unspecified;
                    continue;
                }

                if (!_actions.ValidAction(action))
                {
                    comp.Status = LeapStatus.Unspecified;
                    continue;
                }

                var worldPos = _transform.GetMoverCoordinates(uid);
                var targetPos = _transform.GetMoverCoordinates(comp.Target);

                var addedDis = (targetPos.Position - worldPos.Position).Normalized() * comp.LeapDistance;

                var destination = worldPos.WithPosition(worldPos.Position + addedDis);

                comp.Destination = destination;

                if (action.Event != null)
                {
                    action.Event.Performer = uid;
                    action.Event.Action = xeno.Actions[comp.ActionId];
                    action.Event.Target = destination;
                }

                comp.CurrentDoAfter = after.NextId;

                _actions.PerformAction(uid, null, xeno.Actions[comp.ActionId], action, action.BaseEvent, _timing.CurTime);

                // Means the action was cancelled for some reason
                if (comp.CurrentDoAfter == after.NextId)
                    comp.CurrentDoAfter = null;

            }
        }
    }
}
