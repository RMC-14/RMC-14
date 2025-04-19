using Robust.Shared.Console;

namespace Content.Client._RMC14.Areas;

public sealed class ShowAreasCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "showareas";
    public string Description => "Shows areas depending on their properties.";
    public string Help => $"Usage: {Command} disable | {Command} cas";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0 || args.Length > 1)
        {
            shell.WriteLine(Help);
            return;
        }

        var areas = _entities.System<AreasCommandSystem>();
        switch (args[0].ToLowerInvariant())
        {
            case "cas":
                areas.ShowCAS = !areas.ShowCAS;
                shell.WriteLine($"Showing areas with {nameof(areas.ShowCAS)}: {areas.ShowCAS}");
                break;
            case "disable":
                areas.Enabled = false;
                shell.WriteLine("Disabled area visualizer");
                return;
            default:
                shell.WriteLine(Help);
                return;
        }

        areas.Enabled = true;
    }
}
