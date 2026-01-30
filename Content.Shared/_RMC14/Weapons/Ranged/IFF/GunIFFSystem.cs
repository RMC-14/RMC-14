using System.Collections.Generic;
using System.Linq;
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
    private readonly HashSet<EntProtoId<IFFFactionComponent>> _factionBuffer = new();

    public override void Initialize()
    {
        _userIFFQuery = GetEntityQuery<UserIFFComponent>();

        SubscribeLocalEvent<UserIFFComponent, GetIFFFactionEvent>(OnUserIFFGetFaction);
        SubscribeLocalEvent<InventoryComponent, GetIFFFactionEvent>(OnInventoryIFFGetFaction);
        SubscribeLocalEvent<HandsComponent, GetIFFFactionEvent>(OnHandsIFFGetFaction);
        SubscribeLocalEvent<ItemIFFComponent, InventoryRelayedEvent<GetIFFFactionEvent>>(OnItemIFFGetFaction);
        SubscribeLocalEvent<GunIFFComponent, AmmoShotEvent>(OnGunIFFAmmoShot, before: new[] { typeof(AttachableIFFSystem) });
        SubscribeLocalEvent<GunIFFComponent, ExaminedEvent>(OnGunIFFExamined);
        SubscribeLocalEvent<ProjectileIFFComponent, PreventCollideEvent>(OnProjectileIFFPreventCollide);
    }

    private void OnUserIFFGetFaction(Entity<UserIFFComponent> ent, ref GetIFFFactionEvent args)
    {
        args.Factions.UnionWith(ent.Comp.Factions);
    }

    private void OnInventoryIFFGetFaction(Entity<InventoryComponent> ent, ref GetIFFFactionEvent args)
    {
        _inventory.RelayEvent(ent, ref args);
    }

    private void OnHandsIFFGetFaction(Entity<HandsComponent> ent, ref GetIFFFactionEvent args)
    {
        foreach (var held in _hands.EnumerateHeld((ent, ent)))
        {
            RaiseLocalEvent(held, ref args);
        }
    }

    private void OnItemIFFGetFaction(Entity<ItemIFFComponent> ent, ref InventoryRelayedEvent<GetIFFFactionEvent> args)
    {
        if (ent.Comp.Factions.Count > 0)
            args.Args.Factions.UnionWith(ent.Comp.Factions);
    }

    private void OnGunIFFAmmoShot(Entity<GunIFFComponent> ent, ref AmmoShotEvent args)
    {
        GiveAmmoIFF(ent.Owner, ref args, ent.Comp.Intrinsic, ent.Comp.Enabled);
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
        if (args.Cancelled)
            return;

        if (!ent.Comp.Enabled)
            return;

        foreach (var faction in ent.Comp.Factions)
        {
            if (IsInFaction(args.OtherEntity, faction))
            {
                args.Cancelled = true;
                return;
            }
        }
    }

    /// <summary>
    ///     Gets the UserIFF faction of the user.
    /// </summary>
    public bool TryGetUserFaction(Entity<UserIFFComponent?> user, out EntProtoId<IFFFactionComponent> faction)
    {
        faction = default;
        if (!_userIFFQuery.Resolve(user, ref user.Comp, false) ||
            user.Comp.Factions.Count == 0)
            return false;

        faction = user.Comp.Factions.First();
        return true;
    }

    /// <summary>
    ///     Gets the IFFFaction of the user. Includes the UserIFFComponent and any items on the given slot flags that have the ItemIFFComponent.
    /// </summary>
    public bool TryGetFaction(Entity<UserIFFComponent?> user, out EntProtoId<IFFFactionComponent> faction, SlotFlags slots = SlotFlags.IDCARD)
    {
        faction = default;
        var buffer = new HashSet<EntProtoId<IFFFactionComponent>>();
        if (!TryGetFactions(user, buffer, slots))
            return false;

        faction = buffer.First();
        return true;
    }

    public bool TryGetFactions(Entity<UserIFFComponent?> user, HashSet<EntProtoId<IFFFactionComponent>> factions, SlotFlags slots = SlotFlags.IDCARD)
    {
        factions.Clear();
        if (!_userIFFQuery.Resolve(user, ref user.Comp, false))
            return false;

        factions.UnionWith(user.Comp.Factions);

        var ev = new GetIFFFactionEvent(slots, new HashSet<EntProtoId<IFFFactionComponent>>());
        RaiseLocalEvent(user, ref ev);

        factions.UnionWith(ev.Factions);

        if (factions.Count == 0)
            return false;

        if (_userIFFQuery.Resolve(user, ref user.Comp, false))
            user.Comp.Factions.UnionWith(ev.Factions);

        return true;
    }

    public bool IsInFaction(Entity<UserIFFComponent?> user, EntProtoId<IFFFactionComponent> faction)
    {
        var uid = user.Owner;

        if (_userIFFQuery.Resolve(user, ref user.Comp, false))
        {
            if (user.Comp.Factions.Count > 0 && user.Comp.Factions.Contains(faction))
                return true;
        }

        var ev = new GetIFFFactionEvent(SlotFlags.IDCARD, new HashSet<EntProtoId<IFFFactionComponent>>());
        RaiseLocalEvent(uid, ref ev);

        if (ev.Factions.Count > 0 && ev.Factions.Contains(faction))
            return true;

        return false;
    }

    public bool IsInFaction(EntityUid uid, EntProtoId<IFFFactionComponent> faction)
    {
        return IsInFaction((uid, null), faction);
    }

    public void SetIdFaction(Entity<ItemIFFComponent> card, EntProtoId<IFFFactionComponent> faction)
    {
        card.Comp.Factions.Clear();
        card.Comp.Factions.Add(faction);
        Dirty(card);
    }

    public void SetUserFaction(Entity<UserIFFComponent?> user, EntProtoId<IFFFactionComponent> faction)
    {
        user.Comp = EnsureComp<UserIFFComponent>(user);
        user.Comp.Factions.Clear();
        user.Comp.Factions.Add(faction);
        Dirty(user);
    }

    public void AddUserFaction(Entity<UserIFFComponent?> user, EntProtoId<IFFFactionComponent> faction)
    {
        user.Comp = EnsureComp<UserIFFComponent>(user);
        user.Comp.Factions.Add(faction);
        Dirty(user);
    }

    public void RemoveUserFaction(Entity<UserIFFComponent?> user, EntProtoId<IFFFactionComponent> faction)
    {
        if (!_userIFFQuery.Resolve(user, ref user.Comp, false))
            return;

        user.Comp.Factions.Remove(faction);
        Dirty(user);
    }

    public void ClearUserFactions(Entity<UserIFFComponent?> user)
    {
        user.Comp = EnsureComp<UserIFFComponent>(user);
        user.Comp.Factions.Clear();
        Dirty(user);
    }

    public void SetIFFState(EntityUid ent, bool enabled)
    {
        if (!TryComp<GunIFFComponent>(ent, out var comp))
            return;

        comp.Enabled = enabled;
        Dirty(ent, comp);
    }

    public void EnableIntrinsicIFF(EntityUid ent)
    {
        var comp = EnsureComp<GunIFFComponent>(ent);
        comp.Intrinsic = true;
        comp.Enabled = true;
        Dirty(ent, comp);
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

        if (!_userIFFQuery.TryComp(owner, out var ownerIFF))
            return;

        _factionBuffer.Clear();
        if (!TryGetFactions((owner, ownerIFF), _factionBuffer, SlotFlags.IDCARD))
            return;

        var hasAnyFaction = enabled && _factionBuffer.Count > 0;

        foreach (var projectile in args.FiredProjectiles)
        {
            var iff = EnsureComp<ProjectileIFFComponent>(projectile);

            iff.Factions.Clear();
            foreach (var faction in _factionBuffer)
            {
                iff.Factions.Add(faction);
            }

            iff.Enabled = hasAnyFaction;
            Dirty(projectile, iff);
        }
    }

    public void GiveAmmoIFF(EntityUid uid, EntProtoId<IFFFactionComponent>? faction, bool enabled)
    {
        var projectileIFFComponent = EnsureComp<ProjectileIFFComponent>(uid);
        projectileIFFComponent.Factions.Clear();
        if (faction is { } add)
            projectileIFFComponent.Factions.Add(add);

        projectileIFFComponent.Enabled = enabled && projectileIFFComponent.Factions.Count > 0;
        Dirty(uid, projectileIFFComponent);
    }

    public void GiveAmmoMultiFactionIFF(EntityUid uid, HashSet<EntProtoId<IFFFactionComponent>> factions, bool enabled)
    {
        var projectileIFFComponent = EnsureComp<ProjectileIFFComponent>(uid);
        projectileIFFComponent.Factions.Clear();

        foreach (var faction in factions)
            projectileIFFComponent.Factions.Add(faction);

        projectileIFFComponent.Enabled = enabled && projectileIFFComponent.Factions.Count > 0;
        Dirty(uid, projectileIFFComponent);
    }
}
