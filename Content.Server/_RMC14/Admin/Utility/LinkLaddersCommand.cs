using Content.Server._RMC14.Ladder;
using Content.Server.Administration;
using Content.Shared._RMC14.Ladder;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Admin.Utility;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
internal sealed class LadderCommand : ToolshedCommand
{
    private LadderSystem? _ladder;

    [CommandImplementation("link")]
    public void LinkLadder(IInvocationContext ctx, string groupId, EntityUid ladder)
    {
        _ladder ??= Sys<LadderSystem>();

        if (!TryComp<LadderComponent>(ladder, out var ladderComp))
        {
            ctx.WriteLine($"The given Ladder entity must have {nameof(LadderComponent)}!");
            return;
        }

        if (_ladder.TryAddToGroup((ladder, ladderComp), groupId))
        {
            ctx.WriteLine($"{EntityManager.ToPrettyString(ladder)} added to group '{groupId}'!");
            return;
        }

        // Duplicate check as one in `TryAddToGroup()` if that returned false, so that the error message can be shown to the client running the command as well.
        // (There's probably a better way of doing this)
        var group = _ladder.GetLadderGroup(groupId);
        if (group.TryFirstOrNull(l => l.Comp.Level == ladderComp.Level, out var sameLevelLadder))
            ctx.WriteLine($"Failed to add {EntityManager.ToPrettyString(ladder)} to group '{groupId}'. {EntityManager.ToPrettyString(sameLevelLadder)} has a duplicate 'Level' value of {ladderComp.Level}!");
    }

    [CommandImplementation("unlink")]
    public void UnlinkLadder(IInvocationContext ctx, string groupId, EntityUid ladder)
    {
        _ladder ??= Sys<LadderSystem>();

        if (!TryComp<LadderComponent>(ladder, out var ladderComp))
        {
            ctx.WriteLine($"The given Ladder entity must have {nameof(LadderComponent)}!");
            return;
        }

        if (ladderComp.GroupId != groupId)
        {
            ctx.WriteLine($"{EntityManager.ToPrettyString(ladder)} isn't in the '{groupId}' group! ('{ladderComp.GroupId}' != '{groupId}')");
            return;
        }

        if (_ladder.TryRemoveFromGroup((ladder, ladderComp), groupId))
            ctx.WriteLine($"{EntityManager.ToPrettyString(ladder)} removed from group '{groupId}'!");
    }

    [CommandImplementation("set_level")]
    public void SetLevel(IInvocationContext ctx, int newLevel, EntityUid ladder)
    {
        _ladder ??= Sys<LadderSystem>();

        if (!TryComp<LadderComponent>(ladder, out var ladderComp))
        {
            ctx.WriteLine($"The given Ladder entity must have {nameof(LadderComponent)}!");
            return;
        }

        if (_ladder.TrySetLevel((ladder, ladderComp), newLevel))
        {
            ctx.WriteLine($"Level of {EntityManager.ToPrettyString(ladder)} set to {newLevel}!");
            return;
        }

        // Same as above in `LinkLadder()`, this is a copy of one of the checks in `TrySetLevel()` in order to pass the error message along.
        if (ladderComp.GroupId != null)
        {
            var group = _ladder.GetLadderGroup(ladderComp.GroupId);
            if (group.TryFirstOrNull(l => l.Comp.Level == newLevel, out var sameLevelLadder))
            {
                ctx.WriteLine($"Failed to change the Level of {EntityManager.ToPrettyString(ladder)} to {newLevel}, as {EntityManager.ToPrettyString(sameLevelLadder)} already holds that position!");
            }
        }
    }
}
