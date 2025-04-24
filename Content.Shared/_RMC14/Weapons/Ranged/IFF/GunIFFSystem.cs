using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

public sealed class GunIFFSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    private EntityQuery<NpcFactionMemberComponent> _userIFFQuery;

    public override void Initialize()
    {
        _userIFFQuery = GetEntityQuery<NpcFactionMemberComponent>();

        SubscribeLocalEvent<NpcFactionMemberComponent, GetIFFFactionEvent>(OnUserIFFGetFaction);
        SubscribeLocalEvent<InventoryComponent, GetIFFFactionEvent>(OnInventoryIFFGetFaction);
        SubscribeLocalEvent<HandsComponent, GetIFFFactionEvent>(OnHandsIFFGetFaction);
        SubscribeLocalEvent<ItemIFFComponent, InventoryRelayedEvent<GetIFFFactionEvent>>(OnItemIFFGetFaction);
        SubscribeLocalEvent<GunIFFComponent, AmmoShotEvent>(OnGunIFFAmmoShot, before: [typeof(AttachableIFFSystem)]);
        SubscribeLocalEvent<GunIFFComponent, ExaminedEvent>(OnGunIFFExamined);
        SubscribeLocalEvent<ProjectileIFFComponent, PreventCollideEvent>(OnProjectileIFFPreventCollide);
    }

    private void OnUserIFFGetFaction(Entity<NpcFactionMemberComponent> ent, ref GetIFFFactionEvent args)
    {
        args.Factions ??= ent.Comp.Factions;
    }

    private void OnInventoryIFFGetFaction(Entity<InventoryComponent> ent, ref GetIFFFactionEvent args)
    {
        if (args.Factions != null)
            return;

        _inventory.RelayEvent(ent, ref args);
    }

    private void OnHandsIFFGetFaction(Entity<HandsComponent> ent, ref GetIFFFactionEvent args)
    {
        if (args.Factions != null)
            return;

        foreach (var (_, hand) in ent.Comp.Hands)
        {
            if (hand.HeldEntity is not { } held)
                continue;

            RaiseLocalEvent(held, ref args);
            if (args.Factions != null)
                break;
        }
    }

    private void OnItemIFFGetFaction(Entity<ItemIFFComponent> ent, ref InventoryRelayedEvent<GetIFFFactionEvent> args)
    {
        if (ent.Comp.Faction is { } faction)
            args.Args.Factions ??= new([faction]);
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
            ent.Comp.Factions is not { } factions)
        {
            return;
        }

        if (ent.Comp.Enabled && IsInFaction(args.OtherEntity, factions))
            args.Cancelled = true;

        if (HasComp<EntityIFFComponent>(args.OtherEntity) && IsInFaction(args.OtherEntity, factions))
            args.Cancelled = true;
    }

    public bool TryGetUserFactions(Entity<NpcFactionMemberComponent?> user, out HashSet<ProtoId<NpcFactionPrototype>> factions)
    {
        factions = [];
        if (!_userIFFQuery.Resolve(user, ref user.Comp, false) ||
            user.Comp.Factions is not { } userFaction)
            return false;

        factions = userFaction;
        return true;
    }

    public bool IsInFaction(Entity<NpcFactionMemberComponent?> user, HashSet<ProtoId<NpcFactionPrototype>> factions)
    {
        if (!_userIFFQuery.Resolve(user, ref user.Comp, false))
            return false;

        var ev = new GetIFFFactionEvent(null, SlotFlags.IDCARD);
        RaiseLocalEvent(user, ref ev);
        if (ev.Factions is not { } eventFactions)
            return false;

        return factions.Overlaps(eventFactions);
    }

    public void SetUserFactions(Entity<NpcFactionMemberComponent?> user, HashSet<ProtoId<NpcFactionPrototype>> factions)
    {
        _faction.ClearFactions(user);
        _faction.AddFactions(user, factions);
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
        else if (_container.TryGetContainingContainer((gun, null), out var container))
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

        if (ev.Factions is not { } id)
            return;

        var friendly = _faction.GetFriendlyFactions(id);
        foreach (var projectile in args.FiredProjectiles)
        {
            var iff = EnsureComp<ProjectileIFFComponent>(projectile);
            iff.Factions = friendly;
            iff.Enabled = enabled;
            Dirty(projectile, iff);
        }
    }

    public void GiveAmmoIFF(EntityUid uid, HashSet<ProtoId<NpcFactionPrototype>> factions, bool enabled)
    {
        var projectileIFFComponent = EnsureComp<ProjectileIFFComponent>(uid);
        projectileIFFComponent.Factions = factions;
        projectileIFFComponent.Enabled = enabled;
    }
}
