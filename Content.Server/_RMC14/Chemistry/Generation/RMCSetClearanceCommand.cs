using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Chemistry.Generation;

[AdminCommand(AdminFlags.Debug)]
public sealed class RMCSetClearanceCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystems = default!;

    public string Command => "rmcsetclearance";
    public string Description => "Sets the research clearance level.";
    public string Help => $"{Command} <level>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || !int.TryParse(args[0], out var level))
        {
            shell.WriteError("Level must be a number.");
            return;
        }

        var research = _entitySystems.GetEntitySystem<RMCChemistryResearchSystem>();
        var clamped = research.SetClearance(level);
        shell.WriteLine($"Clearance level set to {clamped}.");
    }
}
