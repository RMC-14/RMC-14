using Content.Shared._RMC14.Evasion;

namespace Content.Shared._RMC14.Stealth;

public sealed class ActiveInvisibleSystem : EntitySystem
{
    [Dependency] private readonly EvasionSystem _evasionSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EntityActiveInvisibleComponent, ComponentAdd>(OnInvisibleComponentAdd);
        SubscribeLocalEvent<EntityActiveInvisibleComponent, ComponentRemove>(OnInvisibleComponentRemove);
        SubscribeLocalEvent<EntityActiveInvisibleComponent, EvasionRefreshModifiersEvent>(OnInvisibleRefreshModifiers);
    }

    private void OnInvisibleComponentAdd(Entity<EntityActiveInvisibleComponent> entity, ref ComponentAdd args)
    {
        _evasionSystem.RefreshEvasionModifiers(entity.Owner);
    }

    private void OnInvisibleComponentRemove(Entity<EntityActiveInvisibleComponent> entity, ref ComponentRemove args)
    {
        _evasionSystem.RefreshEvasionModifiers(entity.Owner);
    }

    private void OnInvisibleRefreshModifiers(Entity<EntityActiveInvisibleComponent> entity, ref EvasionRefreshModifiersEvent args)
    {
        if (entity.Owner != args.Entity.Owner)
            return;

        args.Evasion += entity.Comp.EvasionModifier;
        args.EvasionFriendly += entity.Comp.EvasionFriendlyModifier;
    }
}
