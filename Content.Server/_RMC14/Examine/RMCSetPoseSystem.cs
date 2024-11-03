using Content.Server.Administration;
using Content.Shared._RMC14.Examine.Pose;
using Content.Shared.Actions;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Examine;

public sealed class RMCSetPoseSystem : SharedRMCSetPoseSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSetPoseComponent, RMCSetPoseActionEvent>(OnSetPoseAction);
    }

    private void OnSetPoseAction(Entity<RMCSetPoseComponent> ent, ref RMCSetPoseActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        var setPosePrompt = Loc.GetString("rmc-set-pose-dialog", ("ent", ent));

        _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("rmc-set-pose-title"), setPosePrompt,
            (string pose) =>
            {
                ent.Comp.Pose = pose;
                Dirty(ent);
            });


        args.Handled = true;
    }
}
