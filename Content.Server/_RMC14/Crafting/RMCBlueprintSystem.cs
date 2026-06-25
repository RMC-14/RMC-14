using Content.Shared._RMC14.Crafting.Components;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Text;
using Content.Shared.Tag;
using Content.Shared.Crafting.Prototypes;
using System.Diagnostics;

namespace Content.Server.Crafting;
/// <summary>
/// Recipe system. The cool thing about it is that items made with recipes created through it will always have the
/// correct ingredients, because it pulls the data directly from the recipe prototype (craftRecipe).
/// </summary>
public sealed class STBlueprintSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private ISawmill _sawmill = default!;
    private Dictionary<string, string> _descriptionsByBlueprint = new();
    private Dictionary<string, string> _namesByBlueprint = new();
    private const string WORKBENCH_TAG = "RMCWorkbench";
    private Dictionary<string, string> _workbenchNamesById = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("Blueprint");
        _workbenchNamesById = _proto.EnumeratePrototypes<EntityPrototype>().Where(entity =>
            entity.TryGetComponent<TagComponent>(out var tag, _componentFactory) && _tagSystem.HasTag(tag, WORKBENCH_TAG)
        ).ToDictionary(entity => entity.ID, entity => entity.Name);

        if (_workbenchNamesById.Count == 0)
        {
            _sawmill.Error($"There is no valid workbenches. Check that {WORKBENCH_TAG} exist");
        }
        AddDescriptions();
        SubscribeLocalEvent<CraftingBlueprintComponent, ExaminedEvent>(OnBlueprintExamine);
    }

    /// <summary>
    /// Shift right-clicking shows the detailed crafting recipe in the description
    /// </summary>
    public void OnBlueprintExamine(EntityUid uid, CraftingBlueprintComponent component, ExaminedEvent args)
    {
        if (!component.BlueprintId.HasValue)
            return;
        if (!args.IsInDetailsRange)
            return;
        if (!_descriptionsByBlueprint.TryGetValue(component.BlueprintId.Value.Id, out var description))
            return;

        args.PushMarkup(description);
    }

    private void AddDescriptions()
    {
        var blueprints = _proto.EnumeratePrototypes<CraftingPrototype>().ToList();

        foreach (var blueprint in blueprints)
        {
            var stringBuilder = new StringBuilder();
            string workbench = Loc.GetString("rmc-blueprint-anyworkbench");
            if (blueprint.RequiredWorkbench != null && _workbenchNamesById.TryGetValue(blueprint.RequiredWorkbench, out var workbenchName))
            {
                workbench = workbenchName;
            }

            string workbenchDetails = $"{Loc.GetString("rmc-blueprint-workbench")}: {workbench}";
            stringBuilder.AppendLine(workbenchDetails);

            stringBuilder.AppendLine(Loc.GetString("rmc-blueprint-ingridients"));
            foreach (var (id, details) in blueprint.Items)
            {
                var name = id;

                if (!details.Tag)
                {
                    if (!_proto.TryIndex(id, out var prototype))
                    {
                        _sawmill.Error($"There is a recipe {blueprint.ID} with an ingredient {id}, but the ingredient prototype is missing.");
                        stringBuilder.AppendLine(Loc.GetString("rmc-blueprint-not-found"));
                        continue;
                    }

                    name = prototype.Name;
                }

                stringBuilder.AppendLine($"\t{name} {details.Amount} {GetCatalistIcon(details.Catalyzer)}");
            }
            stringBuilder.AppendLine(Loc.GetString("rmc-blueprint-result"));
            string? resultName = null;

            foreach (var id in blueprint.ResultProtos)
            {
                if (!_proto.TryIndex(id, out var prototype))
                {
                    _sawmill.Error($"There is a recipe {blueprint.ID} with a result {id}. But the result's prototype is missing");
                    stringBuilder.AppendLine(Loc.GetString("rmc-blueprint-not-found"));
                    continue;
                }
                if (resultName == null)
                    resultName = prototype.Name;
                stringBuilder.AppendLine($"\t{prototype.Name}");
            }
            var description = stringBuilder.ToString();
            var multipleResults = blueprint.ResultProtos.Count > 1 ? Loc.GetString("rmc-blueprint-multiple-results") : string.Empty;
            resultName = $"{Loc.GetString("rmc-blueprint-prefix")} {resultName} {multipleResults}";

            _namesByBlueprint.Add(blueprint.ID, resultName);
            _descriptionsByBlueprint.Add(blueprint.ID, description);
        }
    }

    private string GetCatalistIcon(bool isCatalyzer)
    {
        return isCatalyzer ? Loc.GetString("rmc-blueprint-ingridient-saved") : "";
    }
}
