using Content.Server.Actions;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Timing;
using XenoComponent = Content.Shared._CM14.Xenos.XenoComponent;
using XenoEvolveActionComponent = Content.Shared._CM14.Xenos.Evolution.XenoEvolveActionComponent;

namespace Content.Server._CM14.Xenos;

[AdminCommand(AdminFlags.Debug)]
public sealed class EvolutionCooldownCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override string Command => "evolutioncooldown";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entity = shell.Player?.AttachedEntity;
        var seconds = 0;
        if (args.Length == 1)
        {
            seconds = int.Parse(args[0]);
        }
        else if (args.Length == 2)
        {
            entity = EntityUid.Parse(args[0]);
            seconds = int.Parse(args[1]);
        }

        if (entity == null)
        {
            shell.WriteError(Loc.GetString("cm-cmd-no-entity-found", ("usage", Help)));
            return;
        }

        if (!_entities.TryGetComponent(entity, out XenoComponent? xeno))
        {
            shell.WriteError(Loc.GetString("cm-cmd-entity-no-component",
                ("entity", entity), ("component", nameof(XenoComponent))));
            return;
        }

        var actions = _entities.System<ActionsSystem>();
        var any = false;
        foreach (var action in xeno.Actions.Values)
        {
            if (!_entities.HasComponent<XenoEvolveActionComponent>(action))
                continue;

            if (seconds == 0)
            {
                actions.ClearCooldown(action);
            }
            else
            {
                actions.SetCooldown(action, _timing.CurTime, _timing.CurTime + TimeSpan.FromSeconds(seconds));
            }

            any = true;
        }

        if (!any)
        {
            shell.WriteError(Loc.GetString("cmd-evolutioncooldown-no-evolve-action", ("entity", entity)));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-evolutioncooldown-set-cooldown", ("seconds", seconds)));
    }
}
