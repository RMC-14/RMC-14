using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Robust.Shared.Console;

namespace Content.Server._Forge.RoundSeed;

[AdminCommand(AdminFlags.Host)]
public sealed class SetRoundSeedCommand : LocalizedEntityCommands
{
    public override string Command => "setroundseed";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(LocalizationManager.GetString("command-setroundseed-usage"));
            return;
        }

        var seedText = args[0];

        if (string.IsNullOrWhiteSpace(seedText))
        {
            shell.WriteError(LocalizationManager.GetString("command-setroundseed-usage"));
            return;
        }

        var ticker = EntityManager.System<GameTicker>();

        if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            shell.WriteError(LocalizationManager.GetString("command-setroundseed-not-in-lobby"));
            return;
        }

        var actor = shell.Player != null
            ? shell.Player.Name
            : LocalizationManager.GetString("command-setroundseed-server-console");

        var roundSeed = EntityManager.System<RoundSeedSystem>();
        roundSeed.SetNextSeed(seedText, actor);

        shell.WriteLine(LocalizationManager.GetString("command-setroundseed-success", ("seed", seedText)));
    }
}
