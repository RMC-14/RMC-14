using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Forge.RoundSeed;

[AdminCommand(AdminFlags.Host)]
public sealed class CFGetRoundSeedCommand : LocalizedEntityCommands
{
    public override string Command => "cfgetroundseed";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var roundSeed = EntityManager.System<CFRoundSeedSystem>();

        if (!roundSeed.TryGetSeed(out var seed))
        {
            shell.WriteError(LocalizationManager.GetString("command-cfgetroundseed-no-seed"));
            return;
        }

        shell.WriteLine(LocalizationManager.GetString("command-cfgetroundseed-success", ("seed", seed)));
    }
}
