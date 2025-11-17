using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Robust.Shared.Console;

namespace Content.Server._Forge.RoundSeed;

[AdminCommand(AdminFlags.Host)]
public sealed class CFSetRoundSeedCommand : LocalizedEntityCommands
{
    public override string Command => "cfsetroundseed";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(LocalizationManager.GetString("command-cfsetroundseed-usage"));
            return;
        }

        var seedText = args[0];

        if (string.IsNullOrWhiteSpace(seedText))
        {
            shell.WriteError(LocalizationManager.GetString("command-cfsetroundseed-usage"));
            return;
        }

        var ticker = EntityManager.System<GameTicker>();

        if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            shell.WriteError(LocalizationManager.GetString("command-cfsetroundseed-not-in-lobby"));
            return;
        }

        var actor = shell.Player != null
            ? shell.Player.Name
            : LocalizationManager.GetString("command-cfsetroundseed-server-console");

        var roundSeed = EntityManager.System<CFRoundSeedSystem>();
        roundSeed.SetNextSeed(seedText, actor);

        shell.WriteLine(LocalizationManager.GetString("command-cfsetroundseed-success", ("seed", seedText)));
    }
}
