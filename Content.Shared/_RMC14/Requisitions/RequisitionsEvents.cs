using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Requisitions;

public sealed class RequisitionsOrderPlacedEvent : EntityEventArgs
{
    public EntityUid Actor;
    public int CategoryIndex;
    public int OrderIndex;
    public EntProtoId Crate;

    public RequisitionsOrderPlacedEvent(EntityUid actor, int categoryIndex, int orderIndex, EntProtoId crate)
    {
        Actor = actor;
        CategoryIndex = categoryIndex;
        OrderIndex = orderIndex;
        Crate = crate;
    }
}

public sealed class RequisitionsSetCategoriesEvent : EntityEventArgs
{
    public List<RequisitionsCategory> Categories;

    public RequisitionsSetCategoriesEvent(List<RequisitionsCategory> categories)
    {
        Categories = categories;
    }
}
