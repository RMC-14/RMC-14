using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Hypertension;

public sealed class XenoDespoilerHypertensionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public void AddSlashPoints(Entity<XenoDespoilerHypertensionComponent> ent)
    {
        AddPoints(ent, ent.Comp.PointsPerSlash);
    }

    public void AddPoints(Entity<XenoDespoilerHypertensionComponent> ent, float amount)
    {
        if (amount <= 0)
            return;

        ent.Comp.Points += amount;
        ent.Comp.LastActivityAt = _timing.CurTime;

        while (ent.Comp.Points >= ent.Comp.PointsPerStack && ent.Comp.Stacks < ent.Comp.MaxStacks)
        {
            ent.Comp.Points -= ent.Comp.PointsPerStack;
            ent.Comp.Stacks++;
        }

        if (ent.Comp.Stacks >= ent.Comp.MaxStacks)
            ent.Comp.Points = 0;

        Dirty(ent);
    }

    public bool TrySpendStacks(Entity<XenoDespoilerHypertensionComponent> ent, int count)
    {
        if (ent.Comp.Stacks < count)
            return false;

        ent.Comp.Stacks -= count;
        Dirty(ent);
        return true;
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoDespoilerHypertensionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Stacks <= 0 && comp.Points <= 0)
                continue;

            if (now - comp.LastActivityAt < comp.DecayDelay)
                continue;

            comp.Points -= comp.DecayPerSecond * frameTime;
            while (comp.Points < 0 && comp.Stacks > 0)
            {
                comp.Stacks--;
                comp.Points += comp.PointsPerStack;
            }

            if (comp.Stacks <= 0 && comp.Points < 0)
                comp.Points = 0;

            Dirty(uid, comp);
        }
    }
}
