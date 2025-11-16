using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server._NC14.RoundSeed;

[AdminCommand(AdminFlags.Host)]
public sealed class GetRoundSeedCommand : IConsoleCommand
{
    public string Command => "getroundseed";
    public string Description => Loc.GetString("nc14-command-getroundseed-description");
    public string Help => Loc.GetString("nc14-command-getroundseed-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        var roundSeed = entManager.System<NCRoundSeedSystem>();

        if (!roundSeed.TryGetSeed(out var seed))
        {
            shell.WriteError(Loc.GetString("nc14-command-getroundseed-no-seed"));
            return;
        }

        shell.WriteLine(Loc.GetString("nc14-command-getroundseed-success", ("seed", seed)));
    }
}
