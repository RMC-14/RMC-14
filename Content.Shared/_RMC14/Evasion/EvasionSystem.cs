using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Standing;

namespace Content.Shared._RMC14.Evasion;

public sealed class EvasionSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<EvasionComponent, MapInitEvent>(CallRefresh);
        SubscribeLocalEvent<EvasionComponent, XenoRestEvent>(CallRefresh);
        SubscribeLocalEvent<EvasionComponent, DownedEvent>(CallRefresh);
        SubscribeLocalEvent<EvasionComponent, StoodEvent>(CallRefresh);

        SubscribeLocalEvent<RMCSizeComponent, EvasionRefreshModifiersEvent>(OnSizeRefreshEvasion);
    }

    public void RefreshEvasionModifiers(EntityUid entity)
    {
        if (!TryComp(entity, out EvasionComponent? evasionComponent))
            return;

        RefreshEvasionModifiers((entity, evasionComponent));
    }

    public void RefreshEvasionModifiers(Entity<EvasionComponent> entity)
    {
        var ev = new EvasionRefreshModifiersEvent(
            entity,
            entity.Comp.Evasion,
            entity.Comp.EvasionFriendly);

        RaiseLocalEvent(entity.Owner, ref ev);

        entity.Comp.ModifiedEvasion = ev.Evasion;
        entity.Comp.ModifiedEvasionFriendly = ev.EvasionFriendly;

        Dirty(entity);
    }

    private void CallRefresh<T>(Entity<EvasionComponent> entity, ref T args) where T : notnull
    {
        RefreshEvasionModifiers(entity);
    }

    private void OnSizeRefreshEvasion(Entity<RMCSizeComponent> size, ref EvasionRefreshModifiersEvent args)
    {
        if (size.Owner != args.Entity.Owner)
            return;

        if (size.Comp.Size <= RMCSizes.Small)
            args.Evasion += (int) EvasionModifiers.SizeSmall;

        if (size.Comp.Size >= RMCSizes.Big)
            args.Evasion += (int) EvasionModifiers.SizeBig;
    }
}
