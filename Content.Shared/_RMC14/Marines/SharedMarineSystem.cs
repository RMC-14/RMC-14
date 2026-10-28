using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines;

public abstract class SharedMarineSystem : EntitySystem
{
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetConfigurationManager _netConfig = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;

    private readonly HashSet<EntProtoId<IFFFactionComponent>> _attackedFactions = [];
    private readonly HashSet<EntProtoId<IFFFactionComponent>> _attackerFactions = [];

    public override void Initialize()
    {
        base.Initialize();

        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<MarineComponent, CheckMeleeTargetEvent>(OnMarineCheckMeleeTarget);
        SubscribeLocalEvent<MarineComponent, GetMarineIconEvent>(OnMarineGetIcon);

        SubscribeLocalEvent<GrantMarineIconsComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantMarineIconsComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<GrantMarineIconsComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        GiveMarineHud(args.Equipee, ent.Comp.Factions, ent.Comp.BypassFactionIcons);
    }

    private void OnGotUnequipped(Entity<GrantMarineIconsComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        if (!_inventory.TryGetInventoryEntity<GrantMarineIconsComponent>(args.Equipee, out _))
            RemCompDeferred<ShowMarineIconsComponent>(args.Equipee);
    }

    private void OnMarineGetIcon(Entity<MarineComponent> marine, ref GetMarineIconEvent args)
    {
        if (marine.Comp.Icon is { } icon)
            args.Icon = icon;
    }

    private void OnMarineCheckMeleeTarget(Entity<MarineComponent> marine, ref CheckMeleeTargetEvent args)
    {
        if (args.Skip)
            return;

        // If you're trying to attack yourself, allow if if you have damage yourself enabled, otherwise don't allow it at all.
        if (marine.Owner == args.Target)
        {
            if (args.Disarm
                || !TryComp<ActorComponent>(marine, out var actor)
                || !_netConfig.GetClientCVar(actor.PlayerSession.Channel, RMCCVars.RMCDamageYourself))
            {
                args.Skip = true;
            }

            return;
        }

        // Marines can only attack damageable things
        if (!args.Disarm && !_damageableQuery.HasComp(args.Target))
        {
            args.Skip = true;
            return;
        }

        // Marines can only disarm mobs
        if (args.Disarm && !_mobStateQuery.HasComp(args.Target))
        {
            args.Skip = true;
            return;
        }

        if (args.Defer)
            return;

        // Marines prefer not to attack entities they share an IFF faction with
        if (!args.Disarm
            && _gunIFF.TryGetFactions(args.Target, _attackedFactions)
            && _gunIFF.TryGetFactions(marine.Owner, _attackerFactions)
            && _attackerFactions.Overlaps(_attackedFactions))
        {
            args.Defer = true;
        }
    }

    public GetMarineIconEvent GetMarineIcon(EntityUid uid)
    {
        var ev = new GetMarineIconEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev;
    }

    public void SetMarineIcon(EntityUid marine, SpriteSpecifier specifier)
    {
        if (!TryComp<MarineComponent>(marine, out var comp))
            return;

        comp.Icon = _serialization.CreateCopy(specifier, notNullableOverride: true);
        Dirty(marine, comp);
    }

    public void ClearMarineIcon(EntityUid marine)
    {
        if (TryComp<MarineComponent>(marine, out var comp))
        {
            comp.Icon = null;
            Dirty(marine, comp);
        }
    }

    public void MakeMarine(EntityUid uid, SpriteSpecifier? icon)
    {
        var marine = EnsureComp<MarineComponent>(uid);
        marine.Icon = _serialization.CreateCopy(icon);
        Dirty(uid, marine);
    }

    public void ClearIcon(Entity<MarineComponent> marine)
    {
        marine.Comp.Icon = null;
        Dirty(marine);
    }

    public Dictionary<ProtoId<NpcFactionPrototype>, SpriteSpecifier>? GetFactionIcons(EntityUid uid)
    {
        if (TryComp<MarineComponent>(uid, out var marine))
            return marine.GenericFactionIcons;

        return null;
    }

    public void GiveMarineHud(EntityUid uid, List<ProtoId<NpcFactionPrototype>>? faction, bool bypassIcons)
    {
        var icons = EnsureComp<ShowMarineIconsComponent>(uid);
        icons.Factions = faction;
        icons.BypassFactionIcons = bypassIcons;
        Dirty(uid, icons);
    }
}
