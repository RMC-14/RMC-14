using Content.Shared._RMC14.Xenonids.Rest;

namespace Content.Shared._RMC14.DoAfter;

public sealed class RMCDoafterSystem : EntitySystem
{
    public bool ShouldCancel(Shared.DoAfter.DoAfter doAfter)
    {
        // Cancel the DoAfter is the entity is resting.
        if (doAfter.Args.BreakOnRest && HasComp<XenoRestingComponent>(doAfter.Args.User))
            return true;

        return false;
    }
}
