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

    [CommandImplementation("group_add")]
    public void GroupAdd(IInvocationContext ctx, EntityUid ladder, string groupId, int? newLevel = null)
    {
        _ladder ??= Sys<LadderSystem>();

        if (!TryComp<LadderComponent>(ladder, out var ladderComp))
        {
            ctx.WriteLine($"The given Ladder entity must have {nameof(LadderComponent)}!");
            return;
        }

        if (ladderComp.GroupId == groupId)
        {
            ctx.WriteLine($"{EntityManager.ToPrettyString(ladder)} is already in group '{groupId}'!");
            return;
        }

        if (newLevel.HasValue)
            _ladder.TrySetLevel(ladder, newLevel.Value);

        // Check for conflicts with either `newLevel` (if it's been provided), or the level already set on `ladder`.
        var group = _ladder.GetLadderGroup(groupId);
        foreach (var existingLadder in group)
        {
            // If a `newLevel` value is provided, check if there's a ladder at that level already.
            if (existingLadder.Comp.Level == newLevel)
            {
                ctx.WriteLine($"Failed to add {EntityManager.ToPrettyString(ladder)} to group '{groupId}' at level {newLevel}. {EntityManager.ToPrettyString(existingLadder)} already holds that position!");
                return;
            }
            // If not, check using the level already set on `ladder`.
            else if (existingLadder.Comp.Level == ladderComp.Level)
            {
                // This is exactly the same as one of the checks in `TryAddToGroup()`, so that the error message can be shown to the client running the command as well.
                // (There's probably a better way of doing this)
                ctx.WriteLine($"Failed to add {EntityManager.ToPrettyString(ladder)} to group '{groupId}'. {EntityManager.ToPrettyString(existingLadder)} has a duplicate 'Level' value of {ladderComp.Level}!");
                return;
            }
        }

        if (newLevel.HasValue)
            _ladder.TrySetLevel(ladder, newLevel.Value);

        if (_ladder.TryAddToGroup((ladder, ladderComp), groupId))
            ctx.WriteLine($"{EntityManager.ToPrettyString(ladder)} added to group '{groupId}'!");
    }

    [CommandImplementation("group_rem")]
    public void GroupRem(IInvocationContext ctx, EntityUid ladder, string groupId)
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
    public void SetLevel(IInvocationContext ctx, EntityUid ladder, int newLevel)
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
                ctx.WriteLine($"Failed to change the Level of {EntityManager.ToPrettyString(ladder)} to {newLevel}. {EntityManager.ToPrettyString(sameLevelLadder)} already holds that position!");
            }
        }
    }
}
