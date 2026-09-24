using Content.Server.Administration;
using Content.Shared._RMC14.Ladder;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server._RMC14.Admin.Utility;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
internal sealed class LinkLaddersCommand : ToolshedCommand
{
    private SharedLadderSystem? _ladder;

    [CommandImplementation]
    public void LinkLadders(IInvocationContext ctx, [CommandArgument] string sharedId, [CommandArgument] EntityUid ladder1, [CommandArgument] EntityUid ladder2)
    {
        _ladder ??= Sys<SharedLadderSystem>();

        if (_ladder.LadderIdInUse(sharedId))
        {
            ctx.WriteLine($"That Id is currently in use, use a different one!");
            return;
        }

        if (!TryComp<LadderComponent>(ladder1, out var comp1) || !TryComp<LadderComponent>(ladder2, out var comp2))
        {
            ctx.WriteLine($"One of the given ladders does not have LadderComponent!");
            return;
        }

        _ladder.ReassignLadderId((ladder1, comp1), sharedId);
        _ladder.ReassignLadderId((ladder2, comp2), sharedId);
    }
}
