using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Power;
using Content.Shared.Construction;
using Content.Shared.Construction.Conditions;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Construction.Conditions;

/// <summary>
/// Prevents starting APC construction in an area that already has an APC.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class RMCApcConstructionAreaAvailable : IConstructionCondition
{
    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var areaSystem = entityManager.System<AreaSystem>();
        if (!areaSystem.TryGetArea(location, out var area, out _))
            return false;

        var powerSystem = entityManager.System<SharedRMCPowerSystem>();
        return !powerSystem.HasApcInArea(area.Value);
    }

    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = "rmc-apc-construction-area-available",
        };
    }
}
