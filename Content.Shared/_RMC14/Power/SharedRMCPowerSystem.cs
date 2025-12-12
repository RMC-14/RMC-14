using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Access.Components;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Numerics;
using static Content.Shared.Popups.PopupType;

namespace Content.Shared._RMC14.Power;

public abstract class SharedRMCPowerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedRMCSpriteSystem _sprite = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected readonly HashSet<EntityUid> ToUpdate = new();
    private readonly Dictionary<MapId, List<EntityUid>> _reactorPoweredLights = new();
    private readonly HashSet<MapId> _reactorsUpdated = new();
    private bool _recalculate;

    private EntityQuery<RMCApcComponent> _apcQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;
    private EntityQuery<RMCAreaPowerComponent> _areaPowerQuery;
    private EntityQuery<AreaComponent> _areaQuery;
    private EntityQuery<RMCPowerReceiverComponent> _powerReceiverQuery;

    public override void Initialize()
    {
        _apcQuery = GetEntityQuery<RMCApcComponent>();
        _appearanceQuery = GetEntityQuery<AppearanceComponent>();
        _areaPowerQuery = GetEntityQuery<RMCAreaPowerComponent>();
        _areaQuery = GetEntityQuery<AreaComponent>();
        _powerReceiverQuery = GetEntityQuery<RMCPowerReceiverComponent>();

        SubscribeLocalEvent<RMCApcComponent, ComponentStartup>(OnApcStartup);
        SubscribeLocalEvent<RMCApcComponent, MapInitEvent>(OnApcUpdate);
        SubscribeLocalEvent<RMCApcComponent, EntParentChangedMessage>(OnApcUpdate);
        SubscribeLocalEvent<RMCApcComponent, ComponentRemove>(OnApcRemove);
        SubscribeLocalEvent<RMCApcComponent, EntityTerminatingEvent>(OnApcRemove);
        SubscribeLocalEvent<RMCApcComponent, BreakageEventArgs>(OnApcBreakage);
        SubscribeLocalEvent<RMCApcComponent, InteractUsingEvent>(OnApcInteractUsing);
        SubscribeLocalEvent<RMCApcComponent, InteractHandEvent>(OnApcInteractHand);
        SubscribeLocalEvent<RMCApcComponent, ActivatableUIOpenAttemptEvent>(OnApcActivatableUIOpenAttempt);
        SubscribeLocalEvent<RMCApcComponent, ExaminedEvent>(OnApcExamined);

        SubscribeLocalEvent<RMCPowerReceiverComponent, MapInitEvent>(OnReceiverMapInit);
        SubscribeLocalEvent<RMCPowerReceiverComponent, EntParentChangedMessage>(OnReceiverUpdate);
        SubscribeLocalEvent<RMCPowerReceiverComponent, ComponentRemove>(OnReceiverRemove);
        SubscribeLocalEvent<RMCPowerReceiverComponent, EntityTerminatingEvent>(OnReceiverRemove);

        SubscribeLocalEvent<RMCFusionReactorComponent, MapInitEvent>(OnFusionReactorMapInit);
        SubscribeLocalEvent<RMCFusionReactorComponent, InteractUsingEvent>(OnFusionReactorInteractUsing);
        SubscribeLocalEvent<RMCFusionReactorComponent, RMCFusionReactorCellDoAfterEvent>(OnFusionReactorCellDoAfter);
        SubscribeLocalEvent<RMCFusionReactorComponent, RMCFusionReactorRemoveCellDoAfterEvent>(OnFusionReactorRemoveCellDoAfter);
        SubscribeLocalEvent<RMCFusionReactorComponent, RMCFusionReactorRepairDoAfterEvent>(OnFusionReactorRepairWeldingDoAfter);
        SubscribeLocalEvent<RMCFusionReactorComponent, InteractHandEvent>(OnFusionReactorInteractHand);
        SubscribeLocalEvent<RMCFusionReactorComponent, RMCFusionReactorDestroyDoAfterEvent>(OnFusionReactorDestroyDoAfter);
        SubscribeLocalEvent<RMCFusionReactorComponent, ExaminedEvent>(OnFusionReactorExamined);

        SubscribeLocalEvent<RMCReactorPoweredLightComponent, MapInitEvent>(OnReactorPoweredLightMapInit);

        Subs.BuiEvents<RMCApcComponent>(RMCApcUiKey.Key,
            subs =>
            {
                subs.Event<RMCApcSetChannelBuiMsg>(OnApcSetChannelBuiMsg);
                subs.Event<RMCApcCoverBuiMsg>(OnApcCover);
            });
    }

    private void OnApcStartup(Entity<RMCApcComponent> ent, ref ComponentStartup args)
    {
        OffsetApc(ent);
    }

    private void OnApcUpdate<T>(Entity<RMCApcComponent> ent, ref T args)
    {
        if (!TryComp(ent, out MetaDataComponent? metaData) ||
            metaData.EntityLifeStage < EntityLifeStage.MapInitialized)
        {
            return;
        }

        ToUpdate.Add(ent);

        if (_net.IsClient)
            return;

        if (TerminatingOrDeleted(ent))
            return;

        if (_area.TryGetArea(ent, out _, out var areaProto))
            _metaData.SetEntityName(ent, $"{areaProto.Name} APC");

        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.CellContainerSlot);
        if (ent.Comp.StartingCell is { } startingCell)
            TrySpawnInContainer(startingCell, ent, ent.Comp.CellContainerSlot, out _);

        OffsetApc(ent);
    }

    private void OnApcRemove<T>(Entity<RMCApcComponent> ent, ref T args)
    {
        if (TerminatingOrDeleted(ent.Comp.Area))
            return;

        if (_areaPowerQuery.TryComp(ent.Comp.Area, out var map))
        {
            map.Apcs.Remove(ent);
            Dirty(ent.Comp.Area.Value, map);
        }
    }

    private void OnApcBreakage(Entity<RMCApcComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.State = RMCApcState.WiresExposed;
        ent.Comp.Broken = true;
        Dirty(ent);

        _appearance.SetData(ent, RMCApcVisualsLayers.Layer, RMCApcState.WiresExposed);
    }

    private void OnApcInteractUsing(Entity<RMCApcComponent> ent, ref InteractUsingEvent args)
    {
        var user = args.User;
        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
        {
            _popup.PopupClient($"You don't know how to use the {Name(ent)}'s interface.", ent, user, SmallCaution);
            return;
        }

        var used = args.Used;
        if (_tool.HasQuality(used, ent.Comp.CrowbarTool))
        {
            switch (ent.Comp.State)
            {
                case RMCApcState.Working:
                case RMCApcState.WiresExposed:
                    if (ent.Comp.CoverLockedButton)
                    {
                        _popup.PopupClient("The cover is locked and cannot be opened.", user, user, MediumCaution);
                        return;
                    }

                    ent.Comp.State =
                        _container.TryGetContainer(ent, ent.Comp.CellContainerSlot, out var container) &&
                        container.ContainedEntities.Count > 0
                            ? RMCApcState.CoverOpenBattery
                            : RMCApcState.CoverOpenNoBattery;
                    Dirty(ent);
                    _appearance.SetData(ent, RMCApcVisualsLayers.Layer, ent.Comp.State);
                    break;
                case RMCApcState.CoverOpenBattery:
                case RMCApcState.CoverOpenNoBattery:
                    ent.Comp.State = RMCApcState.Working;
                    Dirty(ent);
                    _appearance.SetData(ent, RMCApcVisualsLayers.Layer, ent.Comp.State);
                    break;
            }
        }

        if (HasComp<PowerCellComponent>(used) && ent.Comp.State == RMCApcState.CoverOpenNoBattery)
        {
            var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.CellContainerSlot);
            _hands.TryDropIntoContainer(user, used, container);
            if (container.ContainedEntities.Count > 0)
            {
                ent.Comp.State = RMCApcState.CoverOpenBattery;
                Dirty(ent);
                _appearance.SetData(ent, RMCApcVisualsLayers.Layer, ent.Comp.State);
                ToUpdate.Add(ent);
            }

            return;
        }

        if (TryComp(used, out AccessComponent? access))
        {
            // TODO RMC14 access wire
            // var hasAccess = access.Tags.Any(t => ent.Comp.Access.Contains(t));
            // if (!hasAccess)
            // {
            //     _popup.PopupClient("Access denied.", ent, user, SmallCaution);
            //     return;
            // }

            ent.Comp.Locked = !ent.Comp.Locked;
            Dirty(ent);
        }

        if (!_tool.HasQuality(used, ent.Comp.RepairTool))
            return;

        ent.Comp.State = ent.Comp.State switch
        {
            RMCApcState.Working => RMCApcState.WiresExposed,
            RMCApcState.WiresExposed => RMCApcState.Working,
            _ => ent.Comp.State,
        };

        ent.Comp.Broken = false;
        Dirty(ent);

        _appearance.SetData(ent, RMCApcVisualsLayers.Layer, ent.Comp.State);

        if (TryComp(ent, out DamageableComponent? damageable))
            _damageable.SetAllDamage(ent, damageable, FixedPoint2.Zero);
    }

    private void OnApcInteractHand(Entity<RMCApcComponent> ent, ref InteractHandEvent args)
    {
        if (ent.Comp.State != RMCApcState.CoverOpenBattery)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.CellContainerSlot, out var container))
            return;

        foreach (var contained in container.ContainedEntities)
        {
            if (_container.Remove(contained, container))
            {
                _hands.TryPickupAnyHand(args.User, contained);

                ent.Comp.State = RMCApcState.CoverOpenNoBattery;
                ent.Comp.ChargePercentage = 0;
                Dirty(ent);

                _appearance.SetData(ent, RMCApcVisualsLayers.Layer, ent.Comp.State);
                ToUpdate.Add(ent);
                break;
            }
        }
    }

    private void OnApcActivatableUIOpenAttempt(Entity<RMCApcComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_skills.HasSkill(args.User, ent.Comp.Skill, ent.Comp.SkillLevel))
        {
            args.Cancel();
            _popup.PopupClient($"You don't know how to use the {Name(ent)}'s interface.", ent, args.User, SmallCaution);
            return;
        }

        if (ent.Comp.State != RMCApcState.Working)
            args.Cancel();
    }

    private void OnApcExamined(Entity<RMCApcComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(RMCApcComponent)))
        {
            var markup = ent.Comp.State switch
            {
                RMCApcState.Working => "Use:\n" +
                                       "- An [color=cyan]engineering ID[/color] to lock or unlock the interface.\n" +
                                       "- A [color=cyan]crowbar[/color] to open the cover.\n" +
                                       "- A [color=cyan]screwdriver[/color] to expose the wires.",
                RMCApcState.WiresExposed => "Use a [color=cyan]screwdriver[/color] to unexpose the wires or a [color=cyan]crowbar[/color] to open the cover!",
                RMCApcState.CoverOpenBattery => "Use an [color=cyan]empty hand[/color] to remove the battery or a [color=cyan]crowbar[/color] to close the cover!",
                RMCApcState.CoverOpenNoBattery => "Use a [color=cyan]battery[/color] to put in a battery!",
                _ => null,
            };

            if (markup != null)
                args.PushMarkup(markup);
        }
    }

    protected virtual void OnReceiverMapInit(Entity<RMCPowerReceiverComponent> ent, ref MapInitEvent args)
    {
        OnReceiverUpdate(ent, ref args);
    }

    private void OnReceiverUpdate<T>(Entity<RMCPowerReceiverComponent> ent, ref T args)
    {
        ToUpdate.Add(ent);
    }

    private void OnReceiverRemove<T>(Entity<RMCPowerReceiverComponent> ent, ref T args)
    {
        if (!TryGetPowerArea(ent, out var area) ||
            TerminatingOrDeleted(area))
        {
            return;
        }

        GetAreaReceivers(area, ent.Comp.Channel).Remove(ent);
    }

    private void OnFusionReactorMapInit(Entity<RMCFusionReactorComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.CellContainerSlot);
        if (ent.Comp.StartingCell is { } startingCell)
            TrySpawnInContainer(startingCell, ent, ent.Comp.CellContainerSlot, out _);

        if (ent.Comp.RandomizeDamage)
        {
            var random = _random.NextDouble();
            if (random < 0.5)
                ent.Comp.State = RMCFusionReactorState.Weld;
            else if (random < 0.85)
                ent.Comp.State = RMCFusionReactorState.Wire;
            else
                ent.Comp.State = RMCFusionReactorState.Wrench;

            Dirty(ent);
        }

        UpdateAppearance(ent);
        ReactorUpdated(ent);
    }

    private void OnFusionReactorInteractUsing(Entity<RMCFusionReactorComponent> ent, ref InteractUsingEvent args)
    {
        var user = args.User;
        var used = args.Used;

        args.Handled = true;
        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.CellContainerSlot);
        if (HasComp<RMCFusionCellComponent>(used))
        {
            if (container.ContainedEntity != null)
            {
                var msg = Loc.GetString("rmc-fusion-reactor-insert-already-has-cell", ("reactor", ent));
                _popup.PopupClient(msg, ent, user, SmallCaution);
                return;
            }

            var ev = new RMCFusionReactorCellDoAfterEvent();
            var delay = ent.Comp.CellDelay * _skills.GetSkillDelayMultiplier(user, ent.Comp.Skill);
            var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent, used: used)
            {
                BreakOnMove = true,
                DuplicateCondition = DuplicateConditions.SameEvent,
            };

            if (_doAfter.TryStartDoAfter(doAfter))
            {
                var msg = Loc.GetString("rmc-fusion-reactor-insert-start-self", ("cell", used), ("reactor", ent));
                _popup.PopupClient(msg, ent, user);
            }
        }
        else if (_tool.HasQuality(used, ent.Comp.CrowbarQuality))
        {
            if (container.ContainedEntity == null)
            {
                var msg = Loc.GetString("rmc-fusion-reactor-remove-none", ("reactor", ent));
                _popup.PopupClient(msg, ent, user, SmallCaution);
                return;
            }

            var ev = new RMCFusionReactorRemoveCellDoAfterEvent();
            var delay = ent.Comp.CellDelay * _skills.GetSkillDelayMultiplier(user, ent.Comp.Skill);
            var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent, used: used)
            {
                BreakOnMove = true,
                DuplicateCondition = DuplicateConditions.SameEvent,
            };

            if (_doAfter.TryStartDoAfter(doAfter))
            {
                var msg = Loc.GetString("rmc-fusion-reactor-remove-start-self",
                    ("cell", container.ContainedEntity.Value),
                    ("reactor", ent));
                _popup.PopupClient(msg, ent, user);
            }
        }
        else if (_tool.HasQuality(used, ent.Comp.WeldingQuality))
        {
            TryRepair(ent, user, used, RMCFusionReactorState.Weld);
        }
        else if (_tool.HasQuality(used, ent.Comp.CuttingQuality))
        {
            TryRepair(ent, user, used, RMCFusionReactorState.Wire);
        }
        else if (_tool.HasQuality(used, ent.Comp.WrenchQuality))
        {
            TryRepair(ent, user, used, RMCFusionReactorState.Wrench);
        }
        else if (TryComp<RMCDeviceBreakerComponent>(used, out var breaker) && ent.Comp.State != RMCFusionReactorState.Weld)
        {
            var doafter = new DoAfterArgs(EntityManager, args.User, breaker.DoAfterTime, new RMCDeviceBreakerDoAfterEvent(), args.Used, args.Target, args.Used)
            {
                BreakOnMove = true,
                RequireCanInteract = true,
                BreakOnHandChange = true,
                DuplicateCondition = DuplicateConditions.SameTool
            };

            _doAfter.TryStartDoAfter(doafter);
            return;
        }
    }

    private void OnFusionReactorCellDoAfter(Entity<RMCFusionReactorComponent> ent, ref RMCFusionReactorCellDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } used)
            return;

        args.Handled = true;

        var user = args.User;
        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.CellContainerSlot);
        string msg;
        if (!_container.Insert(used, container))
        {
            msg = Loc.GetString("rmc-fusion-reactor-insert-fail-self", ("cell", used), ("reactor", ent));
            _popup.PopupClient(msg, ent, user, SmallCaution);
            return;
        }

        // TODO RMC14 reactor failure
        msg = Loc.GetString("rmc-fusion-reactor-insert-finish-self", ("cell", used), ("reactor", ent));
        _popup.PopupClient(msg, ent, user);

        UpdateAppearance(ent);
    }

    private void OnFusionReactorRemoveCellDoAfter(Entity<RMCFusionReactorComponent> ent, ref RMCFusionReactorRemoveCellDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var user = args.User;
        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.CellContainerSlot);
        string msg;
        if (container.ContainedEntity is not { } cell)
        {
            msg = Loc.GetString("rmc-fusion-reactor-remove-none", ("reactor", ent));
            _popup.PopupClient(msg, ent, user, SmallCaution);
            return;
        }

        if (_container.Remove(cell, container))
            _hands.TryPickupAnyHand(user, cell);

        msg = Loc.GetString("rmc-fusion-reactor-remove-finish-self", ("cell", cell), ("reactor", ent));
        _popup.PopupClient(msg, ent, user);

        UpdateAppearance(ent);
    }

    private void OnFusionReactorRepairWeldingDoAfter(Entity<RMCFusionReactorComponent> ent, ref RMCFusionReactorRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.State != args.State)
            return;

        ent.Comp.State = args.State switch
        {
            RMCFusionReactorState.Wrench => RMCFusionReactorState.Working,
            RMCFusionReactorState.Wire => RMCFusionReactorState.Wrench,
            RMCFusionReactorState.Weld => RMCFusionReactorState.Wire,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Dirty(ent);
        UpdateAppearance(ent);
        ReactorUpdated(ent);
    }

    private void OnFusionReactorInteractHand(Entity<RMCFusionReactorComponent> ent, ref InteractHandEvent args)
    {
        var user = args.User;
        if (!HasComp<XenoComponent>(user) || !HasComp<MeleeWeaponComponent>(user))
            return;

        if (ent.Comp.State == RMCFusionReactorState.Weld)
        {
            _popup.PopupClient(Loc.GetString("rmc-fusion-reactor-already-destroyed", ("reactor", ent)), ent, user);
            return;
        }

        var ev = new RMCFusionReactorDestroyDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.DestroyDelay, ev, ent, ent)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnFusionReactorDestroyDoAfter(Entity<RMCFusionReactorComponent> ent, ref RMCFusionReactorDestroyDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || args.Handled)
            return;

        if (ent.Comp.State == RMCFusionReactorState.Weld)
        {
            _popup.PopupClient(Loc.GetString("rmc-fusion-reactor-already-destroyed", ("reactor", ent)), ent, user);
            return;
        }

        args.Handled = true;
        DestroyReactor(ent, args.User);

        if (ent.Comp.State != RMCFusionReactorState.Weld)
            args.Repeat = true;
    }

    public void DestroyReactor(Entity<RMCFusionReactorComponent> ent, EntityUid? user)
    {
        ent.Comp.State = ent.Comp.State switch
        {
            RMCFusionReactorState.Working => RMCFusionReactorState.Wrench,
            RMCFusionReactorState.Wrench => RMCFusionReactorState.Wire,
            RMCFusionReactorState.Wire => RMCFusionReactorState.Weld,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Dirty(ent);
        UpdateAppearance(ent);

        _popup.PopupClient(Loc.GetString("rmc-fusion-reactor-destroyed", ("reactor", ent)), ent, user, SmallCaution);

        ReactorUpdated(ent);
    }

    private void OnFusionReactorExamined(Entity<RMCFusionReactorComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(RMCFusionReactorComponent)))
        {
            if (ent.Comp.State != RMCFusionReactorState.Working)
            {
                // TODO: localize
                var tool = ent.Comp.State switch
                {
                    RMCFusionReactorState.Wrench => "a [color=cyan]Wrench[/color]",
                    RMCFusionReactorState.Wire => "[color=cyan]Wirecutters[/color]",
                    RMCFusionReactorState.Weld => "a [color=cyan]Welder[/color]",
                    _ => throw new ArgumentOutOfRangeException(),
                };

                args.PushMarkup($"Use {tool} to repair it!");
            }

            if (!_container.TryGetContainer(ent, ent.Comp.CellContainerSlot, out var container) ||
                container.ContainedEntities.Count == 0)
            {
                // TODO: localize
                args.PushMarkup("It needs a [color=cyan]fuel cell[/color]!");
            }
        }
    }

    private void OnReactorPoweredLightMapInit(Entity<RMCReactorPoweredLightComponent> ent, ref MapInitEvent args)
    {
        if (TryComp(ent, out TransformComponent? xform))
            _reactorPoweredLights.GetOrNew(xform.MapID).Add(ent);
    }

    private void OnApcSetChannelBuiMsg(Entity<RMCApcComponent> ent, ref RMCApcSetChannelBuiMsg args)
    {
        return;
        var channel = (int) args.Channel;
        if (args.Channel < 0 || channel >= ent.Comp.Channels.Length)
            return;

        ent.Comp.Channels[channel].Button = args.State;
        Dirty(ent);
    }

    private void OnApcCover(Entity<RMCApcComponent> ent, ref RMCApcCoverBuiMsg args)
    {
        if (ent.Comp.State != RMCApcState.Working ||
            ent.Comp.Locked)
        {
            return;
        }

        ent.Comp.CoverLockedButton = !ent.Comp.CoverLockedButton;
        Dirty(ent);
    }

    private void UpdateAppearance(Entity<RMCFusionReactorComponent> ent)
    {
        switch (ent.Comp.State)
        {
            case RMCFusionReactorState.Weld:
                _appearance.SetData(ent, RMCFusionReactorLayers.Layer, RMCFusionReactorVisuals.Weld);
                return;
            case RMCFusionReactorState.Wire:
                _appearance.SetData(ent, RMCFusionReactorLayers.Layer, RMCFusionReactorVisuals.Wire);
                return;
            case RMCFusionReactorState.Wrench:
                _appearance.SetData(ent, RMCFusionReactorLayers.Layer, RMCFusionReactorVisuals.Wrench);
                return;
        }

        // TODO RMC14 off
        if (!_container.TryGetContainer(ent, ent.Comp.CellContainerSlot, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            _appearance.SetData(ent, RMCFusionReactorLayers.Layer, RMCFusionReactorVisuals.Empty);
            return;
        }

        // TODO RMC14 overloaded
        // TODO RMC14 fuel use
        _appearance.SetData(ent, RMCFusionReactorLayers.Layer, RMCFusionReactorVisuals.Hundred);
    }

    private void TryRepair(
        Entity<RMCFusionReactorComponent> ent,
        EntityUid user,
        EntityUid used,
        RMCFusionReactorState state)
    {
        string msg;
        if (ent.Comp.State == RMCFusionReactorState.Working)
        {
            msg = Loc.GetString("rmc-fusion-reactor-repair-not-needed", ("reactor", ent));
            _popup.PopupClient(msg, ent, user, SmallCaution);
            return;
        }
        else if (ent.Comp.State != state)
        {
            msg = Loc.GetString("rmc-fusion-reactor-repair-different-tool", ("reactor", ent));
            _popup.PopupClient(msg, ent, user, SmallCaution);
            return;
        }

        var quality = state switch
        {
            RMCFusionReactorState.Wrench => ent.Comp.WrenchQuality,
            RMCFusionReactorState.Wire => ent.Comp.CuttingQuality,
            RMCFusionReactorState.Weld => ent.Comp.WeldingQuality,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };

        var toolUsed = _tool.UseTool(
            used,
            user,
            ent,
            (float)ent.Comp.RepairDelay.TotalSeconds,
            quality,
            new RMCFusionReactorRepairDoAfterEvent(state),
            ent.Comp.WeldingCost,
            duplicateCondition: DuplicateConditions.SameTool
        );

        if (!toolUsed)
            return;

        msg = Loc.GetString("rmc-fusion-reactor-repair-start-self", ("reactor", ent), ("tool", used));
        _popup.PopupClient(msg, ent, user);
    }

    private bool TryGetPowerArea(EntityUid ent, out Entity<RMCAreaPowerComponent> areaPower)
    {
        areaPower = default;
        if (!_area.TryGetArea(ent, out var area, out _))
            return false;

        var areaPowerComp = EnsureComp<RMCAreaPowerComponent>(area.Value);
        areaPower = (area.Value, areaPowerComp);
        return true;
    }

    private int GetNewPowerLoad(Entity<RMCPowerReceiverComponent> receiver)
    {
        return receiver.Comp.Mode switch
        {
            RMCPowerMode.Off => 0,
            RMCPowerMode.Idle => receiver.Comp.IdleLoad,
            RMCPowerMode.Active => receiver.Comp.ActiveLoad,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    protected HashSet<EntityUid> GetAreaReceivers(Entity<RMCAreaPowerComponent> area, RMCPowerChannel channel)
    {
        return channel switch
        {
            RMCPowerChannel.Equipment => area.Comp.EquipmentReceivers,
            RMCPowerChannel.Lighting => area.Comp.LightingReceivers,
            RMCPowerChannel.Environment => area.Comp.EnvironmentReceivers,
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
        };
    }

    protected void UpdateApcChannel(Entity<RMCApcComponent> apc, Entity<RMCAreaPowerComponent> area, RMCPowerChannel channel, bool on)
    {
        ref var apcChannel = ref apc.Comp.Channels[(int) channel];
        if (apcChannel.On == on)
            return;

        if (apcChannel.Button == RMCApcButtonState.Auto ||
            (apcChannel.Button == RMCApcButtonState.On && on) ||
            (apcChannel.Button == RMCApcButtonState.Off && !on))
        {
            apcChannel.On = on;
        }

        PowerUpdated(area, channel, on);
    }

    protected virtual void PowerUpdated(Entity<RMCAreaPowerComponent> area, RMCPowerChannel channel, bool on)
    {
    }

    public bool IsAreaPowered(Entity<RMCAreaPowerComponent?> area, RMCPowerChannel channel)
    {
        if (!_areaPowerQuery.Resolve(area, ref area.Comp, false))
            return false;

        if (_areaQuery.TryComp(area, out var areaComponent) && areaComponent.AlwaysPowered)
            return true;

        foreach (var apcId in area.Comp.Apcs)
        {
            if (!_apcQuery.TryComp(apcId, out var apc))
                continue;

            if (apc.Channels[(int)channel].On)
                return true;
        }

        return false;
    }

    public abstract bool IsPowered(EntityUid ent);

    private bool AnyReactorsOn(MapId map)
    {
        var reactors = EntityQueryEnumerator<RMCFusionReactorComponent, TransformComponent>();
        while (reactors.MoveNext(out var comp, out var xform))
        {
            if (comp.State == RMCFusionReactorState.Working && xform.MapID == map)
                return true;
        }

        return false;
    }

    private void ReactorUpdated(Entity<RMCFusionReactorComponent> ent)
    {
        var mapId = _transform.GetMapId(ent.Owner);
        _reactorsUpdated.Add(mapId);
    }

    protected void UpdateReceiverPower(EntityUid receiver, ref PowerChangedEvent ev)
    {
        SharedApcPowerReceiverComponent? receiverComp = null;
        if (!_powerReceiver.ResolveApc(receiver, ref receiverComp))
            return;

        if (receiverComp.Powered == ev.Powered)
            return;

        if (!receiverComp.NeedsPower)
            return;

        receiverComp.Powered = ev.Powered;
        Dirty(receiver, receiverComp);

        RaiseLocalEvent(receiver, ref ev);

        if (_appearanceQuery.TryComp(receiver, out var appearance))
            _appearance.SetData(receiver, PowerDeviceVisuals.Powered, ev.Powered, appearance);
    }

    public void RecalculatePower()
    {
        _recalculate = true;
    }

    private void OffsetApc(Entity<RMCApcComponent> ent)
    {
        var sprite = EnsureComp<SpriteSetRenderOrderComponent>(ent);
        switch (Transform(ent).LocalRotation.GetDir())
        {
            case Direction.South:
                _sprite.SetOffset(ent, new Vector2(0.45f, -0.32f));
                break;
            case Direction.East:
                _sprite.SetOffset(ent, new Vector2(0.7f, -1.45f));
                break;
            case Direction.North:
                _sprite.SetOffset(ent, new Vector2(-0.5f, -1.5f));
                break;
            case Direction.West:
                _sprite.SetOffset(ent, new Vector2(-0.7f, -0.4f));
                break;
        }

        Dirty(ent, sprite);
    }

    public override void Update(float frameTime)
    {
        if (_recalculate)
        {
            _recalculate = false;
            var apcQuery = EntityQueryEnumerator<RMCApcComponent>();
            while (apcQuery.MoveNext(out var uid, out _))
            {
                ToUpdate.Add(uid);
            }

            var receiverQuery = EntityQueryEnumerator<RMCPowerReceiverComponent>();
            while (receiverQuery.MoveNext(out var uid, out _))
            {
                ToUpdate.Add(uid);
            }

            var reactorQuery = EntityQueryEnumerator<RMCFusionReactorComponent>();
            while (reactorQuery.MoveNext(out var uid, out _))
            {
                _reactorsUpdated.Add(Transform(uid).MapID);
            }

            var lightQuery = EntityQueryEnumerator<RMCReactorPoweredLightComponent>();
            while (lightQuery.MoveNext(out var uid, out var comp))
            {
                _reactorPoweredLights.GetOrNew(Transform(uid).MapID).Add(uid);
            }
        }

        if (_net.IsClient)
        {
            ToUpdate.Clear();
            _reactorPoweredLights.Clear();
            _reactorsUpdated.Clear();
            return;
        }

        try
        {
            foreach (var map in _reactorsUpdated)
            {
                var powered = AnyReactorsOn(map);
                var lights = EntityQueryEnumerator<RMCReactorPoweredLightComponent, TransformComponent>();
                while (lights.MoveNext(out var uid, out _, out var xform))
                {
                    if (xform.MapID == map)
                    {
                        _appearance.SetData(uid, ToggleableVisuals.Enabled, powered);
                        _pointLight.SetEnabled(uid, powered);
                    }
                }
            }
        }
        finally
        {
            _reactorsUpdated.Clear();
        }

        try
        {
            foreach (var update in ToUpdate)
            {
                if (TerminatingOrDeleted(update))
                    continue;

                if (_apcQuery.TryComp(update, out var apc))
                {
                    if (_areaPowerQuery.TryComp(apc.Area, out var oldArea))
                    {
                        oldArea.Apcs.Remove(update);
                        Dirty(update, apc);
                    }
                }

                if (_powerReceiverQuery.TryComp(update, out var receiver))
                {
                    if (_areaPowerQuery.TryComp(receiver.Area, out var oldArea))
                    {
                        GetAreaReceivers((receiver.Area.Value, oldArea), receiver.Channel).Remove(update);
                        oldArea.Load[(int) receiver.Channel] -= receiver.LastLoad;
                        Dirty(update, receiver);
                    }
                }

                if (!TryGetPowerArea(update, out var area))
                    continue;

                if (apc != null)
                {
                    if (area.Comp.Apcs.Add(update))
                        Dirty(area);

                    apc.Area = area;
                    Dirty(update, apc);
                }

                if (receiver != null)
                {
                    receiver.Area = area;
                    Dirty(update, receiver);

                    var ev = new PowerChangedEvent(IsAreaPowered((area, area), receiver.Channel), 0);
                    UpdateReceiverPower(update, ref ev);

                    if (GetAreaReceivers(area, receiver.Channel).Add(update))
                    {
                        receiver.LastLoad = GetNewPowerLoad((update, receiver));
                        area.Comp.Load[(int) receiver.Channel] += receiver.LastLoad;
                        Dirty(area);
                    }
                }
            }
        }
        finally
        {
            ToUpdate.Clear();
        }
    }
}
