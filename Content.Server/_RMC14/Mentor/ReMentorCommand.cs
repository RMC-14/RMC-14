using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Mentor;

[AnyCommand]
public sealed partial class ReMentorCommand : LocalizedCommands
{
    [Dependency] private MentorManager _mentor = default!;

    public override string Command => "rementor";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player?.UserId is { } user)
            _mentor.ReMentor(user);
    }
}
