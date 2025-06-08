using System.Linq;
using Content.Server._RMC14.LinkAccount;
using Content.Server.Administration.Logs;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.NamedItems;
using Content.Shared._RMC14.Sentry;
using Content.Shared._RMC14.Vendors;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Storage;

namespace Content.Server._RMC14.NamedItems;

public sealed class RMCNamedItemSystem : SharedRMCNamedItemSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;

    private EntityQuery<RMCNameItemOnVendComponent> _nameItemOnVendQuery;

    public override void Initialize()
    {
        base.Initialize();
        _nameItemOnVendQuery = GetEntityQuery<RMCNameItemOnVendComponent>();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<RMCUserNamedItemsComponent, RMCAutomatedVendedUserEvent>(OnAutomatedVendorUser);

        SubscribeLocalEvent<RMCNamedItemComponent, ComponentRemove>(OnItemRemove);
        SubscribeLocalEvent<RMCNamedItemComponent, EntityTerminatingEvent>(OnItemRemove);
        SubscribeLocalEvent<RMCNamedItemComponent, SentryUpgradedEvent>(OnSentryUpgraded);
        SubscribeLocalEvent<RMCNamedItemComponent, RMCArmorVariantCreatedEvent>(OnArmorVariantCreated);
        SubscribeLocalEvent<RMCNamedItemComponent, RefreshNameModifiersEvent>(OnItemRefreshNameModifiers);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (_linkAccount.GetPatron(ev.Player)?.Tier is not { NamedItems: true })
            return;

        var user = EnsureComp<RMCUserNamedItemsComponent>(ev.Mob);
        var named = ev.Profile.NamedItems;
        user.Names = new SharedRMCNamedItems(named.PrimaryGunName, named.SidearmName, named.HelmetName, named.ArmorName, named.SentryName);
    }

    private void OnAutomatedVendorUser(Entity<RMCUserNamedItemsComponent> ent, ref RMCAutomatedVendedUserEvent args)
    {
        if (_nameItemOnVendQuery.TryComp(args.Item, out var itemComp) &&
            TryNameItem(ent, args.Item, itemComp.Item))
        {
            return;
        }

        if (TryComp(args.Item, out StorageComponent? storage))
        {
            foreach (var item in storage.StoredItems.Keys.ToList())
            {
                if (_nameItemOnVendQuery.TryComp(item, out itemComp))
                    TryNameItem(ent, item, itemComp.Item);
            }
        }
    }

    private void OnItemRemove<T>(Entity<RMCNamedItemComponent> ent, ref T args)
    {
        if (TryComp(ent.Comp.User, out RMCUserNamedItemsComponent? user) &&
            ent.Comp.Type is { } type)
        {
            var typeInt = (int) type;
            if (typeInt >= 0 && typeInt < user.Entities.Length)
                user.Entities[typeInt] = null;
        }

        if (!TerminatingOrDeleted(ent))
            _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    private void OnSentryUpgraded(Entity<RMCNamedItemComponent> ent, ref SentryUpgradedEvent args)
    {
        if (!TryComp(args.OldSentry, out RMCNamedItemComponent? nameComp) ||
            nameComp.Name is not { } name ||
            nameComp.Type is not { } type)
        {
            return;
        }

        RenameItem(args.NewSentry, name, args.User, type);
    }

    private void OnArmorVariantCreated(Entity<RMCNamedItemComponent> ent, ref RMCArmorVariantCreatedEvent args)
    {
        if (!TryComp(args.Old, out RMCNamedItemComponent? old) ||
            old.User is not { } user ||
            old.Type is not { } type)
        {
            return;
        }

        RenameItem(args.New, old.Name, user, type);
    }

    private void OnItemRefreshNameModifiers(Entity<RMCNamedItemComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("rmc-patron-named-item", extraArgs: ("name", ent.Comp.Name));
    }

    protected override bool TryNameItem(Entity<RMCUserNamedItemsComponent> user, EntityUid item, RMCNamedItemType type)
    {
        var names = user.Comp.Names;
        string? name;
        switch (type)
        {
            case RMCNamedItemType.PrimaryGun:
                name = names.PrimaryGunName;
                break;
            case RMCNamedItemType.Sidearm:
                name = names.SidearmName;
                break;
            case RMCNamedItemType.Helmet:
                name = names.HelmetName;
                break;
            case RMCNamedItemType.Armor:
                name = names.ArmorName;
                break;
            case RMCNamedItemType.Sentry:
                name = names.SentryName;
                break;
            default:
                Log.Error($"Unknown named item type found by {ToPrettyString(user)}: {item}");
                name = null;
                break;
        }

        if (name == null)
            return false;

        RenameItem(item, name, (user, user), type);
        return true;
    }

    private void RenameItem(Entity<RMCNamedItemComponent?> item, string prefix, Entity<RMCUserNamedItemsComponent?> player, RMCNamedItemType type)
    {
        prefix = prefix.Trim();
        if (string.IsNullOrWhiteSpace(prefix))
            return;

        player.Comp = EnsureComp<RMCUserNamedItemsComponent>(player);
        var typeInt = (int) type;
        if (typeInt >= 0 && typeInt < player.Comp.Entities.Length)
        {
            if (player.Comp.Entities[typeInt] is { } old)
                RemComp<RMCNamedItemComponent>(old);

            player.Comp.Entities[typeInt] = item;
        }

        item.Comp = EnsureComp<RMCNamedItemComponent>(item);
        item.Comp.User = player;
        item.Comp.Type = type;
        item.Comp.Name = prefix;
        _nameModifier.RefreshNameModifiers(item.Owner);
        _adminLogs.Add(LogType.RMCNamedItem, $"{ToPrettyString(player):player} named item {ToPrettyString(item):item} with name {prefix}");
    }
}
