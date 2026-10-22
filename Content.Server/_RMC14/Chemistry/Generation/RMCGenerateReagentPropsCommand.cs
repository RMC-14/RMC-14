using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Administration;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Generation;

[AdminCommand(AdminFlags.Debug)]
public sealed class RMCGenerateReagentPropsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystems = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public string Command => "rmcgeneratereagentprops";
    public string Description => "Generates a new reagent.";
    public string Help => "rmcgeneratereagentprops <tier 1-3> <volume 1-1000> <Property:Level> [Property:Level ...]";

    private static readonly (int MaxVolume, EntProtoId Container, string Solution)[] Containers =
    {
        (30, "RMCVial", "beaker"),
        (60, "CMBeaker", "beaker"),
        (120, "CMBeakerLarge", "beaker"),
        (300, "RMCBeakerHighCapacity", "beaker"),
        (500, "RMCReagentJug", "beaker"),
        (1000, "RMCTankReagentEmpty", "tank"),
    };

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3)
        {
            shell.WriteError(Help);
            return;
        }

        if (!int.TryParse(args[0], out var tier))
        {
            shell.WriteError("Tier must be a number 1-3.");
            return;
        }

        if (!int.TryParse(args[1], out var volume) || volume < 1 || volume > Containers[^1].MaxVolume)
        {
            shell.WriteError($"Volume must be a number 1-{Containers[^1].MaxVolume}.");
            return;
        }

        var properties = new List<(ChemGeneratorPropertyPrototype Property, int Level)>();
        for (var i = 2; i < args.Length; i++)
        {
            var split = args[i].Split(':');
            if (split.Length != 2 || !int.TryParse(split[1], out var level))
            {
                shell.WriteError($"Malformed property argument \"{args[i]}\" - expected Property:Level.");
                return;
            }

            if (!_prototype.TryIndex<ChemGeneratorPropertyPrototype>(split[0], out var property))
            {
                var valid = string.Join(", ", _prototype.EnumeratePrototypes<ChemGeneratorPropertyPrototype>().Select(p => p.ID).OrderBy(id => id));
                shell.WriteError($"Unknown property \"{split[0]}\". Valid properties: {valid}");
                return;
            }

            properties.Add((property, level));
        }

        var generator = _entitySystems.GetEntitySystem<RMCChemicalGeneratorSystem>();
        var rmcReagent = _entitySystems.GetEntitySystem<RMCReagentSystem>();
        var research = _entitySystems.GetEntitySystem<RMCChemistryResearchSystem>();

        Reagent reagentProto;
        ProtoId<ReagentPrototype> reagentId;
        try
        {
            reagentId = generator.GenerateReagentWithProperties(properties, tier);
            reagentProto = rmcReagent.Index(reagentId);
        }
        catch (Exception e)
        {
            shell.WriteError($"Generation failed: {e}");
            return;
        }

        if (research.AddContractForReagent(reagentId, tier))
            shell.WriteLine("Added a matching research contract for it.");
        else
            shell.WriteError("Could not add a research contract for it.");

        shell.WriteLine($"Generated reagent: {reagentId} (\"{reagentProto.LocalizedName}\")");
        shell.WriteLine($"  Color: {reagentProto.SubstanceColor}");
        shell.WriteLine($"  Overdose: {reagentProto.Overdose} / Critical: {reagentProto.CriticalOverdose}");

        if (reagentProto.Metabolisms is { } metabolisms)
        {
            foreach (var (group, entry) in metabolisms)
            {
                shell.WriteLine($"  Metabolism group: {group}");
                foreach (var effect in entry.Effects)
                    shell.WriteLine($"    - {effect.GetType().Name}");
            }
        }

        var recipe = _prototype.EnumeratePrototypes<ReactionPrototype>()
            .FirstOrDefault(r => r.Products.ContainsKey(reagentId.Id));
        if (recipe != null)
        {
            shell.WriteLine("  Recipe:");
            foreach (var (reactantId, reactant) in recipe.Reactants)
                shell.WriteLine($"    - {reactantId} x{reactant.Amount}{(reactant.Catalyst ? " (catalyst)" : "")}");
        }
        else
        {
            shell.WriteLine("  No recipe was generated.");
        }

        var coords = EntityCoordinates.Invalid;
        if (shell.Player?.AttachedEntity is { } attached && _entity.TryGetComponent(attached, out TransformComponent? xform))
            coords = xform.Coordinates;

        if (!coords.IsValid(_entity))
        {
            shell.WriteLine("No attached entity to spawn a container at - skipping container spawn.");
            return;
        }

        var (_, container, solutionName) = Array.Find(Containers, c => volume <= c.MaxVolume);
        var solution = _entitySystems.GetEntitySystem<SharedSolutionContainerSystem>();
        var spawned = _entity.SpawnEntity(container, coords);
        if (solution.TryGetSolution(spawned, solutionName, out var soln, out _))
        {
            solution.TryAddReagent(soln.Value, reagentId, FixedPoint2.New(volume));
            shell.WriteLine($"Spawned a {container} with {volume}u of {reagentId}.");
        }
        else
        {
            shell.WriteError($"Spawned {container} but could not find its solution to fill.");
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint("<tier 1-3>");

        if (args.Length == 2)
            return CompletionResult.FromHint($"<volume 1-{Containers[^1].MaxVolume}>");

        var used = args.Skip(2).Select(a => a.Split(':')[0]).ToHashSet();
        var options = _prototype.EnumeratePrototypes<ChemGeneratorPropertyPrototype>()
            .Where(p => !used.Contains(p.ID))
            .Select(p => new CompletionOption(p.ID, p.Category.ToString()))
            .OrderBy(o => o.Value);

        return CompletionResult.FromHintOptions(options, "<Property:Level>");
    }
}
