using Content.Server._RMC14.LinkAccount;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared._RMC14.NamedItems;
using Content.Shared._RMC14.Vendors;
using Content.Shared.Database;

namespace Content.Server._RMC14.NamedItems;

public sealed class RMCNamedItemSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;

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
        if (!_nameItemOnVendQuery.TryComp(args.Item, out var item))
            return;

        var names = ent.Comp.Names;
        switch (item.Item)
        {
            case RMCNamedItemType.PrimaryGun:
                NameItem(ent, args.Item, names.PrimaryGunName);
                ent.Comp.Names = names with { PrimaryGunName = null };
                break;
            case RMCNamedItemType.Sidearm:
                NameItem(ent, args.Item, names.SidearmName);
                ent.Comp.Names = names with { SidearmName = null };
                break;
            case RMCNamedItemType.Helmet:
                NameItem(ent, args.Item, names.HelmetName);
                ent.Comp.Names = names with { HelmetName = null };
                break;
            case RMCNamedItemType.Armor:
                NameItem(ent, args.Item, names.ArmorName);
                ent.Comp.Names = names with { ArmorName = null };
                break;
            default:
                Log.Error($"Unknown named item type found by {ToPrettyString(ent)}: {item.Item}");
                break;
        }
    }

    private void NameItem(EntityUid player, EntityUid item, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        name = name.Trim();
        var metaData = MetaData(item);
        var newName = $"'{name}' {metaData.EntityName}";
        _metaData.SetEntityName(item, newName, metaData);
        _adminLogs.Add(LogType.RMCNamedItem, $"{ToPrettyString(player):player} named item {ToPrettyString(item):item} with name {newName}");
    }
}
