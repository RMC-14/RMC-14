using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Power;

[AdminCommand(AdminFlags.Admin)]
public sealed class RMCPowerBlackoutCommand : IConsoleCommand
{
    public string Command => "rmc_power_blackout";
    public string Description => "Black out the named RMC power network containing an entity.";
    public string Help => "Usage: rmc_power_blackout <NetEntity>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!TryGetNetwork(shell, args, out var power, out var key))
            return;

        if (!power.BlackoutNetwork(key))
        {
            shell.WriteError("The selected power network has no APC or SMES participants.");
            return;
        }

        shell.WriteLine($"Blacked out RMC power network '{key.PowerNet}' on map entity {key.Map}.");
    }

    internal static bool TryGetNetwork(
        IConsoleShell shell,
        string[] args,
        out RMCPowerSystem power,
        out Content.Shared._RMC14.Power.RMCPowerNetworkKey key)
    {
        power = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RMCPowerSystem>();
        key = default;
        var entities = IoCManager.Resolve<IEntityManager>();
        if (args.Length < 1 ||
            !NetEntity.TryParse(args[0], out var netEntity) ||
            !entities.TryGetEntity(netEntity, out var entity) ||
            !power.TryGetPowerNetwork(entity.Value, out key))
        {
            shell.WriteError("Expected a networked entity located inside an RMC powered area.");
            return false;
        }

        return true;
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class RMCPowerRestoreCommand : IConsoleCommand
{
    public string Command => "rmc_power_restore";
    public string Description => "Restore the named RMC power network containing an entity.";
    public string Help => "Usage: rmc_power_restore <NetEntity> [advanced]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!RMCPowerBlackoutCommand.TryGetNetwork(shell, args, out var power, out var key))
            return;

        var advanced = args.Length > 1 &&
                       (args[1].Equals("advanced", StringComparison.OrdinalIgnoreCase) ||
                        bool.TryParse(args[1], out var enabled) && enabled);
        if (!power.RestoreNetwork(key, advanced))
        {
            shell.WriteError("The selected power network has no restorable participants.");
            return;
        }

        shell.WriteLine($"Restored RMC power network '{key.PowerNet}' on map entity {key.Map}" +
                        (advanced ? " with reactor repair." : "."));
    }
}
