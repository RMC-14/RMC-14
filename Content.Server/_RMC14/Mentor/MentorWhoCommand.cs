using System.Linq;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Mentor;

[AnyCommand]
public sealed class MentorWhoCommand : LocalizedCommands
{
    [Dependency] private readonly MentorManager _mentor = default!;

    public override string Command => "mentorwho";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
            return;

        if (!_mentor.IsMentor(shell.Player.UserId))
            return;

        shell.WriteLine(string.Join("\n", _mentor.GetActiveMentors().Select(m => m.Name).Order()));
    }
}
