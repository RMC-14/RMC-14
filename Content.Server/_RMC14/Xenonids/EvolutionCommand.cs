using Content.Server.Administration;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Xenonids;

[AdminCommand(AdminFlags.Debug)]
public sealed class EvolutionCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override string Command => "evolution";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entity = shell.Player?.AttachedEntity;
        int? points = null;
        if (args.Length == 1)
        {
            points = int.Parse(args[0]);
        }
        else if (args.Length == 2)
        {
            entity = EntityUid.Parse(args[0]);
            points = int.Parse(args[1]);
        }

        if (entity == null)
        {
            shell.WriteError(Loc.GetString("cm-cmd-no-entity-found", ("usage", Help)));
            return;
        }

        if (!_entities.TryGetComponent(entity, out XenoEvolutionComponent? evolution))
        {
            shell.WriteError(Loc.GetString("cmd-evolution-cant-evolve", ("entity", entity)));
            return;
        }

        _entities.System<XenoEvolutionSystem>().SetPoints((entity.Value, evolution), points ?? evolution.Max);
        shell.WriteLine(Loc.GetString("cmd-evolution-set-points", ("points", evolution.Points)));
    }
}
