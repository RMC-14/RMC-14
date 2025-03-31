using Content.Server.Administration;
using Content.Shared._RMC14.Examine.Pose;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Examine;

public sealed class RMCSetPoseSystem : SharedRMCSetPoseSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    protected override void SetPose(Entity<RMCSetPoseComponent> ent)
    {
        base.SetPose(ent);

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        var setPosePrompt = Loc.GetString("rmc-set-pose-dialog", ("ent", ent));
        _quickDialog.OpenDialog(actor.PlayerSession,
            Loc.GetString("rmc-set-pose-title"),
            setPosePrompt,
            (string pose) =>
            {
                if (pose.Length > 1000)
                    pose = pose[..999];
                _adminLog.Add(LogType.RMCSetPose, $"{ToPrettyString(ent)} set their pose to {pose}");
                ent.Comp.Pose = pose;
                Dirty(ent);
            }
        );
    }
}
