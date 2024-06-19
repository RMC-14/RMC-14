namespace Content.Shared._CM14.Construction;

public sealed class CMConstructionSystem : EntitySystem
{
    public bool CanConstruct(EntityUid? user)
    {
        return !HasComp<DisableConstructionComponent>(user);
    }
}
