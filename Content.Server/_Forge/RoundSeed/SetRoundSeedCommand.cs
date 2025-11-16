using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server._NC14.RoundSeed;

[AdminCommand(AdminFlags.Host)]
public sealed class SetRoundSeedCommand : IConsoleCommand
{
    public string Command => "setroundseed";
    public string Description => Loc.GetString("nc14-command-setroundseed-description");
    public string Help => Loc.GetString("nc14-command-setroundseed-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("nc14-command-setroundseed-usage"));
            return;
        }

        var seedText = args[0];

        if (string.IsNullOrWhiteSpace(seedText))
        {
            shell.WriteError(Loc.GetString("nc14-command-setroundseed-usage"));
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var ticker = entManager.System<GameTicker>();

        if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            shell.WriteError(Loc.GetString("nc14-command-setroundseed-not-in-lobby"));
            return;
        }

        var actor = shell.Player != null
            ? shell.Player.Name
            : Loc.GetString("nc14-command-setroundseed-server-console");

        var roundSeed = entManager.System<NCRoundSeedSystem>();
        roundSeed.SetNextSeed(seedText, actor);

        shell.WriteLine(Loc.GetString("nc14-command-setroundseed-success", ("seed", seedText)));
    }
}
