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
using Content.Shared.Verbs;

namespace Content.Server._RMC14.NamedItems;

public sealed class RMCNamedItemSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;

    private EntityQuery<RMCNameItemOnVendComponent> _nameItemOnVendQuery;

    public override void Initialize()
    {
        _nameItemOnVendQuery = GetEntityQuery<RMCNameItemOnVendComponent>();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<RMCUserNamedItemsComponent, RMCAutomatedVendedUserEvent>(OnAutomatedVenderUser);
        SubscribeLocalEvent<RMCNameItemOnVendComponent, SentryUpgradedEvent>(OnSentryUpgraded);

        SubscribeLocalEvent<RMCNamedItemComponent, RMCArmorVariantCreatedEvent>(OnArmorVariantCreated);
        SubscribeLocalEvent<RMCNamedItemComponent, RefreshNameModifiersEvent>(OnItemRefreshNameModifiers);

        SubscribeLocalEvent<RMCNameItemOnVendComponent, GetVerbsEvent<AlternativeVerb>>(AddNameVerb);

    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (_linkAccount.GetPatron(ev.Player)?.Tier is not { NamedItems: true })
            return;

        var user = EnsureComp<RMCUserNamedItemsComponent>(ev.Mob);
        var named = ev.Profile.NamedItems;
        user.Names = new SharedRMCNamedItems(named.PrimaryGunName, named.SidearmName, named.HelmetName, named.ArmorName, named.SentryName);
        user.NameOnDispense = ev.Profile.AutoItemName;
    }

    private void OnAutomatedVenderUser(Entity<RMCUserNamedItemsComponent> ent, ref RMCAutomatedVendedUserEvent args)
    {
        if (!ent.Comp.NameOnDispense)
            return;

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

private void AddNameVerb(EntityUid uid, RMCNameItemOnVendComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.User == null || !TryComp<RMCUserNamedItemsComponent>(args.User, out var userNamedItem))
            return;

        if (!_nameItemOnVendQuery.TryComp(uid, out var itemComp)|| !TryNameItem((args.User, userNamedItem), (uid, itemComp), true) || HasComp<RMCNamedItemComponent>(uid))
            return;

        var verb = new AlternativeVerb()
        {
            Act = () => TryNameItem((args.User, userNamedItem), (uid, itemComp)),
            IconEntity = GetNetEntity(uid),
            Text = Loc.GetString("named-item-verb-text"),
            Message = Loc.GetString("named-item-verb-message")
        };
        args.Verbs.Add(verb);
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

    private void OnArmorVariantCreated(Entity<RMCNamedItemComponent> ent, ref RMCArmorVariantCreatedEvent args)
    {
        if (!TryComp(args.Old, out RMCNamedItemComponent? old))
            return;

        var namedNew = EnsureComp<RMCNamedItemComponent>(args.New);
        namedNew.Name = old.Name;

        _nameModifier.RefreshNameModifiers(args.New);
    }

    private void OnItemRefreshNameModifiers(Entity<RMCNamedItemComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("rmc-patron-named-item", extraArgs: ("name", ent.Comp.Name));
    }

    private bool TryNameItem(Entity<RMCUserNamedItemsComponent> ent, Entity<RMCNameItemOnVendComponent> item, bool CheckOnly = false)
    {
        var names = ent.Comp.Names;
        string? name;
        switch (item.Comp.Item)
        {
            case RMCNamedItemType.PrimaryGun:
                name = names.PrimaryGunName;
                if (!CheckOnly)
                    ent.Comp.Names = names with { PrimaryGunName = null };
                break;
            case RMCNamedItemType.Sidearm:
                name = names.SidearmName;
                if (!CheckOnly)
                    ent.Comp.Names = names with { SidearmName = null };
                break;
            case RMCNamedItemType.Helmet:
                name = names.HelmetName;
                if (!CheckOnly)
                    ent.Comp.Names = names with { HelmetName = null };
                break;
            case RMCNamedItemType.Armor:
                name = names.ArmorName;
                if (!CheckOnly)
                    ent.Comp.Names = names with { ArmorName = null };
                break;
            case RMCNamedItemType.Sentry:
                name = names.SentryName;
                if (!CheckOnly)
                    ent.Comp.Names = names with { SentryName = null };
                break;
            default:
                Log.Error($"Unknown named item type found by {ToPrettyString(ent)}: {item}");
                name = null;
                break;
        }

        if (name == null)
            return false;
        if (!CheckOnly)
        {
            NameItem(ent, item, name);
            item.Comp.Name = name;
        }
        return true;
    }

    private void NameItem(EntityUid player, EntityUid item, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        name = name.Trim();
        var metaData = MetaData(item);
        var newName = $"'{name}' {metaData.EntityName}";

        var named = EnsureComp<RMCNamedItemComponent>(item);
        named.Name = name;
        _nameModifier.RefreshNameModifiers(item);

        _adminLogs.Add(LogType.RMCNamedItem, $"{ToPrettyString(player):player} named item {ToPrettyString(item):item} with name {newName}");
    }
}
