using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Mentor;

[AnyCommand]
public sealed class MentorChatCommand : IConsoleCommand
{
    [Dependency] private readonly MentorManager _mentor = default!;

    public string Command => "msay";
    public string Description => "Send chat messages to the private mentor chat channel.";
    public string Help => "msay <text>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;

        if (player == null)
        {
            shell.WriteError("You can't run this command locally.");
            return;
        }

        if (!_mentor.IsMentor(player.UserId))
            return;

        if (args.Length < 1)
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        IoCManager.Resolve<IChatManager>().TrySendOOCMessage(player, message, OOCChatType.Mentor);
    }
}
