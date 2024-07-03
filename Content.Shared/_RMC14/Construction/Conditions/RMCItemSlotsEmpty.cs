using Content.Shared._RMC14.Inventory;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Shared._RMC14.Construction.Conditions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class RMCItemSlotsEmpty : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        return entityManager.System<SharedCMInventorySystem>().GetItemSlotsFilled(uid).Filled == 0;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var inventorySys = entMan.System<SharedCMInventorySystem>();

        if (inventorySys.GetItemSlotsFilled(args.Examined).Filled == 0)
            return false;

        args.PushMarkup(Loc.GetString("rmc-construction-slots-examine-empty"));
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry
        {
            Localization = "rmc-construction-slots-guide-empty",
        };
    }
}
