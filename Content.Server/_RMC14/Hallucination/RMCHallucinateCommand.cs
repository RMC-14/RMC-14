using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Hallucination.Events;
using Content.Shared.Administration;
using Content.Shared.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Hallucination;

[AdminCommand(AdminFlags.Debug)]
public sealed class RMCHallucinateCommand : IConsoleCommand
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public string Command => "rmchallucinate";

    public string Description => "Forces a hallucination event on an entity";

    public string Help => "rmchallucinate <event> [entity]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            shell.WriteError(Help);
            return;
        }

        if (!_prototypes.TryIndex(args[0], out EntityPrototype? prototype) ||
            !prototype.HasComponent<RMCHallucinationEventComponent>(_compFactory))
        {
            shell.WriteError($"Unknown event {args[0]}. Options: {string.Join(", ", GetEvents())}");
            return;
        }

        var hallucination = new EntProtoId(prototype.ID);

        EntityUid target;
        if (args.Length == 2)
        {
            if (!NetEntity.TryParse(args[1], out var netTarget) || !_entity.TryGetEntity(netTarget, out var parsed))
            {
                shell.WriteError($"Invalid entity {args[1]}");
                return;
            }

            target = parsed.Value;
        }
        else if (shell.Player?.AttachedEntity is { } attached)
        {
            target = attached;
        }
        else
        {
            shell.WriteError("mo attached entity, specify one");
            return;
        }

        if (_entity.System<RMCHallucinationSystem>().ForceEvent(target, hallucination))
            shell.WriteLine($"Triggered {hallucination} on {_entity.ToPrettyString(target)}");
        else
            shell.WriteError($"{hallucination} couldnt happen on {_entity.ToPrettyString(target)} (its conditions aren't met)");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(GetEvents(), "<event>"),
            2 => CompletionResult.FromHint("[entity]"),
            _ => CompletionResult.Empty,
        };
    }

    private IEnumerable<string> GetEvents()
    {
        return _prototypes.EnumeratePrototypes<EntityPrototype>()
            .Where(p => !p.Abstract && p.HasComponent<RMCHallucinationEventComponent>(_compFactory))
            .Select(p => p.ID);
    }
}
