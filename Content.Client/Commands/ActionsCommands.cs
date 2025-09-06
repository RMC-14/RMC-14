using Content.Client.Actions;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[AnyCommand]
public sealed class SaveActionsCommand : IConsoleCommand
{
    public string Command => "saveacts";
    public string Description => "Saves the current action toolbar assignments to a file";
    public string Help => $"Usage: {Command} <user resource path>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        try
        {
            EntitySystem.Get<ActionsSystem>().SaveActionAssignments(args[0]);
        }
        catch
        {
            shell.WriteLine("Failed to save action assignments");
        }
    }
}

[AnyCommand]
public sealed class LoadActionsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "loadacts";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        try
        {
            _entitySystemManager.GetEntitySystem<ActionsSystem>().LoadActionAssignments(args[0], true);
        }
        catch
        {
            shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error"));
        }
    }
}

[AnyCommand]
public sealed class LoadActionPositionsCommand : IConsoleCommand
{
    public string Command => "loadactpos";
    public string Description => "Loads only the positions of existing actions from a file";
    public string Help => $"Usage: {Command} <user resource path>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        try
        {
            EntitySystem.Get<ActionsSystem>().LoadActionPositions(args[0], true);
            shell.WriteLine("Loaded action positions");
        }
        catch
        {
            shell.WriteLine("Failed to load action positions");
        }
    }
}
