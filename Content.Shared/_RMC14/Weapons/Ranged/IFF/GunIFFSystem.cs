using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

public sealed class GunIFFSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private EntityQuery<UserIFFComponent> _userIFFQuery;

    public override void Initialize()
    {
        _userIFFQuery = GetEntityQuery<UserIFFComponent>();

        SubscribeLocalEvent<UserIFFComponent, GetIFFFactionEvent>(OnUserIFFGetFaction);
        SubscribeLocalEvent<InventoryComponent, GetIFFFactionEvent>(OnInventoryIFFGetFaction);
        SubscribeLocalEvent<HandsComponent, GetIFFFactionEvent>(OnHandsIFFGetFaction);
        SubscribeLocalEvent<ItemIFFComponent, InventoryRelayedEvent<GetIFFFactionEvent>>(OnItemIFFGetFaction);
        SubscribeLocalEvent<GunIFFComponent, AmmoShotEvent>(OnGunIFFAmmoShot, before: [typeof(AttachableIFFSystem)]);
        SubscribeLocalEvent<GunIFFComponent, ExaminedEvent>(OnGunIFFExamined);
        SubscribeLocalEvent<ProjectileIFFComponent, PreventCollideEvent>(OnProjectileIFFPreventCollide);
    }

    private void OnUserIFFGetFaction(Entity<UserIFFComponent> ent, ref GetIFFFactionEvent args)
    {
        args.Faction ??= ent.Comp.Faction;
    }

    private void OnInventoryIFFGetFaction(Entity<InventoryComponent> ent, ref GetIFFFactionEvent args)
    {
        if (args.Faction != null)
            return;

        _inventory.RelayEvent(ent, ref args);
    }

    private void OnHandsIFFGetFaction(Entity<HandsComponent> ent, ref GetIFFFactionEvent args)
    {
        if (args.Faction != null)
            return;

        foreach (var held in _hands.EnumerateHeld((ent, ent)))
        {
            RaiseLocalEvent(held, ref args);
            if (args.Faction != null)
                break;
        }
    }

    private void OnItemIFFGetFaction(Entity<ItemIFFComponent> ent, ref InventoryRelayedEvent<GetIFFFactionEvent> args)
    {
        args.Args.Faction ??= ent.Comp.Faction;
    }

    private void OnGunIFFAmmoShot(Entity<GunIFFComponent> ent, ref AmmoShotEvent args)
    {
        GiveAmmoIFF(ent, ref args, ent.Comp.Intrinsic, ent.Comp.Enabled);
    }

    private void OnGunIFFExamined(Entity<GunIFFComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        using (args.PushGroup(nameof(GunIFFComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-examine-text-iff"));
        }
    }

    private void OnProjectileIFFPreventCollide(Entity<ProjectileIFFComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled ||
            ent.Comp.Faction is not { } faction)
        {
            return;
        }

        if (ent.Comp.Enabled && IsInFaction(args.OtherEntity, faction))
            args.Cancelled = true;

        if (HasComp<EntityIFFComponent>(args.OtherEntity) && IsInFaction(args.OtherEntity, faction))
            args.Cancelled = true;
    }

    /// <summary>
    ///     Gets the UserIFF faction of the user.
    /// </summary>
    public bool TryGetUserFaction(Entity<UserIFFComponent?> user, out EntProtoId<IFFFactionComponent> faction)
    {
        faction = default;
        if (!_userIFFQuery.Resolve(user, ref user.Comp, false) ||
            user.Comp.Faction is not { } userFaction)
            return false;

        faction = userFaction;
        return true;
    }

    /// <summary>
    ///     Gets the IFFFaction of the user. Includes the UserIFFComponent and any items on the given slot flags that have the ItemIFFComponent.
    /// </summary>
    public bool TryGetFaction(Entity<UserIFFComponent?> user, out EntProtoId<IFFFactionComponent> faction, SlotFlags slots = SlotFlags.IDCARD)
    {
        faction = default;
        if (!_userIFFQuery.Resolve(user, ref user.Comp, false))
            return false;

        var ev = new GetIFFFactionEvent(null, slots);
        RaiseLocalEvent(user, ref ev);

        if (ev.Faction is not { } newFaction)
            return false;

        faction = newFaction;
        return true;
    }

    public bool IsInFaction(Entity<UserIFFComponent?> user, EntProtoId<IFFFactionComponent> faction)
    {
        if (!_userIFFQuery.Resolve(user, ref user.Comp, false))
            return false;

        var ev = new GetIFFFactionEvent(null, SlotFlags.IDCARD);
        RaiseLocalEvent(user, ref ev);
        return ev.Faction == faction;
    }

    public void SetIdFaction(Entity<ItemIFFComponent> card, EntProtoId<IFFFactionComponent> faction)
    {
        card.Comp.Faction = faction;
        Dirty(card);
    }

    public void SetUserFaction(Entity<UserIFFComponent?> user, EntProtoId<IFFFactionComponent> faction)
    {
        user.Comp = EnsureComp<UserIFFComponent>(user);
        user.Comp.Faction = faction;
        Dirty(user);
    }

    public void SetIFFState(EntityUid ent, bool enabled)
    {
        if (TryComp<GunIFFComponent>(ent, out var comp))
        {
            comp.Enabled = enabled;
            Dirty(ent, comp);
        }
    }

    public void GiveAmmoIFF(EntityUid gun, ref AmmoShotEvent args, bool intrinsic, bool enabled)
    {
        EntityUid owner;
        if (intrinsic)
        {
            owner = gun;
        }
        else if (_container.TryGetOuterContainer(gun, Transform(gun), out var container))
        {
            owner = container.Owner;
        }
        else
        {
            return;
        }

        if (!_userIFFQuery.HasComp(owner))
        {
            return;
        }

        var ev = new GetIFFFactionEvent(null, SlotFlags.IDCARD);
        RaiseLocalEvent(owner, ref ev);

        if (ev.Faction is not { } id)
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            var iff = EnsureComp<ProjectileIFFComponent>(projectile);
            iff.Faction = id;
            iff.Enabled = enabled;
            Dirty(projectile, iff);
        }
    }

    public void GiveAmmoIFF(EntityUid uid, EntProtoId<IFFFactionComponent>? faction, bool enabled)
    {
        var projectileIFFComponent = EnsureComp<ProjectileIFFComponent>(uid);
        projectileIFFComponent.Faction = faction;
        projectileIFFComponent.Enabled = enabled;
    }
}
