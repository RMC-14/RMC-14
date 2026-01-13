using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.DoAfter;

namespace Content.Shared._RMC14.DoAfter;

public sealed class RMCDoAfterSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public bool ShouldCancel(Shared.DoAfter.DoAfter doAfter)
    {
        // Cancel the DoAfter is the entity is resting.
        if (doAfter.Args.BreakOnRest && HasComp<XenoRestingComponent>(doAfter.Args.User))
            return true;

        return false;
    }

    public void TryCancel(Entity<DoAfterComponent?> ent, ushort? doAfterIndex)
    {
        if (doAfterIndex == null)
            return;

        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var doAfters = ent.Comp.DoAfters;
        if (!doAfters.ContainsKey(doAfterIndex.Value))
            return;

        _doAfter.Cancel(ent, doAfterIndex.Value, ent);
    }
}
