using Content.Server._RMC14.Mentor.ImaginaryFriend;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Admin.Utility;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
internal sealed class AssignImaginaryFriendCommand : ToolshedCommand
{
    private ImaginaryFriendSystem? _imaginaryFriend;

    [CommandImplementation]
    public void LinkLadders(IInvocationContext ctx, [CommandArgument] ICommonSession receivesFriend, [CommandArgument] ICommonSession becomesFriend)
    {
        _imaginaryFriend ??= Sys<ImaginaryFriendSystem>();

        if (receivesFriend.AttachedEntity is not { } targetEntity)
        {
            ctx.WriteLine(Loc.GetString("assign-imaginary-friend-command-target-no-entity"));
            return;
        }

        _imaginaryFriend.OpenImaginaryFriendConfirmWindow(becomesFriend, targetEntity);
        ctx.WriteLine(Loc.GetString("assign-imaginary-friend-command-success", ("friend", becomesFriend.Name), ("target", targetEntity)));
    }
}
