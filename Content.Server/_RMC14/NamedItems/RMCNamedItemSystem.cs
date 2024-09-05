using System.Linq;
using Content.Server._RMC14.LinkAccount;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RMC14.NamedItems;
using Content.Shared._RMC14.Vendors;
using Content.Shared.Database;
using Content.Shared.Storage;

namespace Content.Server._RMC14.NamedItems;

public sealed class RMCNamedItemSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly StorageSystem _storage = default!;

    private EntityQuery<RMCNameItemOnVendComponent> _nameItemOnVendQuery;

    public override void Initialize()
    {
        _nameItemOnVendQuery = GetEntityQuery<RMCNameItemOnVendComponent>();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<RMCUserNamedItemsComponent, RMCAutomatedVendedUserEvent>(OnAutomatedVenderUser);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (_linkAccount.GetPatron(ev.Player)?.Tier is not { NamedItems: true })
            return;

        var user = EnsureComp<RMCUserNamedItemsComponent>(ev.Mob);
        var named = ev.Profile.NamedItems;
        user.Names = new SharedRMCNamedItems(named.PrimaryGunName, named.SidearmName, named.HelmetName, named.ArmorName);
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

    private bool TryNameItem(Entity<RMCUserNamedItemsComponent> ent, Entity<RMCNameItemOnVendComponent> item)
    {
        var names = ent.Comp.Names;
        bool named;
        switch (item.Comp.Item)
        {
            case RMCNamedItemType.PrimaryGun:
                named = NameItem(ent, item, names.PrimaryGunName);
                ent.Comp.Names = names with { PrimaryGunName = null };
                break;
            case RMCNamedItemType.Sidearm:
                named = NameItem(ent, item, names.SidearmName);
                ent.Comp.Names = names with { SidearmName = null };
                break;
            case RMCNamedItemType.Helmet:
                named = NameItem(ent, item, names.HelmetName);
                ent.Comp.Names = names with { HelmetName = null };
                break;
            case RMCNamedItemType.Armor:
                named = NameItem(ent, item, names.ArmorName);
                ent.Comp.Names = names with { ArmorName = null };
                break;
            default:
                Log.Error($"Unknown named item type found by {ToPrettyString(ent)}: {item}");
                named = false;
                break;
        }

        return named;
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
