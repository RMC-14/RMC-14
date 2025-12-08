using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Drowsyness;

public sealed class DrowsynessSystem : EntitySystem
{
    public void TryChange(Entity<DrowsynessComponent?> ent, FixedPoint2 amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Amount += amount;
        Dirty(ent);
    }

    public void TrySet(Entity<DrowsynessComponent?> ent, FixedPoint2 amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Amount = amount;
        Dirty(ent);
    }
}
