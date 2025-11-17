using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Forge.RoundSeed;

[AdminCommand(AdminFlags.Host)]
public sealed class GetRoundSeedCommand : LocalizedEntityCommands
{
    public override string Command => "getroundseed";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var roundSeed = EntityManager.System<RoundSeedSystem>();

        if (!roundSeed.TryGetSeed(out var seed))
        {
            shell.WriteError(LocalizationManager.GetString("command-getroundseed-no-seed"));
            return;
        }

        shell.WriteLine(LocalizationManager.GetString("command-getroundseed-success", ("seed", seed)));
    }
}
