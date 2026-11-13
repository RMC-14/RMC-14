using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Power;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server._RMC14.Construction.Conditions;

/// <summary>
/// Final backstop that prevents completing a second APC in an area.
/// Construction is also checked before the first step, but multiple frames can be started at the same time
/// or an APC can appear after construction begins. The frame remains intact so its materials can be recovered.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class RMCApcAreaAvailable : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        var areaSystem = entityManager.System<AreaSystem>();
        if (!areaSystem.TryGetArea(uid, out var area, out _))
            return false;

        var powerSystem = entityManager.System<SharedRMCPowerSystem>();
        return !powerSystem.HasApcInArea(area.Value);
    }

    public bool DoExamine(ExaminedEvent args)
    {
        if (Condition(args.Examined, IoCManager.Resolve<IEntityManager>()))
            return false;

        args.PushMarkup(Loc.GetString("rmc-apc-construction-area-occupied"));
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry
        {
            Localization = "rmc-apc-construction-area-available",
        };
    }
}
