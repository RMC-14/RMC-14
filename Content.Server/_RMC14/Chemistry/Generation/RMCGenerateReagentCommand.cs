using System.Linq;
using Content.Server.Administration;
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
public sealed class RMCGenerateReagentCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystems = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public string Command => "rmcgeneratereagent";
    public string Description => "Generates new reagent with random properties, prints details, & filled bottle";
    public string Help => "rmcgeneratereagent <tier 1-3>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var tier = 1;
        if (args.Length > 0 && !int.TryParse(args[0], out tier))
        {
            shell.WriteError("Tier must be a number 1-3.");
            return;
        }

        var generator = _entitySystems.GetEntitySystem<RMCChemicalGeneratorSystem>();
        var rmcReagent = _entitySystems.GetEntitySystem<RMCReagentSystem>();
        var research = _entitySystems.GetEntitySystem<RMCChemistryResearchSystem>();

        Reagent reagentProto;
        ProtoId<ReagentPrototype> reagentId;
        try
        {
            reagentId = generator.GenerateReagent(tier);
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
            shell.WriteLine("No attached entity to spawn a bottle at - skipping bottle spawn.");
            return;
        }

        var solution = _entitySystems.GetEntitySystem<SharedSolutionContainerSystem>();
        var vial = _entity.SpawnEntity("RMCVial", coords);
        if (solution.TryGetSolution(vial, "beaker", out var soln, out _))
        {
            solution.TryAddReagent(soln.Value, reagentId, FixedPoint2.New(30));
            shell.WriteLine($"Spawned a vial of {reagentId}.");
        }
        else
        {
            shell.WriteError("Spawned vial but could not find its solution to fill.");
        }
    }
}
