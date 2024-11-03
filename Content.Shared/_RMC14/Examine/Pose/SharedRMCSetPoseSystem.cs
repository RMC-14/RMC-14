using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Mobs;

namespace Content.Shared._RMC14.Examine.Pose;

public abstract class SharedRMCSetPoseSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSetPoseComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCSetPoseComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RMCSetPoseComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(Entity<RMCSetPoseComponent> ent, ref MapInitEvent ev)
    {
        if (_actions.AddAction(ent, ref ent.Comp.Action, out var action, ent.Comp.ActionPrototype))
            action.EntityIcon = ent;
    }

    private void OnExamine(Entity<RMCSetPoseComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;

        if (comp.Pose.Trim() == string.Empty)
            return;

        using (args.PushGroup(nameof(RMCSetPoseComponent)))
        {
            var pose = Loc.GetString("rmc-set-pose-examined", ("ent", ent), ("pose", comp.Pose));
            args.PushMarkup(pose, -5);
        }
    }

    private void OnMobStateChanged(Entity<RMCSetPoseComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        ent.Comp.Pose = string.Empty; // reset the pose on death/crit
        Dirty(ent);
    }
}

public sealed partial class RMCSetPoseActionEvent : InstantActionEvent;
