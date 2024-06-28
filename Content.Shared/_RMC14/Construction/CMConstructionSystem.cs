namespace Content.Shared._RMC14.Construction;

public sealed class CMConstructionSystem : EntitySystem
{
    public bool CanConstruct(EntityUid? user)
    {
        return !HasComp<DisableConstructionComponent>(user);
    }
}
