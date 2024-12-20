using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Mentor;

[AnyCommand]
public sealed class DeMentorCommand : LocalizedCommands
{
    [Dependency] private readonly MentorManager _mentor = default!;

    public override string Command => "dementor";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is { } user)
            _mentor.DeMentor(user.Channel);
    }
}
