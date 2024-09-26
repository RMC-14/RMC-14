using System.Linq;
using Content.Server._RMC14.LinkAccount;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RMC14.NamedItems;
using Content.Shared._RMC14.Sentry;
using Content.Shared._RMC14.Vendors;
using Content.Shared.Database;
using Content.Shared.Storage;

namespace Content.Server._RMC14.NamedItems;

public sealed class RMCNamedItemSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private EntityQuery<RMCNameItemOnVendComponent> _nameItemOnVendQuery;

    public override void Initialize()
    {
        _nameItemOnVendQuery = GetEntityQuery<RMCNameItemOnVendComponent>();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<RMCUserNamedItemsComponent, RMCAutomatedVendedUserEvent>(OnAutomatedVenderUser);
        SubscribeLocalEvent<RMCNameItemOnVendComponent, SentryUpgradedEvent>(OnSentryUpgraded);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (_linkAccount.GetPatron(ev.Player)?.Tier is not { NamedItems: true })
            return;

        var user = EnsureComp<RMCUserNamedItemsComponent>(ev.Mob);
        var named = ev.Profile.NamedItems;
        user.Names = new SharedRMCNamedItems(named.PrimaryGunName, named.SidearmName, named.HelmetName, named.ArmorName, named.SentryName);
    }

    private void OnAutomatedVenderUser(Entity<RMCUserNamedItemsComponent> ent, ref RMCAutomatedVendedUserEvent args)
    {
        if (_nameItemOnVendQuery.TryComp(args.Item, out var itemComp) &&
            TryNameItem(ent, (args.Item, itemComp)))
        {
            return;
        }

        if (TryComp(args.Item, out StorageComponent? storage))
        {
            foreach (var item in storage.StoredItems.Keys.ToList())
            {
                if (_nameItemOnVendQuery.TryComp(item, out itemComp))
                    TryNameItem(ent, (item, itemComp));
            }
        }
    }

    private void OnSentryUpgraded(Entity<RMCNameItemOnVendComponent> ent, ref SentryUpgradedEvent args)
    {
        if (!TryComp(args.OldSentry, out RMCNameItemOnVendComponent? nameComp) ||
            nameComp.Name is not { } name)
        {
            return;
        }

        var newNameComp = EnsureComp<RMCNameItemOnVendComponent>(args.NewSentry);
        newNameComp.Name = name;
        NameItem(args.User, args.NewSentry, name);
    }

    private bool TryNameItem(Entity<RMCUserNamedItemsComponent> ent, Entity<RMCNameItemOnVendComponent> item)
    {
        var names = ent.Comp.Names;
        string? name;
        switch (item.Comp.Item)
        {
            case RMCNamedItemType.PrimaryGun:
                name = names.PrimaryGunName;
                ent.Comp.Names = names with { PrimaryGunName = null };
                break;
            case RMCNamedItemType.Sidearm:
                name = names.SidearmName;
                ent.Comp.Names = names with { SidearmName = null };
                break;
            case RMCNamedItemType.Helmet:
                name = names.HelmetName;
                ent.Comp.Names = names with { HelmetName = null };
                break;
            case RMCNamedItemType.Armor:
                name = names.ArmorName;
                ent.Comp.Names = names with { ArmorName = null };
                break;
            case RMCNamedItemType.Sentry:
                name = names.SentryName;
                ent.Comp.Names = names with { SentryName = null };
                break;
            default:
                Log.Error($"Unknown named item type found by {ToPrettyString(ent)}: {item}");
                name = null;
                break;
        }

        if (name == null)
            return false;

        NameItem(ent, item, name);
        item.Comp.Name = name;
        return true;
    }

    private bool NameItem(EntityUid player, EntityUid item, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        name = name.Trim();
        var metaData = MetaData(item);
        var newName = $"'{name}' {metaData.EntityName}";
        _metaData.SetEntityName(item, newName, metaData);
        _adminLogs.Add(LogType.RMCNamedItem, $"{ToPrettyString(player):player} named item {ToPrettyString(item):item} with name {newName}");
        return true;
    }
}
