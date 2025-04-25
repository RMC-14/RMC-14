using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids.Devour;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._RMC14.Xenonids.Construction.Tunnel;

public sealed partial class XenoTunnelSystem : SharedXenoTunnelSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruct = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public int NextTempTunnelId { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoDigTunnelActionEvent>(OnCreateTunnel);
        SubscribeLocalEvent<XenoComponent, XenoPlaceResinTunnelDestroyWeedSourceDoAfterEvent>(OnCompleteRemoveWeedSource);
        SubscribeLocalEvent<XenoComponent, XenoDigTunnelDoAfter>(OnFinishCreateTunnel);

        SubscribeLocalEvent<XenoTunnelComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<XenoTunnelComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractVerbs);
        SubscribeLocalEvent<XenoTunnelComponent, ContainerRelayMovementEntityEvent>(OnAttemptMoveInTunnel);
        SubscribeLocalEvent<XenoTunnelComponent, TraverseXenoTunnelMessage>(OnMoveThroughTunnel);

        SubscribeLocalEvent<XenoTunnelComponent, EnterXenoTunnelDoAfterEvent>(OnFinishEnterTunnel);
        SubscribeLocalEvent<XenoTunnelComponent, TraverseXenoTunnelDoAfterEvent>(OnFinishMoveThroughTunnel);

        SubscribeLocalEvent<XenoTunnelComponent, OpenBoundInterfaceMessage>(GetAllAvailableTunnels);

        SubscribeLocalEvent<XenoTunnelComponent, GetVerbsEvent<ActivationVerb>>(OnGetRenameVerb);
        SubscribeLocalEvent<XenoTunnelComponent, InteractUsingEvent>(OnFillTunnel);
        SubscribeLocalEvent<XenoTunnelComponent, XenoCollapseTunnelDoAfterEvent>(OnCollapseTunnelFinish);
        SubscribeLocalEvent<XenoTunnelComponent, EntityTerminatingEvent>(OnDeleteTunnel);
        SubscribeLocalEvent<XenoTunnelComponent, EntInsertedIntoContainerMessage>(OnTunnelEntInserted);

        SubscribeLocalEvent<XenoTunnelComponent, ContainerIsInsertingAttemptEvent>(OnInsertEntityIntoTunnel);

        SubscribeLocalEvent<InXenoTunnelComponent, RegurgitateEvent>(OnRegurgitateInTunnel);
        SubscribeLocalEvent<InXenoTunnelComponent, ComponentInit>(OnInTunnel);
        SubscribeLocalEvent<InXenoTunnelComponent, ComponentRemove>(OnOutTunnel);
        SubscribeLocalEvent<InXenoTunnelComponent, DropAttemptEvent>(OnTryDropInTunnel);
        SubscribeLocalEvent<InXenoTunnelComponent, MobStateChangedEvent>(OnDeathInTunnel);

        Subs.BuiEvents<XenoTunnelComponent>(NameTunnelUI.Key, subs =>
        {
            subs.Event<NameTunnelMessage>(OnNameTunnel);
        });
    }

    private void OnCreateTunnel(Entity<XenoComponent> xenoBuilder, ref XenoDigTunnelActionEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        var location = _transform.GetMoverCoordinates(xenoBuilder).SnapToGrid(_entities);
        if (!CanPlaceTunnelPopup(args.Performer, location))
        {
            return;
        }

        if (_transform.GetGrid(location) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid) ||
            HasComp<AlmayerComponent>(gridId))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-bad-area-tunnel"), xenoBuilder, xenoBuilder);
            return;
        }

        if (!_xenoPlasma.HasPlasmaPopup(xenoBuilder.Owner, args.PlasmaCost, false))
            return;

        if (!Area.TryGetArea(location, out var area, out _) || area.Value.Comp.NoTunnel)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-bad-area-tunnel"), xenoBuilder, xenoBuilder);
            return;
        }

        if (_xenoWeeds.GetWeedsOnFloor((gridId, grid), location, true) is { } weedSource)
        {
            XenoPlaceResinTunnelDestroyWeedSourceDoAfterEvent weedRemovalEv = new()
            {
                CreateTunnelDelay = args.CreateTunnelDelay,
                PlasmaCost = args.PlasmaCost,
                Prototype = args.Prototype
            };

            var doAfterWeedRemovalArgs = new DoAfterArgs(EntityManager, xenoBuilder.Owner, args.DestroyWeedSourceDelay, weedRemovalEv, xenoBuilder.Owner, weedSource)
            {
                BlockDuplicate = true,
                BreakOnMove = true,
                DuplicateCondition = DuplicateConditions.SameTarget
            };
            _doAfter.TryStartDoAfter(doAfterWeedRemovalArgs);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-tunnel-uproot"), args.Performer, args.Performer);
            args.Handled = true;
            return;
        }

        var createTunnelEv = new XenoDigTunnelDoAfter(args.Prototype, args.PlasmaCost);
        var doAfterTunnelCreationArgs = new DoAfterArgs(EntityManager, xenoBuilder.Owner, args.CreateTunnelDelay, createTunnelEv, xenoBuilder.Owner)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameTarget
        };
        _doAfter.TryStartDoAfter(doAfterTunnelCreationArgs);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-tunnel-create-tunnel"), args.Performer, args.Performer);
        args.Handled = true;
    }

    private void OnCompleteRemoveWeedSource(Entity<XenoComponent> xenoBuilder, ref XenoPlaceResinTunnelDestroyWeedSourceDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            var actions = _action.GetActions(xenoBuilder.Owner);
            foreach (var (action, comp) in actions)
            {
                if (comp is InstantActionComponent instantActionComp &&
                    instantActionComp.Event is XenoDigTunnelActionEvent)
                {
                    _action.ClearCooldown(action);
                    break;
                }
            }
        }

        if (args.Handled || args.Cancelled)
        {
            return;
        }

        if (args.Target is null)
        {
            return;
        }

        if (!_xenoPlasma.HasPlasmaPopup(xenoBuilder.Owner, args.PlasmaCost, false))
        {
            return;
        }

        QueueDel(args.Target);

        var createTunnelEv = new XenoDigTunnelDoAfter(args.Prototype, args.PlasmaCost);
        var doAfterTunnelCreationArgs = new DoAfterArgs(EntityManager, xenoBuilder.Owner, args.CreateTunnelDelay, createTunnelEv, xenoBuilder.Owner)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameTarget
        };
        _doAfter.TryStartDoAfter(doAfterTunnelCreationArgs);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-tunnel-create-tunnel"), xenoBuilder.Owner, xenoBuilder.Owner);
        args.Handled = true;
    }

    private void OnFinishCreateTunnel(Entity<XenoComponent> xenoBuilder, ref XenoDigTunnelDoAfter args)
    {
        if (args.Cancelled)
        {
            var actions = _action.GetActions(xenoBuilder.Owner);
            foreach (var (action, comp) in actions)
            {
                if (comp is InstantActionComponent instantActionComp &&
                    instantActionComp.Event is XenoDigTunnelActionEvent)
                {
                    _action.ClearCooldown(action);
                    break;
                }
            }
        }

        if (args.Handled || args.Cancelled)
            return;

        if (!_xenoPlasma.HasPlasmaPopup(xenoBuilder.Owner, args.PlasmaCost))
            return;

        var tunnelFailureMessage = Loc.GetString("rmc-xeno-construction-failed-tunnel-rename");

        var location = _transform.GetMoverCoordinates(xenoBuilder).SnapToGrid(_entities);
        if (!CanPlaceTunnelPopup(xenoBuilder.Owner, location))
        {
            _popup.PopupEntity(tunnelFailureMessage, xenoBuilder.Owner, xenoBuilder.Owner);
            return;
        }

        _xenoPlasma.TryRemovePlasma(xenoBuilder.Owner, args.PlasmaCost);

        if (!TryPlaceTunnel(xenoBuilder.Owner, null, out var newTunnelEnt))
        {
            _popup.PopupEntity(tunnelFailureMessage, xenoBuilder.Owner, xenoBuilder.Owner);
            return;
        }

        NextTempTunnelId++;
        _ui.OpenUi(newTunnelEnt.Value, NameTunnelUI.Key, xenoBuilder.Owner);

        args.Handled = true;
    }

    private void OnNameTunnel(Entity<XenoTunnelComponent> xenoTunnel, ref NameTunnelMessage args)
    {
        var name = args.TunnelName;
        if (name.Length > 50)
            name = name[..50];

        var hive = Hive.GetHive(xenoTunnel.Owner);
        if (hive is null)
        {
            return;
        }
        var hiveComp = hive.Value.Comp;
        var hiveTunnels = hiveComp.HiveTunnels;

        string? curName = null;
        foreach (var item in hiveTunnels)
        {
            if (item.Value == xenoTunnel.Owner)
            {
                curName = item.Key;
            }
        }

        if (!hiveTunnels.TryAdd(name, xenoTunnel.Owner))
        {
            _popup.PopupCursor(Loc.GetString("rmc-xeno-construction-failed-tunnel-rename"), args.Actor);
            return;
        }

        _adminLog.Add(LogType.RMCXenoTunnel, $"{ToPrettyString(args.Actor)} renamed {ToPrettyString(xenoTunnel)} to {name}");
        if (curName is string)
        {
            hiveTunnels.Remove(curName);
        }

        _ui.CloseUi(xenoTunnel.Owner, NameTunnelUI.Key, args.Actor);
    }

    private void OnGetInteractVerbs(Entity<XenoTunnelComponent> xenoTunnel, ref GetVerbsEvent<InteractionVerb> args)
    {
        var user = args.User;
        var target = args.Target;
        var interactVerb = new InteractionVerb()
        {
            Act = () =>
            {
                var ev = new InteractHandEvent(user, target);
                RaiseLocalEvent(xenoTunnel, ev);
            },
            Text = Loc.GetString("xeno-ui-enter-tunnel-verb")
        };

        args.Verbs.Add(interactVerb);
    }

    private void OnInteract(Entity<XenoTunnelComponent> xenoTunnel, ref InteractHandEvent args)
    {
        var (ent, comp) = xenoTunnel;
        if (args.Handled)
        {
            return;
        }

        var enteringEntity = args.User;

        if (_container.ContainsEntity(xenoTunnel.Owner, enteringEntity))
        {
            _ui.OpenUi(ent, SelectDestinationTunnelUI.Key, enteringEntity);
            return;
        }

        var mobContainer = _container.EnsureContainer<Container>(xenoTunnel.Owner, XenoTunnelComponent.ContainedMobsContainerId);
        if (!HasComp<XenoComponent>(enteringEntity))
        {
            if (mobContainer.Count == 0)
                _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-tunnel-empty-non-xeno-enter-failure"), enteringEntity, enteringEntity);
            else
                _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-tunnel-occupied-non-xeno-enter-failure"), enteringEntity, enteringEntity);
            return;
        }

        if (mobContainer.Count >= xenoTunnel.Comp.MaxMobs)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-tunnel-full-xeno-failure"), enteringEntity, enteringEntity);
            return;
        }

        if (!_actionBlocker.CanMove(enteringEntity) || Transform(enteringEntity).Anchored)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-tunnel-xeno-immobile-failure"), enteringEntity, enteringEntity);
            return;
        }

        if (!TryComp(enteringEntity, out RMCSizeComponent? xenoSize))
        {
            return;
        }

        string? tunnelName;

        var enterDelay = comp.StandardXenoEnterDelay;
        TryGetHiveTunnelName(xenoTunnel, out tunnelName);
        string enterMessageLocID = "rmc-xeno-construction-tunnel-default-xeno-enter";

        switch (xenoSize.Size)
        {
            case RMCSizes.Small:
                enterDelay = comp.SmallXenoEnterDelay;
                enterMessageLocID = "rmc-xeno-construction-tunnel-default-xeno-enter";
                break;
            case RMCSizes.Big:
            case RMCSizes.Immobile:
                enterDelay = comp.LargeXenoEnterDelay;
                enterMessageLocID = "rmc-xeno-construction-tunnel-large-xeno-enter";
                break;
        }

        if (tunnelName is string)
        {
            _popup.PopupEntity(Loc.GetString(enterMessageLocID, ("tunnelName", tunnelName)), enteringEntity, enteringEntity);
        }
        var ev = new EnterXenoTunnelDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(_entities, enteringEntity, enterDelay, ev, xenoTunnel.Owner)
        {
            BreakOnMove = true,

        };
        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    private void OnMoveThroughTunnel(Entity<XenoTunnelComponent> xenoTunnel, ref TraverseXenoTunnelMessage args)
    {
        var (ent, comp) = xenoTunnel;

        var startingTunnel = _entities.GetEntity(args.Entity);
        var traversingXeno = args.Actor;

        // If the xeno leaves the tunnel, prevent teleportation
        if (!_container.ContainsEntity(startingTunnel, traversingXeno))
        {
            return;
        }

        var destinationTunnel = _entities.GetEntity(args.DestinationTunnel);
        if (!HasComp<XenoTunnelComponent>(destinationTunnel))
        {
            return;
        }

        var mobContainer = _container.EnsureContainer<Container>(destinationTunnel, XenoTunnelComponent.ContainedMobsContainerId);
        if (mobContainer.Count >= xenoTunnel.Comp.MaxMobs)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-tunnel-full-xeno-failure"), traversingXeno, traversingXeno);
            return;
        }


        if (!TryComp(traversingXeno, out RMCSizeComponent? xenoSize))
        {
            return;
        }

        var moveDelay = comp.StandardXenoMoveDelay;
        switch (xenoSize.Size)
        {
            case RMCSizes.Small:
                moveDelay = comp.SmallXenoMoveDelay;
                break;
            case RMCSizes.Big:
            case RMCSizes.Immobile:
                moveDelay = comp.LargeXenoMoveDelay;
                break;
        }

        var ev = new TraverseXenoTunnelDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(_entities, traversingXeno, moveDelay, ev, destinationTunnel, null, xenoTunnel.Owner)
        {
            BreakOnMove = true
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnAttemptMoveInTunnel(Entity<XenoTunnelComponent> xenoTunnel, ref ContainerRelayMovementEntityEvent args)
    {
        _transform.PlaceNextTo(args.Entity, xenoTunnel.Owner);
        RemCompDeferred<InXenoTunnelComponent>(args.Entity);
    }

    private void OnFinishEnterTunnel(Entity<XenoTunnelComponent> xenoTunnel, ref EnterXenoTunnelDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
        {
            return;
        }
        var (ent, comp) = xenoTunnel;
        var enteringEntity = args.User;

        var mobContainer = _container.EnsureContainer<Container>(ent, XenoTunnelComponent.ContainedMobsContainerId);
        if (mobContainer.Count >= xenoTunnel.Comp.MaxMobs)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-tunnel-full-xeno-failure"), enteringEntity, enteringEntity);
            return;
        }

        if (!_actionBlocker.CanMove(enteringEntity) || Transform(enteringEntity).Anchored)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-tunnel-xeno-immobile-failure"), enteringEntity, enteringEntity);
            return;
        }


        _container.Insert(enteringEntity, mobContainer);
        _ui.OpenUi(ent, SelectDestinationTunnelUI.Key, enteringEntity);

        args.Handled = true;
    }

    private void OnFinishMoveThroughTunnel(Entity<XenoTunnelComponent> destinationXenoTunnel, ref TraverseXenoTunnelDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var (ent, comp) = destinationXenoTunnel;
        var traversingXeno = args.User;
        var startingTunnel = args.Used!.Value;

        // If the xeno leaves the tunnel, prevent teleportation
        if (!_container.ContainsEntity(startingTunnel, traversingXeno))
            return;

        if (_transform.GetMap(startingTunnel) != _transform.GetMap(destinationXenoTunnel.Owner))
            return;

        var mobContainer = _container.EnsureContainer<Container>(destinationXenoTunnel, XenoTunnelComponent.ContainedMobsContainerId);
        if (mobContainer.Count >= comp.MaxMobs)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-tunnel-full-xeno-failure"), traversingXeno, traversingXeno);
            return;
        }

        _container.Insert(traversingXeno, mobContainer);
        _ui.OpenUi(destinationXenoTunnel.Owner, SelectDestinationTunnelUI.Key, args.User);

        args.Handled = true;
    }

    private void OnGetRenameVerb(Entity<XenoTunnelComponent> xenoTunnel, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!HasComp<XenoComponent>(args.User))
            return;

        var uid = args.User;

        var renameTunnelVerb = new ActivationVerb
        {
            Text = Loc.GetString("xeno-ui-rename-tunnel-verb"),
            Act = () =>
            {
                _ui.TryOpenUi(xenoTunnel.Owner, NameTunnelUI.Key, uid);
            },

            Impact = LogImpact.Low,
        };

        if (_hive.FromSameHive(xenoTunnel.Owner, uid) && HasComp<TunnelRenamerComponent>(uid))
        {
            args.Verbs.Add(renameTunnelVerb);
        }
    }

    private void GetAllAvailableTunnels(Entity<XenoTunnelComponent> destinationXenoTunnel, ref OpenBoundInterfaceMessage args)
    {
        var hive = Hive.GetHive(destinationXenoTunnel.Owner);
        if (hive is null ||
            !TryComp(hive, out HiveComponent? hiveComp))
        {
            return;
        }


        var hiveTunnels = hiveComp.HiveTunnels;
        Dictionary<string, NetEntity> netHiveTunnels = new();
        foreach (var (name, tunnel) in hiveTunnels)
        {
            netHiveTunnels.Add(name, _entities.GetNetEntity(tunnel));
        }

        var newState = new SelectDestinationTunnelInterfaceState(netHiveTunnels);

        _ui.SetUiState(destinationXenoTunnel.Owner, SelectDestinationTunnelUI.Key, newState);
    }

    private void OnFillTunnel(Entity<XenoTunnelComponent> xenoTunnel, ref InteractUsingEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        var tool = args.Used;
        if (TryComp(tool, out XenoTunnelFillerComponent? tunnelFillerComp))
        {
            if (TryComp(tool, out ItemToggleComponent? toggleComp))
            {
                if (!toggleComp.Activated)
                {
                    return;
                }
            }
            args.Handled = true;
            var ev = new XenoCollapseTunnelDoAfterEvent();
            var doAfterArgs = new DoAfterArgs(_entities, args.User, tunnelFillerComp.FillDelay, ev, xenoTunnel.Owner, xenoTunnel, tool)
            {
                BreakOnMove = true,
                NeedHand = true,
                BreakOnDropItem = true,
                BreakOnHandChange = true,
            };

            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-tunnel-fill"), args.User, args.User);
            _doAfter.TryStartDoAfter(doAfterArgs);
        }
    }

    private void OnCollapseTunnelFinish(Entity<XenoTunnelComponent> xenoTunnel, ref XenoCollapseTunnelDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        CollapseTunnel(xenoTunnel);
    }

    private void OnDeleteTunnel(Entity<XenoTunnelComponent> xenoTunnel, ref EntityTerminatingEvent args)
    {
        CollapseTunnel(xenoTunnel);
    }

    private void OnInsertEntityIntoTunnel(Entity<XenoTunnelComponent> xenoTunnel, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Container.ID != XenoTunnelComponent.ContainedMobsContainerId)
            return;

        if (!HasComp<XenoComponent>(args.EntityUid) ||
            !_mobState.IsAlive(args.EntityUid))
        {
            args.Cancel();
        }
    }

    private void OnTunnelEntInserted(Entity<XenoTunnelComponent> xenoTunnel, ref EntInsertedIntoContainerMessage args)
    {
        if (!HasComp<MobStateComponent>(args.Entity))
            return;

        if (!_mobState.IsAlive(args.Entity))
        {
            RemoveFromTunnel(args.Entity, xenoTunnel);
        }

        EnsureComp<InXenoTunnelComponent>(args.Entity);

    }

    /// <summary>
    /// Delete a tunnel and remove anything within it. CALLING DEL OR QUEUEDEL DIRECTLY ON THE TUNNEL IS NOT RECOMMENDED.
    /// </summary>
    /// <param name="xenoTunnel"></param>
    private void CollapseTunnel(Entity<XenoTunnelComponent> xenoTunnel)
    {
        if (Hive.GetHive(xenoTunnel.Owner) is { } hive && TryGetHiveTunnelName(xenoTunnel, out var tunnelName))
            hive.Comp.HiveTunnels.Remove(tunnelName);

        if (_container.TryGetContainer(xenoTunnel.Owner, XenoTunnelComponent.ContainedMobsContainerId, out var mobContainer))
        {
            foreach (var mob in mobContainer.ContainedEntities.ToArray())
            {
                RemoveFromTunnel(mob, mobContainer.Owner);
                _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-tunnel-fill-xeno-drop"), mob, mob);
            }
        }

        QueueDel(xenoTunnel.Owner);
    }

    private void OnInTunnel(Entity<InXenoTunnelComponent> tunneledXeno, ref ComponentInit args)
    {
        DisableAllAbilities(tunneledXeno.Owner);
    }

    private void OnOutTunnel(Entity<InXenoTunnelComponent> tunneledXeno, ref ComponentRemove args)
    {
        EnableAllAbilities(tunneledXeno.Owner);
    }

    private void OnTryDropInTunnel(Entity<InXenoTunnelComponent> tunneledXeno, ref DropAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnDeathInTunnel(Entity<InXenoTunnelComponent> tunneledXeno, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
        {
            return;
        }

        if (!_container.TryGetContainingContainer((tunneledXeno, null, null), out var mobContainer))
        {
            return;
        }

        var tunnel = mobContainer.Owner;
        RemoveFromTunnel(tunneledXeno, tunnel);
    }

    private void OnRegurgitateInTunnel(Entity<InXenoTunnelComponent> tunneledXeno, ref RegurgitateEvent args)
    {
        var regurgitated = _entities.GetEntity(args.NetRegurgitated);
        if (!_container.TryGetContainingContainer((tunneledXeno, null, null), out var mobContainer))
        {
            return;
        }

        var tunnel = mobContainer.Owner;
        RemoveFromTunnel(regurgitated, tunnel);
    }

    private void RemoveFromTunnel(EntityUid tunneledMob, EntityUid tunnel)
    {
        RemCompDeferred<InXenoTunnelComponent>(tunneledMob);
        _transform.DropNextTo(tunneledMob, tunnel);
    }

    private bool CanPlaceTunnelPopup(EntityUid user, EntityCoordinates coords)
    {
        var canPlaceStructure = _xenoConstruct.CanPlaceXenoStructure(user, coords, out var popupType, false);

        if (!canPlaceStructure)
        {
            popupType = popupType + "-tunnel";
            _popup.PopupEntity(Loc.GetString(popupType), user, user, PopupType.SmallCaution);
            return false;
        }

        if (Transform(user).GridUid is not EntityUid gridId ||
            !TryComp(gridId, out MapGridComponent? gridComp))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-bad-tile-tunnel"), user, user, PopupType.SmallCaution);
            return false;
        }
        var tileRef = _map.GetTileRef(gridId, gridComp, coords);
        if (!tileRef.GetContentTileDefinition().CanPlaceTunnel)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-bad-tile-tunnel"), user, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private void DisableAllAbilities(EntityUid ent)
    {
        SetEnabledStatusAllAbilities(ent, false);
    }

    private void EnableAllAbilities(EntityUid ent)
    {
        SetEnabledStatusAllAbilities(ent, true);
    }
    private void SetEnabledStatusAllAbilities(EntityUid ent, bool newStatus)
    {
        var actions = _action.GetActions(ent);
        foreach ((var actionEnt, var actionComp) in actions)
        {
            actionComp.Enabled = newStatus;
        }
    }
    public bool TryPlaceTunnel(Entity<HiveMemberComponent?> builder, string? name, [NotNullWhen(true)] out EntityUid? tunnelEnt)
    {
        tunnelEnt = null;
        if (!Resolve(builder, ref builder.Comp) ||
            builder.Comp.Hive is null)
        {
            return false;
        }

        var hasPlacedTunnel = TryPlaceTunnel(builder.Comp.Hive.Value, name, builder.Owner.ToCoordinates(), out tunnelEnt);

        if (tunnelEnt is EntityUid)
            _hive.SetSameHive(builder.Owner, tunnelEnt.Value);

        return hasPlacedTunnel;
    }
}
