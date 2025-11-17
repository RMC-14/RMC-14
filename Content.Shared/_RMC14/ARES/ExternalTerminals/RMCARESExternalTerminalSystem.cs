using System.Collections.Immutable;
using System.Linq;
using Content.Shared._RMC14.Access;
using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.ARES.Tabs;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Prototypes;
using Content.Shared.UserInterface;
using Microsoft.VisualBasic;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._RMC14.ARES.ExternalTerminals;

public sealed class RMCARESExternalTerminalSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly RMCARESCoreSystem _core = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly EntProtoId<RMCARESLogTypeComponent> CoreLog = "ARESTabARESLogs";
    private static readonly EntProtoId<RMCARESTabCategoryComponent> LogCat = "ARESCategoryLogs";
    private static readonly int LogsShown = 12;

    public HashSet<EntProtoId<RMCARESLogTypeComponent>> LogTypes { get; private set; } = [];
    public HashSet<EntProtoId<RMCARESTabCategoryComponent>> TabCategories { get; private set; } = [];
    public override void Initialize()
    {
        Subs.BuiEvents<RMCARESExternalTerminalComponent>(RMCARESExternalTerminalUIKey.Key,
            subs =>
            {
                subs.Event<RMCARESExternalLogin>(OnExternalLogin);
                subs.Event<RMCARESExternalLogout>(OnExternalLogout);
                subs.Event<RMCARESExternalShowLogs>(OnExternalShowLogs);
            });

        SubscribeLocalEvent<RMCARESExternalTerminalComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCARESExternalTerminalComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        ReloadTabs();
    }

    private void OnExternalShowLogs(Entity<RMCARESExternalTerminalComponent> ent, ref RMCARESExternalShowLogs args)
    {
        ent.Comp.Logs.Clear();
        if (_net.IsClient)
            return;

        if (args.Type == null)
            return;

        if (!ent.Comp.ARESCore.HasValue ||
            !_core.PullARESLogs(ent.Comp.ARESCore.Value, args.Type, out var logs) || logs == null)
        {
            ent.Comp.LogsLength = 1;
            ent.Comp.Logs.Add("No logs to display");
            Dirty(ent);
            return;
        }

        ent.Comp.LogsLength = logs.Count;

        ent.Comp.Logs = logs.SkipLast(args.Index*LogsShown).TakeLast(LogsShown).Reverse().ToList();
        Dirty(ent);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            ReloadTabs();
    }

    private void ReloadTabs()
    {
        LogTypes = [];
        TabCategories = [];

        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.HasComponent<RMCARESLogTypeComponent>())
            {
                LogTypes.Add(entity.ID);
                continue;
            }

            if (entity.HasComponent<RMCARESTabCategoryComponent>())
            {
                TabCategories.Add(entity.ID);
                continue;
            }
        }
    }

    private void OnExternalLogout(Entity<RMCARESExternalTerminalComponent> ent, ref RMCARESExternalLogout args)
    {
        if (Prototype(ent) is not { }  proto)
            return;

        proto.TryGetComponent<RMCARESExternalTerminalComponent>(out var comp, _componentFactory);
        var refComp = ent.Comp;
        _serializationManager.CopyTo(comp, ref refComp);

        Dirty(ent);
    }

    private void OnExternalLogin(Entity<RMCARESExternalTerminalComponent> ent, ref RMCARESExternalLogin args)
    {
        SetAres(ent);
        if (!_idCard.TryFindIdCard(args.Actor, out var idCard) || !TryComp<AccessComponent>(idCard, out var access) || !TryComp<ItemIFFComponent>(idCard, out var itemIff) || idCard.Comp.FullName == null || idCard.Comp._jobTitle == null || itemIff.Faction != ent.Comp.Faction)
            return;

        _core.CreateARESLog(ent.Comp.Faction, CoreLog, $"{idCard.Comp.FullName}'s ID card was used to log into the ARES system.");

        ent.Comp.LoggedIn = true;
        ent.Comp.Accesses = access.Tags;
        ent.Comp.LoggedInUser = $"{idCard.Comp.FullName} ({idCard.Comp._jobTitle})";

        // logs are special...
        if (ent.Comp.ShowsLogs)
        {
            ent.Comp.ShownCategories.Add(LogCat);

            //This compares the stored ID card data and determines what log types you can view.
            foreach (var logType in LogTypes)
            {
                var logPermission = logType.Get(_prototypes, _componentFactory).Permissions;
                if (logPermission == null || logPermission.Count == 0)
                {
                    ent.Comp.ShownLogs.Add(logType);
                    continue;
                }

                foreach (var permission in ent.Comp.Accesses)
                {
                    if (!logPermission.Contains(permission))
                        continue;

                    ent.Comp.ShownLogs.Add(logType);
                    break;
                }
            }
        }
        Dirty(ent);
    }

    private void OnBeforeActivatableUIOpen(Entity<RMCARESExternalTerminalComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        SetAres(ent);
    }

    private void OnMapInit(Entity<RMCARESExternalTerminalComponent> ent, ref MapInitEvent args)
    {
        SetAres(ent);
    }

    private void SetAres(Entity<RMCARESExternalTerminalComponent> ent)
    {
        if (ent.Comp.ARESCore == null)
        {
            _core.TryGetARES(ent.Comp.Faction, out var ares);
            if (ares != null)
            {
                ent.Comp.ARESCore = ares.Value;
                Dirty(ent);
            }
        }
    }
}
