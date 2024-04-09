using Content.Server.Body.Systems;
using Content.Shared._CM14.Medical.Stasis;

namespace Content.Server._CM14.Medical.Stasis;

public sealed class CMStasisBagSystem : SharedCMStasisBagSystem
{
    protected override void OnInsert(Entity<CMStasisBagComponent> bag, EntityUid target)
    {
        base.OnInsert(bag, target);

        // what in gods name is this api upstream TODO CM14
        var ev = new ApplyMetabolicMultiplierEvent(target, bag.Comp.MetabolismMultiplier, true);
        RaiseLocalEvent(target, ref ev);
    }

    protected override void OnRemove(Entity<CMStasisBagComponent> bag, EntityUid target)
    {
        base.OnRemove(bag, target);

        var ev = new ApplyMetabolicMultiplierEvent(target, bag.Comp.MetabolismMultiplier, false);
        RaiseLocalEvent(target, ref ev);
    }
}
