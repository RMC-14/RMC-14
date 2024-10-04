using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    protected readonly HashSet<EntityUid> ToUpdate = new();

    private EntityQuery<RMCApcComponent> _apcQuery;
    private EntityQuery<RMCAreaPowerComponent> _areaPowerQuery;
    private EntityQuery<RMCPowerReceiverComponent> _powerReceiverQuery;

    public override void Initialize()
    {
        _apcQuery = GetEntityQuery<RMCApcComponent>();
        _areaPowerQuery = GetEntityQuery<RMCAreaPowerComponent>();
        _powerReceiverQuery = GetEntityQuery<RMCPowerReceiverComponent>();

        SubscribeLocalEvent<RMCApcComponent, MapInitEvent>(OnApcUpdate);
        SubscribeLocalEvent<RMCApcComponent, EntParentChangedMessage>(OnApcUpdate);
        SubscribeLocalEvent<RMCApcComponent, ComponentRemove>(OnApcRemove);
        SubscribeLocalEvent<RMCApcComponent, EntityTerminatingEvent>(OnApcRemove);
        SubscribeLocalEvent<RMCApcComponent, BreakageEventArgs>(OnApcBreakage);
        SubscribeLocalEvent<RMCApcComponent, InteractUsingEvent>(OnApcInteractUsing);
        SubscribeLocalEvent<RMCApcComponent, ActivatableUIOpenAttemptEvent>(OnApcActivatableUIOpenAttempt);
        SubscribeLocalEvent<RMCApcComponent, ExaminedEvent>(OnApcExamined);

        SubscribeLocalEvent<RMCPowerReceiverComponent, MapInitEvent>(OnReceiverUpdate);
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

        Subs.BuiEvents<RMCApcComponent>(RMCApcUiKey.Key,
            subs =>
            {
                subs.Event<RMCApcSetChannelBuiMsg>(OnApcSetChannelBuiMsg);
            });
    }

    private void OnApcUpdate<T>(Entity<RMCApcComponent> ent, ref T args)
    {
        ToUpdate.Add(ent);

        if (_net.IsClient)
            return;

        if (TerminatingOrDeleted(ent))
            return;

        if (_area.TryGetArea(ent, out var area))
            _metaData.SetEntityName(ent, $"{Name(area)} APC");

        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.CellContainerSlot);
        if (ent.Comp.StartingCell is { } startingCell)
            TrySpawnInContainer(startingCell, ent, ent.Comp.CellContainerSlot, out _);
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
        ent.Comp.Broken = true;
        Dirty(ent);
        _appearance.SetData(ent, RMCApcVisualsLayers.Layer, RMCApcVisuals.Broken);
    }

    private void OnApcInteractUsing(Entity<RMCApcComponent> ent, ref InteractUsingEvent args)
    {
        if (!_tool.HasQuality(args.Used, ent.Comp.RepairTool))
            return;

        ent.Comp.Broken = false;
        Dirty(ent);
        _appearance.SetData(ent, RMCApcVisualsLayers.Layer, RMCApcVisuals.Working);

        if (TryComp(ent, out DamageableComponent? damageable))
            _damageable.SetAllDamage(ent, damageable, FixedPoint2.Zero);
    }

    private void OnApcActivatableUIOpenAttempt(Entity<RMCApcComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (ent.Comp.Broken)
            args.Cancel();
    }

    private void OnApcExamined(Entity<RMCApcComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(RMCApcComponent)))
        {
            if (ent.Comp.Broken)
                args.PushMarkup("Use a [color=cyan]screwdriver[/color] to repair it!");
        }
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
            _ => throw new ArgumentOutOfRangeException()
        };

        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void OnFusionReactorInteractHand(Entity<RMCFusionReactorComponent> ent, ref InteractHandEvent args)
    {
        var user = args.User;
        if (!HasComp<XenoComponent>(user))
            return;

        if (ent.Comp.State == RMCFusionReactorState.Weld)
        {
            _popup.PopupClient("You see no reason to attack the S-52 fusion reactor.", ent, user);
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
            _popup.PopupClient("You see no reason to attack the S-52 fusion reactor.", ent, user);
            return;
        }

        args.Handled = true;
        ent.Comp.State = ent.Comp.State switch
        {
            RMCFusionReactorState.Working => RMCFusionReactorState.Wrench,
            RMCFusionReactorState.Wrench => RMCFusionReactorState.Wire,
            RMCFusionReactorState.Wire => RMCFusionReactorState.Weld,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Dirty(ent);
        UpdateAppearance(ent);

        _popup.PopupClient("The S-52 fusion reactor gets torn apart!", ent, user, SmallCaution);

        if (ent.Comp.State != RMCFusionReactorState.Weld)
            args.Repeat = true;
    }

    private void OnFusionReactorExamined(Entity<RMCFusionReactorComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(RMCFusionReactorComponent)))
        {
            if (ent.Comp.State != RMCFusionReactorState.Working)
            {
                var tool = ent.Comp.State switch
                {
                    RMCFusionReactorState.Wrench => $"a [color=cyan]Wrench[/color]",
                    RMCFusionReactorState.Wire => $"[color=cyan]Wirecutters[/color]",
                    RMCFusionReactorState.Weld => $"a [color=cyan]Welder[/color]",
                    _ => throw new ArgumentOutOfRangeException(),
                };

                args.PushMarkup($"Use {tool} to repair it!");
            }


            if (!_container.TryGetContainer(ent, ent.Comp.CellContainerSlot, out var container) ||
                container.ContainedEntities.Count == 0)
            {
                args.PushMarkup("It needs a [color=cyan]fuel cell[/color]!");
            }
        }
    }

    private void OnApcSetChannelBuiMsg(Entity<RMCApcComponent> ent, ref RMCApcSetChannelBuiMsg args)
    {
        return; // TODO RMC14

        var channel = (int) args.Channel;
        if (args.Channel < 0 || channel >= ent.Comp.Channels.Length)
            return;

        ent.Comp.Channels[channel].On = args.On;
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

        var delay = ent.Comp.RepairDelay * _skills.GetSkillDelayMultiplier(user, ent.Comp.Skill);
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
            (float) delay.TotalSeconds,
            quality,
            new RMCFusionReactorRepairDoAfterEvent(state),
            ent.Comp.WeldingCost
        );

        if (!toolUsed)
            return;

        msg = Loc.GetString("rmc-fusion-reactor-repair-start-self", ("reactor", ent), ("tool", used));
        _popup.PopupClient(msg, ent, user);
    }

    private bool TryGetPowerArea(EntityUid ent, out Entity<RMCAreaPowerComponent> areaPower)
    {
        areaPower = default;
        if (!_area.TryGetArea(ent, out var area))
            return false;

        var areaPowerComp = EnsureComp<RMCAreaPowerComponent>(area);
        areaPower = (area, areaPowerComp);
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

        foreach (var apcId in area.Comp.Apcs)
        {
            if (!_apcQuery.TryComp(apcId, out var apc))
                continue;

            if (apc.Channels[(int)channel].On)
                return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
        {
            ToUpdate.Clear();
            return;
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
                    if (GetAreaReceivers(area, receiver.Channel).Add(update))
                    {
                        receiver.LastLoad = GetNewPowerLoad((update, receiver));
                        area.Comp.Load[(int) receiver.Channel] += receiver.LastLoad;
                        Dirty(area);
                    }

                    receiver.Area = area;
                    Dirty(update, receiver);
                }
            }
        }
        finally
        {
            ToUpdate.Clear();
        }
    }
}
