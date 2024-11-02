using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Map;
using Content.Shared.Construction;
using Content.Shared.Construction.Conditions;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Construction.Conditions;

[DataDefinition]
public sealed partial class TileBarricadeClear : IConstructionCondition
{
    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = "rmc-construction-not-barricade-clear",
        };
    }

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var entities = IoCManager.Resolve<IEntityManager>();
        var rmcMap = entities.System<SharedRMCMapSystem>();
        return !rmcMap.HasAnchoredEntityEnumerator<BarricadeComponent>(location, facing: direction.AsFlag());
    }
}
