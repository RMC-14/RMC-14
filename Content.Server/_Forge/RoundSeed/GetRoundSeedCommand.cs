using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server._Forge.RoundSeed;

[AdminCommand(AdminFlags.Host)]
public sealed class GetRoundSeedCommand : IConsoleCommand
{
    public string Command => "getroundseed";
    public string Description => Loc.GetString("command-getroundseed-description");
    public string Help => Loc.GetString("command-getroundseed-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        var roundSeed = entManager.System<RoundSeedSystem>();

        if (!roundSeed.TryGetSeed(out var seed))
        {
            shell.WriteError(Loc.GetString("command-getroundseed-no-seed"));
            return;
        }

        shell.WriteLine(Loc.GetString("command-getroundseed-success", ("seed", seed)));
    }
}
