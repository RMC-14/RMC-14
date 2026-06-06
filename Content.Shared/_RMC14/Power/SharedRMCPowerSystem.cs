using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
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
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Melee;
using Content.Shared.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Numerics;
using static Content.Shared.Popups.PopupType;

namespace Content.Shared._RMC14.Power;

public abstract class SharedRMCPowerSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    [Dependency] protected readonly SharedPointLightSystem Pointlight = default!;

    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedRMCSpriteSystem _sprite = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedWiresSystem _wires = default!;

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
        SubscribeLocalEvent<RMCApcComponent, AttemptChangePanelEvent>(OnApcAttemptChangePanel);
        SubscribeLocalEvent<RMCApcComponent, PanelChangedEvent>(OnApcPanelChanged);
        SubscribeLocalEvent<RMCApcComponent, RMCApcInstallTerminalDoAfterEvent>(OnApcInstallTerminalDoAfter);
        SubscribeLocalEvent<RMCApcComponent, RMCApcRemoveTerminalDoAfterEvent>(OnApcRemoveTerminalDoAfter);
        SubscribeLocalEvent<RMCApcComponent, RMCApcInstallElectronicsDoAfterEvent>(OnApcInstallElectronicsDoAfter);
        SubscribeLocalEvent<RMCApcComponent, RMCApcRemoveElectronicsDoAfterEvent>(OnApcRemoveElectronicsDoAfter);
        SubscribeLocalEvent<RMCApcComponent, RMCApcSecureElectronicsDoAfterEvent>(OnApcSecureElectronicsDoAfter);
        SubscribeLocalEvent<RMCApcComponent, RMCApcUnsecureElectronicsDoAfterEvent>(OnApcUnsecureElectronicsDoAfter);
        SubscribeLocalEvent<RMCApcComponent, RMCApcRepairFrameDoAfterEvent>(OnApcRepairFrameDoAfter);
        SubscribeLocalEvent<RMCApcComponent, RMCApcDeconstructDoAfterEvent>(OnApcDeconstructDoAfter);

        SubscribeLocalEvent<RMCApcConstructionFrameComponent, ComponentStartup>(OnApcConstructionFrameStartup);
        SubscribeLocalEvent<RMCApcConstructionFrameComponent, MapInitEvent>(OnApcConstructionFrameUpdate);
        SubscribeLocalEvent<RMCApcConstructionFrameComponent, EntParentChangedMessage>(OnApcConstructionFrameUpdate);

        SubscribeLocalEvent<RMCPowerReceiverComponent, MapInitEvent>(OnReceiverMapInit);
        SubscribeLocalEvent<RMCPowerReceiverComponent, EntParentChangedMessage>(OnReceiverUpdate);
        SubscribeLocalEvent<RMCPowerReceiverComponent, AnchorStateChangedEvent>(OnReceiverUpdate);
        SubscribeLocalEvent<RMCPowerReceiverComponent, ComponentRemove>(OnReceiverRemove);
        SubscribeLocalEvent<RMCPowerReceiverComponent, EntityTerminatingEvent>(OnReceiverRemove);

        SubscribeLocalEvent<RMCFusionReactorComponent, MapInitEvent>(OnFusionReactorMapInit);
        SubscribeLocalEvent<RMCFusionReactorComponent, InteractUsingEvent>(OnFusionReactorInteractUsing);
        SubscribeLocalEvent<RMCFusionReactorComponent, RMCFusionReactorCellDoAfterEvent>(OnFusionReactorCellDoAfter);
        SubscribeLocalEvent<RMCFusionReactorComponent, RMCFusionReactorRemoveCellDoAfterEvent>(OnFusionReactorRemoveCellDoAfter);
        SubscribeLocalEvent<RMCFusionReactorComponent, RMCFusionReactorRepairDoAfterEvent>(OnFusionReactorRepairWeldingDoAfter);
        SubscribeLocalEvent<RMCFusionReactorComponent, RMCFusionReactorOverloadDoAfterEvent>(OnFusionReactorOverloadDoAfter);
        SubscribeLocalEvent<RMCFusionReactorComponent, InteractHandEvent>(OnFusionReactorInteractHand);
        SubscribeLocalEvent<RMCFusionReactorComponent, RMCFusionReactorStopOverloadDoAfterEvent>(OnFusionReactorStopOverloadDoAfter);
        SubscribeLocalEvent<RMCFusionReactorComponent, RMCFusionReactorDestroyDoAfterEvent>(OnFusionReactorDestroyDoAfter);
        SubscribeLocalEvent<RMCFusionReactorComponent, ExaminedEvent>(OnFusionReactorExamined);

        SubscribeLocalEvent<RMCReactorPoweredLightComponent, MapInitEvent>(OnReactorPoweredLightMapInit);

        Subs.BuiEvents<RMCApcComponent>(RMCApcUiKey.Key,
            subs =>
            {
                subs.Event<RMCApcSetChannelBuiMsg>(OnApcSetChannelBuiMsg);
                subs.Event<RMCApcCoverBuiMsg>(OnApcCover);
                subs.Event<RMCApcMainBreakerBuiMsg>(OnApcMainBreaker);
                subs.Event<RMCApcChargeModeBuiMsg>(OnApcChargeMode);
            });
    }

    private void OnApcStartup(Entity<RMCApcComponent> ent, ref ComponentStartup args)
    {
        OffsetApcVisual(ent);
    }

    private void OnApcConstructionFrameStartup(Entity<RMCApcConstructionFrameComponent> ent, ref ComponentStartup args)
    {
        UpdateConstructionApcVisual(ent);
    }

    private void OnApcConstructionFrameUpdate<T>(Entity<RMCApcConstructionFrameComponent> ent, ref T args)
    {
        UpdateConstructionApcVisual(ent);
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
            _metaData.SetEntityName(ent, Loc.GetString("rmc-apc-area-name", ("area", areaProto.Name)));

        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.CellContainerSlot);
        if (ent.Comp.StartingCell is { } startingCell &&
            container.ContainedEntity == null)
        {
            TrySpawnInContainer(startingCell, ent, ent.Comp.CellContainerSlot, out _);
        }

        UpdateApcAppearance(ent);
        OffsetApcVisual(ent);
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
        CloseApcWiresPanel(ent);
        ent.Comp.WiresExposed = true;
        ent.Comp.MainPowerWirePulsed = false;
        SetAllApcChannels(ent, false);
        Dirty(ent);

        UpdateApcAppearance(ent);
    }

    private void OnApcInteractUsing(Entity<RMCApcComponent> ent, ref InteractUsingEvent args)
    {
        var user = args.User;
        var used = args.Used;

        if (TryForceOpenBrokenApc(ent, user, used))
        {
            args.Handled = true;
            return;
        }

        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
        {
            PopupApcSkillFail(ent, user);
            return;
        }

        if (TryUseApcFrame(ent, user, used) ||
            TryUseApcCrowbar(ent, user, used) ||
            TryUseApcCell(ent, user, used) ||
            TryUseApcAccess(ent, user, used) ||
            TryUseApcCable(ent, user, used) ||
            TryUseApcWirecutters(ent, user, used) ||
            TryUseApcElectronics(ent, user, used) ||
            TryUseApcScrewdriver(ent, user, used) ||
            TryUseApcWelder(ent, user, used))
        {
            args.Handled = true;
        }
    }

    private void OnApcInteractHand(Entity<RMCApcComponent> ent, ref InteractHandEvent args)
    {
        if (ent.Comp.Cover == RMCApcCover.Closed ||
            !HasApcCell(ent))
            return;

        if (!_skills.HasSkill(args.User, ent.Comp.Skill, ent.Comp.SkillLevel))
        {
            PopupApcSkillFail(ent, args.User);
            return;
        }

        if (!_container.TryGetContainer(ent, ent.Comp.CellContainerSlot, out var container))
            return;

        foreach (var contained in container.ContainedEntities)
        {
            if (_container.Remove(contained, container))
            {
                _hands.TryPickupAnyHand(args.User, contained);

                ent.Comp.ChargePercentage = 0;
                Dirty(ent);

                UpdateApcAppearance(ent);
                ToUpdate.Add(ent);
                args.Handled = true;
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
            PopupApcSkillFail(ent, args.User);
            return;
        }

        if (!CanApcOpenUi(ent))
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
                RMCApcState.Working => Loc.GetString("rmc-apc-examine-working"),
                RMCApcState.WiresExposed => Loc.GetString("rmc-apc-examine-wires-exposed"),
                RMCApcState.CoverOpenBattery => Loc.GetString("rmc-apc-examine-cover-open-battery"),
                RMCApcState.CoverOpenNoBattery => GetApcOpenNoBatteryExamine(ent),
                RMCApcState.CoverRemovedBattery => Loc.GetString("rmc-apc-examine-cover-removed-battery"),
                RMCApcState.CoverRemovedNoBattery => GetApcOpenNoBatteryExamine(ent),
                RMCApcState.Broken => Loc.GetString("rmc-apc-examine-broken"),
                RMCApcState.BrokenCoverRemovedBattery => Loc.GetString("rmc-apc-examine-broken-cover-removed-battery"),
                RMCApcState.BrokenCoverRemovedNoBattery => Loc.GetString("rmc-apc-examine-broken-cover-removed-no-battery"),
                RMCApcState.TerminalMissing => GetApcOpenNoBatteryExamine(ent),
                RMCApcState.Maintenance => Loc.GetString("rmc-apc-examine-maintenance"),
                _ => null,
            };

            if (markup != null)
                args.PushMarkup(markup);
        }
    }

    private void OnApcAttemptChangePanel(Entity<RMCApcComponent> ent, ref AttemptChangePanelEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.User != null &&
            !_skills.HasSkill(args.User.Value, ent.Comp.Skill, ent.Comp.SkillLevel))
        {
            args.Cancelled = true;
            return;
        }

        if (args.Open)
            return;

        if (ent.Comp.Cover != RMCApcCover.Closed ||
            ent.Comp.Broken ||
            IsApcMaintenance(ent))
        {
            args.Cancelled = true;
        }
    }

    private void OnApcPanelChanged(Entity<RMCApcComponent> ent, ref PanelChangedEvent args)
    {
        ent.Comp.WiresExposed = args.Open;
        Dirty(ent);
        UpdateApcAppearance(ent);
    }

    private void OnApcInstallTerminalDoAfter(Entity<RMCApcComponent> ent, ref RMCApcInstallTerminalDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used == null)
            return;

        args.Handled = true;

        if (ent.Comp.Cover == RMCApcCover.Closed ||
            ent.Comp.TerminalInstalled ||
            ent.Comp.Electronics != RMCApcElectronics.Missing ||
            !TryComp(args.Used, out StackComponent? stack) ||
            stack.StackTypeId != ent.Comp.CableStack ||
            !_stack.Use(args.Used.Value, ent.Comp.CableAmount, stack))
        {
            return;
        }

        ent.Comp.TerminalInstalled = true;
        Dirty(ent);
        UpdateApcAppearance(ent);
    }

    private void OnApcRemoveTerminalDoAfter(Entity<RMCApcComponent> ent, ref RMCApcRemoveTerminalDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Cover == RMCApcCover.Closed ||
            !ent.Comp.TerminalInstalled ||
            ent.Comp.Electronics != RMCApcElectronics.Missing)
        {
            return;
        }

        ent.Comp.TerminalInstalled = false;
        SpawnNextToOrDrop(ent.Comp.CablePrototype, ent);
        Dirty(ent);
        UpdateApcAppearance(ent);
    }

    private void OnApcInstallElectronicsDoAfter(Entity<RMCApcComponent> ent, ref RMCApcInstallElectronicsDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used == null)
            return;

        args.Handled = true;

        if (ent.Comp.Cover == RMCApcCover.Closed ||
            ent.Comp.Broken ||
            !ent.Comp.TerminalInstalled ||
            ent.Comp.Electronics != RMCApcElectronics.Missing ||
            Prototype(args.Used.Value)?.ID != ent.Comp.ElectronicsPrototype.Id)
        {
            return;
        }

        QueueDel(args.Used.Value);
        ent.Comp.Electronics = RMCApcElectronics.Inserted;
        Dirty(ent);
        UpdateApcAppearance(ent);
    }

    private void OnApcRemoveElectronicsDoAfter(Entity<RMCApcComponent> ent, ref RMCApcRemoveElectronicsDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Cover == RMCApcCover.Closed ||
            ent.Comp.Electronics == RMCApcElectronics.Missing ||
            HasApcCell(ent))
        {
            return;
        }

        var broken = ent.Comp.Broken;
        ent.Comp.Electronics = RMCApcElectronics.Missing;
        if (!broken)
            SpawnNextToOrDrop(ent.Comp.ElectronicsPrototype, ent);

        Dirty(ent);
        UpdateApcAppearance(ent);
    }

    private void OnApcSecureElectronicsDoAfter(Entity<RMCApcComponent> ent, ref RMCApcSecureElectronicsDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Cover == RMCApcCover.Closed ||
            ent.Comp.Broken ||
            HasApcCell(ent) ||
            !ent.Comp.TerminalInstalled ||
            ent.Comp.Electronics != RMCApcElectronics.Inserted)
        {
            return;
        }

        ent.Comp.Electronics = RMCApcElectronics.Secured;
        Dirty(ent);
        UpdateApcAppearance(ent);
        ToUpdate.Add(ent);
    }

    private void OnApcUnsecureElectronicsDoAfter(Entity<RMCApcComponent> ent, ref RMCApcUnsecureElectronicsDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Cover == RMCApcCover.Closed ||
            ent.Comp.Broken ||
            HasApcCell(ent) ||
            ent.Comp.Electronics != RMCApcElectronics.Secured)
        {
            return;
        }

        ent.Comp.Electronics = RMCApcElectronics.Inserted;
        Dirty(ent);
        UpdateApcAppearance(ent);
        ToUpdate.Add(ent);
    }

    private void OnApcRepairFrameDoAfter(Entity<RMCApcComponent> ent, ref RMCApcRepairFrameDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used == null)
            return;

        args.Handled = true;

        if (!ent.Comp.Broken ||
            ent.Comp.Cover == RMCApcCover.Closed ||
            ent.Comp.Electronics != RMCApcElectronics.Missing ||
            HasApcCell(ent) ||
            !HasComp<RMCApcFrameComponent>(args.Used.Value))
        {
            return;
        }

        QueueDel(args.Used.Value);
        ent.Comp.Broken = false;
        ent.Comp.Cover = RMCApcCover.Open;
        ent.Comp.TerminalInstalled = false;
        ent.Comp.Electronics = RMCApcElectronics.Missing;
        CloseApcWiresPanel(ent);
        ent.Comp.WiresExposed = false;
        ent.Comp.MainPowerWireCut = false;
        ent.Comp.MainPowerWirePulsed = false;
        ent.Comp.IdScannerWireCut = false;

        if (TryComp(ent, out DamageableComponent? damageable))
            _damageable.SetAllDamage(ent, damageable, FixedPoint2.Zero);

        Dirty(ent);
        UpdateApcAppearance(ent);
        ToUpdate.Add(ent);
    }

    private void OnApcDeconstructDoAfter(Entity<RMCApcComponent> ent, ref RMCApcDeconstructDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Cover == RMCApcCover.Closed ||
            ent.Comp.TerminalInstalled ||
            ent.Comp.Electronics != RMCApcElectronics.Missing ||
            HasApcCell(ent))
        {
            return;
        }

        SpawnNextToOrDrop("CMSheetMetal1", ent);
        SpawnNextToOrDrop("CMSheetMetal1", ent);
        QueueDel(ent);
    }

    private bool TryForceOpenBrokenApc(Entity<RMCApcComponent> ent, EntityUid user, EntityUid used)
    {
        if (!ent.Comp.Broken ||
            ent.Comp.Cover != RMCApcCover.Closed ||
            !TryComp(used, out MeleeWeaponComponent? melee) ||
            melee.Damage.GetTotal() < FixedPoint2.New(5))
        {
            return false;
        }

        ent.Comp.Cover = RMCApcCover.Removed;
        ent.Comp.CoverLockedButton = false;
        Dirty(ent);
        UpdateApcAppearance(ent);
        _popup.PopupClient(Loc.GetString("rmc-apc-knock-cover-off", ("apc", ent)), ent, user);
        return true;
    }

    private bool TryUseApcFrame(Entity<RMCApcComponent> ent, EntityUid user, EntityUid used)
    {
        if (!HasComp<RMCApcFrameComponent>(used))
            return false;

        if (!ent.Comp.Broken)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-repair-frame-not-needed", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        if (ent.Comp.Cover == RMCApcCover.Closed)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-damaged-cover-in-way", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        if (ent.Comp.Electronics != RMCApcElectronics.Missing)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-remove-damaged-electronics-first", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        if (HasApcCell(ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-remove-battery-first", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        return TryStartApcDoAfter(ent, user, used, ent.Comp.RepairFrameDelay, new RMCApcRepairFrameDoAfterEvent());
    }

    private bool TryUseApcCrowbar(Entity<RMCApcComponent> ent, EntityUid user, EntityUid used)
    {
        if (!_tool.HasQuality(used, ent.Comp.CrowbarTool))
            return false;

        if (ent.Comp.Cover == RMCApcCover.Closed)
        {
            if (ent.Comp.Broken)
            {
                _popup.PopupClient(Loc.GetString("rmc-apc-damaged-cover-knock-loose", ("apc", ent)), ent, user, SmallCaution);
                return true;
            }

            if (ent.Comp.CoverLockedButton && !IsApcMaintenance(ent))
            {
                var message = ent.Comp.WiresExposed
                    ? Loc.GetString("rmc-apc-cover-lock-still-engaged")
                    : Loc.GetString("rmc-apc-cover-locked");
                _popup.PopupClient(message, user, user, MediumCaution);
                return true;
            }

            ent.Comp.Cover = RMCApcCover.Open;
            CloseApcWiresPanel(ent);
            ent.Comp.WiresExposed = false;
            Dirty(ent);
            UpdateApcAppearance(ent);
            return true;
        }

        if (ent.Comp.Electronics == RMCApcElectronics.Inserted ||
            (ent.Comp.Broken && ent.Comp.Electronics != RMCApcElectronics.Missing))
        {
            if (ent.Comp.Broken && HasApcCell(ent))
            {
                _popup.PopupClient(Loc.GetString("rmc-apc-remove-battery-first", ("apc", ent)), ent, user, SmallCaution);
                return true;
            }

            if (TryStartApcDoAfter(ent, user, used, ent.Comp.RemoveElectronicsDelay, new RMCApcRemoveElectronicsDoAfterEvent()))
            {
                PlayApcSound(ent, ent.Comp.CrowbarSound, user);
                return true;
            }

            return false;
        }

        if (ent.Comp.Cover == RMCApcCover.Removed &&
            ent.Comp.Electronics == RMCApcElectronics.Secured)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-unsecure-electronics-first", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        if (ent.Comp.Cover == RMCApcCover.Open)
        {
            ent.Comp.Cover = RMCApcCover.Closed;
            Dirty(ent);
            UpdateApcAppearance(ent);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-cover-missing", ("apc", ent)), ent, user, SmallCaution);
        }

        return true;
    }

    private bool TryUseApcCell(Entity<RMCApcComponent> ent, EntityUid user, EntityUid used)
    {
        if (!HasComp<PowerCellComponent>(used))
            return false;

        if (ent.Comp.Cover == RMCApcCover.Closed ||
            ent.Comp.Broken ||
            IsApcMaintenance(ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-battery-connector-not-ready", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.CellContainerSlot);
        if (container.ContainedEntity != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-already-has-battery", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        _hands.TryDropIntoContainer(user, used, container);
        if (container.ContainedEntity != null)
        {
            Dirty(ent);
            UpdateApcAppearance(ent);
            ToUpdate.Add(ent);
        }

        return true;
    }

    private bool TryUseApcAccess(Entity<RMCApcComponent> ent, EntityUid user, EntityUid used)
    {
        if (!TryComp(ent, out AccessReaderComponent? reader))
            return false;

        var looksLikeAccessItem = TryComp(used, out AccessComponent? _) ||
                                  _accessReader.IsAllowed(used, ent, reader);
        if (!looksLikeAccessItem)
        {
            return false;
        }

        if (ent.Comp.Cover != RMCApcCover.Closed ||
            ent.Comp.WiresExposed ||
            ent.Comp.Broken ||
            IsApcMaintenance(ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-nothing-happens", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        if (ent.Comp.IdScannerWireCut)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-id-scanner-wire-cut", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        if (!_accessReader.IsAllowed(used, ent, reader))
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-access-denied", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        ent.Comp.Locked = !ent.Comp.Locked;
        Dirty(ent);
        UpdateApcAppearance(ent);
        return true;
    }

    private bool TryUseApcCable(Entity<RMCApcComponent> ent, EntityUid user, EntityUid used)
    {
        if (!TryComp(used, out StackComponent? stack) ||
            stack.StackTypeId != ent.Comp.CableStack)
        {
            return false;
        }

        if (ent.Comp.Cover == RMCApcCover.Closed ||
            ent.Comp.TerminalInstalled ||
            ent.Comp.Electronics != RMCApcElectronics.Missing)
        {
            return false;
        }

        if (stack.Count < ent.Comp.CableAmount)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-need-cable", ("amount", ent.Comp.CableAmount)), ent, user, SmallCaution);
            return true;
        }

        if (TryStartApcDoAfter(ent, user, used, ent.Comp.InstallTerminalDelay, new RMCApcInstallTerminalDoAfterEvent()))
        {
            PlayApcSound(ent, ent.Comp.DeconstructSound, user);
            return true;
        }

        return false;
    }

    private bool TryUseApcWirecutters(Entity<RMCApcComponent> ent, EntityUid user, EntityUid used)
    {
        if (!_tool.HasQuality(used, ent.Comp.CuttingTool))
            return false;

        if (ent.Comp.Cover == RMCApcCover.Closed ||
            !ent.Comp.TerminalInstalled ||
            ent.Comp.Electronics != RMCApcElectronics.Missing)
        {
            return false;
        }

        if (TryStartApcDoAfter(ent, user, used, ent.Comp.RemoveTerminalDelay, new RMCApcRemoveTerminalDoAfterEvent()))
        {
            PlayApcSound(ent, ent.Comp.DeconstructSound, user);
            return true;
        }

        return false;
    }

    private bool TryUseApcElectronics(Entity<RMCApcComponent> ent, EntityUid user, EntityUid used)
    {
        if (Prototype(used)?.ID != ent.Comp.ElectronicsPrototype.Id)
            return false;

        if (ent.Comp.Cover == RMCApcCover.Closed)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-open-cover-first", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        if (ent.Comp.Broken)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-damaged-frame-no-new-module", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        if (!ent.Comp.TerminalInstalled)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-terminal-not-connected", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        if (ent.Comp.Electronics != RMCApcElectronics.Missing)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-already-has-module", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        if (TryStartApcDoAfter(ent, user, used, ent.Comp.InstallElectronicsDelay, new RMCApcInstallElectronicsDoAfterEvent()))
        {
            PlayApcSound(ent, ent.Comp.DeconstructSound, user);
            return true;
        }

        return false;
    }

    private bool TryUseApcScrewdriver(Entity<RMCApcComponent> ent, EntityUid user, EntityUid used)
    {
        if (!_tool.HasQuality(used, ent.Comp.RepairTool))
            return false;

        if (ent.Comp.Cover == RMCApcCover.Closed)
            return false;

        if (ent.Comp.Broken)
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-damaged-electronics-cannot-secure", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        if (HasApcCell(ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-apc-remove-battery-first", ("apc", ent)), ent, user, SmallCaution);
            return true;
        }

        switch (ent.Comp.Electronics)
        {
            case RMCApcElectronics.Secured:
                if (TryStartApcDoAfter(ent, user, used, ent.Comp.SecureElectronicsDelay, new RMCApcUnsecureElectronicsDoAfterEvent()))
                {
                    PlayApcSound(ent, ent.Comp.ScrewdriverSound, user);
                    return true;
                }

                return false;
            case RMCApcElectronics.Inserted when !ent.Comp.TerminalInstalled:
                _popup.PopupClient(Loc.GetString("rmc-apc-terminal-not-connected", ("apc", ent)), ent, user, SmallCaution);
                return true;
            case RMCApcElectronics.Inserted:
                if (TryStartApcDoAfter(ent, user, used, ent.Comp.SecureElectronicsDelay, new RMCApcSecureElectronicsDoAfterEvent()))
                {
                    PlayApcSound(ent, ent.Comp.ScrewdriverSound, user);
                    return true;
                }

                return false;
            default:
                _popup.PopupClient(Loc.GetString("rmc-apc-nothing-to-secure", ("apc", ent)), ent, user, SmallCaution);
                return true;
        }
    }

    private bool TryUseApcWelder(Entity<RMCApcComponent> ent, EntityUid user, EntityUid used)
    {
        if (!_tool.HasQuality(used, ent.Comp.WeldingTool))
            return false;

        if (ent.Comp.Cover == RMCApcCover.Closed ||
            ent.Comp.TerminalInstalled ||
            ent.Comp.Electronics != RMCApcElectronics.Missing ||
            HasApcCell(ent))
        {
            return false;
        }

        if (_tool.UseTool(
                used,
                user,
                ent,
                ent.Comp.DeconstructDelay,
                new[] { ent.Comp.WeldingTool.Id },
                new RMCApcDeconstructDoAfterEvent(),
                out _,
                duplicateCondition: DuplicateConditions.SameTool))
        {
            PlayApcSound(ent, ent.Comp.WelderSound, user);
            return true;
        }

        return false;
    }

    private void PlayApcSound(Entity<RMCApcComponent> ent, SoundSpecifier sound, EntityUid user)
    {
        _audio.PlayPredicted(sound, ent, user);
    }

    private bool TryStartApcDoAfter(Entity<RMCApcComponent> ent, EntityUid user, EntityUid used, TimeSpan delay, SimpleDoAfterEvent ev)
    {
        var scaledDelay = delay * _skills.GetSkillDelayMultiplier(user, ent.Comp.Skill);
        var doAfter = new DoAfterArgs(EntityManager, user, scaledDelay, ev, ent, used: used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = true,
            DuplicateCondition = DuplicateConditions.SameTool,
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    private bool HasApcCell(Entity<RMCApcComponent> ent)
    {
        return _container.TryGetContainer(ent, ent.Comp.CellContainerSlot, out var container) &&
               container.ContainedEntities.Count > 0;
    }

    public bool CanApcOperate(EntityUid uid, RMCApcComponent? comp = null)
    {
        if (!_apcQuery.Resolve(uid, ref comp, false))
            return false;

        return !comp.Broken &&
               comp.TerminalInstalled &&
               comp.Electronics == RMCApcElectronics.Secured &&
               comp.MainBreakerButton &&
               !comp.MainPowerWireCut &&
               !comp.MainPowerWirePulsed;
    }

    private bool CanApcOpenUi(Entity<RMCApcComponent> ent)
    {
        return ent.Comp.Cover == RMCApcCover.Closed &&
               !ent.Comp.WiresExposed &&
               !ent.Comp.Broken &&
               ent.Comp.TerminalInstalled &&
               ent.Comp.Electronics == RMCApcElectronics.Secured;
    }

    private bool IsApcMaintenance(Entity<RMCApcComponent> ent)
    {
        return !ent.Comp.TerminalInstalled ||
               ent.Comp.Electronics != RMCApcElectronics.Secured;
    }

    private string GetApcOpenNoBatteryExamine(Entity<RMCApcComponent> ent)
    {
        if (!ent.Comp.TerminalInstalled)
            return Loc.GetString("rmc-apc-examine-connect-terminal", ("amount", ent.Comp.CableAmount));

        return ent.Comp.Electronics switch
        {
            RMCApcElectronics.Missing => Loc.GetString("rmc-apc-examine-install-electronics"),
            RMCApcElectronics.Inserted => Loc.GetString("rmc-apc-examine-secure-electronics"),
            _ => Loc.GetString("rmc-apc-examine-insert-battery"),
        };
    }

    private void PopupApcSkillFail(Entity<RMCApcComponent> ent, EntityUid user)
    {
        _popup.PopupClient(Loc.GetString("rmc-apc-skill-fail", ("apc", ent)), ent, user, SmallCaution);
    }

    private void UpdateApcAppearance(Entity<RMCApcComponent> ent)
    {
        var state = GetApcVisualState(ent);
        ent.Comp.State = state;

        _appearance.SetData(ent, RMCApcVisualsLayers.Layer, state);
        _appearance.SetData(ent, RMCApcVisualsLayers.Lock, ent.Comp.Locked);
        UpdateApcChannelVisuals(ent);
        Dirty(ent);
    }

    private void CloseApcWiresPanel(Entity<RMCApcComponent> ent)
    {
        if (TryComp(ent, out WiresPanelComponent? panel) && panel.Open)
            _wires.TogglePanel(ent, panel, false);
    }

    private RMCApcState GetApcVisualState(Entity<RMCApcComponent> ent)
    {
        var hasCell = HasApcCell(ent);
        if (ent.Comp.Broken)
        {
            return ent.Comp.Cover == RMCApcCover.Removed
                ? hasCell ? RMCApcState.BrokenCoverRemovedBattery : RMCApcState.BrokenCoverRemovedNoBattery
                : RMCApcState.Broken;
        }

        if (ent.Comp.WiresExposed)
            return RMCApcState.WiresExposed;

        if (!ent.Comp.TerminalInstalled)
            return RMCApcState.TerminalMissing;

        if (ent.Comp.Cover == RMCApcCover.Removed)
            return hasCell ? RMCApcState.CoverRemovedBattery : RMCApcState.CoverRemovedNoBattery;

        if (ent.Comp.Cover == RMCApcCover.Open)
            return hasCell ? RMCApcState.CoverOpenBattery : RMCApcState.CoverOpenNoBattery;

        return IsApcMaintenance(ent)
            ? RMCApcState.Maintenance
            : RMCApcState.Working;
    }

    private void UpdateApcChannelVisuals(Entity<RMCApcComponent> apc)
    {
        UpdateApcChannelVisual(apc, RMCPowerChannel.Equipment);
        UpdateApcChannelVisual(apc, RMCPowerChannel.Lighting);
        UpdateApcChannelVisual(apc, RMCPowerChannel.Environment);
    }

    private void UpdateApcChannelVisual(Entity<RMCApcComponent> apc, RMCPowerChannel channel)
    {
        var visual = GetApcChannelVisualState(apc.Comp.Channels[(int) channel]);
        switch (channel)
        {
            case RMCPowerChannel.Equipment:
                _appearance.SetData(apc, RMCApcVisualsLayers.EquipmentChannel, visual);
                break;
            case RMCPowerChannel.Lighting:
                _appearance.SetData(apc, RMCApcVisualsLayers.LightingChannel, visual);
                break;
            case RMCPowerChannel.Environment:
                _appearance.SetData(apc, RMCApcVisualsLayers.EnvironmentChannel, visual);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
        }
    }

    private static RMCApcChannelVisualState GetApcChannelVisualState(RMCApcChannel channel)
    {
        return channel.Button switch
        {
            RMCApcButtonState.Off => RMCApcChannelVisualState.ManualOff,
            RMCApcButtonState.On => channel.On
                ? RMCApcChannelVisualState.ManualOn
                : RMCApcChannelVisualState.ManualOff,
            RMCApcButtonState.Auto => channel.On
                ? RMCApcChannelVisualState.AutoOn
                : RMCApcChannelVisualState.AutoOff,
            _ => throw new ArgumentOutOfRangeException(nameof(channel.Button), channel.Button, null),
        };
    }

    protected void SetAllApcChannels(Entity<RMCApcComponent> apc, bool on)
    {
        for (var i = 0; i < apc.Comp.Channels.Length; i++)
        {
            apc.Comp.Channels[i].On = on;
        }

        UpdateApcChannelVisuals(apc);
    }

    public void SetApcMainPowerWireCut(Entity<RMCApcComponent?> ent, bool cut)
    {
        if (!_apcQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.MainPowerWireCut = cut;
        Dirty(ent);
        ToUpdate.Add(ent);
    }

    public void SetApcMainPowerWirePulsed(Entity<RMCApcComponent?> ent, bool pulsed)
    {
        if (!_apcQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.MainPowerWirePulsed = pulsed;
        Dirty(ent);
        ToUpdate.Add(ent);
    }

    public void SetApcIdScannerWireCut(Entity<RMCApcComponent?> ent, bool cut)
    {
        if (!_apcQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.IdScannerWireCut = cut;
        Dirty(ent);
    }

    public void PulseApcIdScanner(Entity<RMCApcComponent?> ent)
    {
        if (!_apcQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Locked = false;
        Dirty(ent);
        UpdateApcAppearance((ent.Owner, ent.Comp));
    }

    public void ResetApcIdScannerPulse(Entity<RMCApcComponent?> ent)
    {
        if (!_apcQuery.Resolve(ent, ref ent.Comp, false) ||
            ent.Comp.IdScannerWireCut)
        {
            return;
        }

        ent.Comp.Locked = true;
        Dirty(ent);
        UpdateApcAppearance((ent.Owner, ent.Comp));
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
        if (HasActiveFusionReactorDoAfter(user))
            return;

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
        else if (_tool.HasQuality(used, ent.Comp.OverloadQuality))
        {
            TryStartFusionReactorOverload(ent, user, used);
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

        SetFusionReactorOverloaded(ent, false, user);

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

        SetFusionReactorOverloaded(ent, false, args.User);

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

        if (HasActiveFusionReactorDoAfter(user))
            return;

        if (ent.Comp.Overloaded)
        {
            args.Handled = true;
            var stopOverloadEv = new RMCFusionReactorStopOverloadDoAfterEvent();
            var stopOverloadDoAfter = new DoAfterArgs(EntityManager, user, ent.Comp.XenoOverloadStopDelay, stopOverloadEv, ent, ent)
            {
                BreakOnMove = true,
                DuplicateCondition = DuplicateConditions.SameEvent,
            };

            _doAfter.TryStartDoAfter(stopOverloadDoAfter);
            return;
        }

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

    private void OnFusionReactorStopOverloadDoAfter(Entity<RMCFusionReactorComponent> ent, ref RMCFusionReactorStopOverloadDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || args.Handled)
            return;

        if (!ent.Comp.Overloaded || !HasComp<XenoComponent>(user) || !HasComp<MeleeWeaponComponent>(user))
            return;

        args.Handled = true;
        SetFusionReactorOverloaded(ent, false, user);
        _popup.PopupPredicted(
            Loc.GetString("rmc-fusion-reactor-overload-stop-xeno", ("reactor", ent)),
            Loc.GetString("rmc-fusion-reactor-overload-stop-xeno-others", ("xeno", user), ("reactor", ent)),
            ent,
            user,
            MediumCaution);
        _audio.PlayPredicted(ent.Comp.OverloadStopSound, ent, user);
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
        SetFusionReactorOverloaded(ent, false, user);

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

    public void FullyDestroy(Entity<RMCFusionReactorComponent> ent)
    {
        SetFusionReactorOverloaded(ent, false);

        ent.Comp.State = RMCFusionReactorState.Weld;
        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void OnFusionReactorExamined(Entity<RMCFusionReactorComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(RMCFusionReactorComponent)))
        {
            if (ent.Comp.State != RMCFusionReactorState.Working)
            {
                var msg = ent.Comp.State switch
                {
                    RMCFusionReactorState.Wrench => "rmc-fusion-reactor-examine-repair-wrench",
                    RMCFusionReactorState.Wire => "rmc-fusion-reactor-examine-repair-wirecutters",
                    RMCFusionReactorState.Weld => "rmc-fusion-reactor-examine-repair-welder",
                    _ => throw new ArgumentOutOfRangeException(),
                };

                args.PushMarkup(Loc.GetString(msg));
            }

            if (!_container.TryGetContainer(ent, ent.Comp.CellContainerSlot, out var container) ||
                container.ContainedEntities.Count == 0)
            {
                args.PushMarkup(Loc.GetString("rmc-fusion-reactor-examine-needs-cell"));
            }

            var overload = new RMCFusionReactorOverloadStatusEvent(ent, args.Examiner);
            RaiseLocalEvent(ent.Owner, ref overload, true);
            if (overload.Text != null)
                args.PushMarkup(overload.Text);
            else if (ent.Comp.Overloaded)
                args.PushMarkup(Loc.GetString("rmc-fusion-reactor-overload-examine"));
        }
    }

    private void OnReactorPoweredLightMapInit(Entity<RMCReactorPoweredLightComponent> ent, ref MapInitEvent args)
    {
        if (TryComp(ent, out TransformComponent? xform))
            _reactorPoweredLights.GetOrNew(xform.MapID).Add(ent);
    }

    private void OnApcSetChannelBuiMsg(Entity<RMCApcComponent> ent, ref RMCApcSetChannelBuiMsg args)
    {
        var channel = (int) args.Channel;
        if (ent.Comp.Locked ||
            !CanApcOpenUi(ent) ||
            args.Channel < 0 ||
            channel >= ent.Comp.Channels.Length)
        {
            return;
        }

        ent.Comp.Channels[channel].Button = args.State;
        Dirty(ent);
        ToUpdate.Add(ent);
    }

    private void OnApcCover(Entity<RMCApcComponent> ent, ref RMCApcCoverBuiMsg args)
    {
        if (!CanApcOpenUi(ent) ||
            ent.Comp.Locked)
        {
            return;
        }

        ent.Comp.CoverLockedButton = !ent.Comp.CoverLockedButton;
        Dirty(ent);
    }

    private void OnApcMainBreaker(Entity<RMCApcComponent> ent, ref RMCApcMainBreakerBuiMsg args)
    {
        if (!CanApcOpenUi(ent) ||
            ent.Comp.Locked)
        {
            return;
        }

        ent.Comp.MainBreakerButton = !ent.Comp.MainBreakerButton;
        Dirty(ent);
        ToUpdate.Add(ent);
    }

    private void OnApcChargeMode(Entity<RMCApcComponent> ent, ref RMCApcChargeModeBuiMsg args)
    {
        if (!CanApcOpenUi(ent) ||
            ent.Comp.Locked)
        {
            return;
        }

        ent.Comp.ChargeModeButton = !ent.Comp.ChargeModeButton;
        Dirty(ent);
        ToUpdate.Add(ent);
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

        if (ent.Comp.Overloaded)
        {
            _appearance.SetData(ent, RMCFusionReactorLayers.Layer, RMCFusionReactorVisuals.Overloaded);
            return;
        }

        // TODO RMC14 fuel use
        _appearance.SetData(ent, RMCFusionReactorLayers.Layer, RMCFusionReactorVisuals.Hundred);
    }

    private void TryStartFusionReactorOverload(Entity<RMCFusionReactorComponent> ent, EntityUid user, EntityUid used)
    {
        if (_net.IsClient)
            return;

        if (HasActiveFusionReactorDoAfter(user))
            return;

        if (!CanToggleFusionReactorOverload(ent, user, used))
            return;

        var ev = new RMCFusionReactorOverloadDoAfterEvent();
        var delay = ent.Comp.OverloadDelay * _skills.GetSkillDelayMultiplier(user, ent.Comp.OverloadSkill);
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent, used: used)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        var msg = Loc.GetString(ent.Comp.Overloaded
                ? "rmc-fusion-reactor-overload-start-disable"
                : "rmc-fusion-reactor-overload-start-enable",
            ("reactor", ent));
        _popup.PopupClient(msg, ent, user);
    }

    private void OnFusionReactorOverloadDoAfter(Entity<RMCFusionReactorComponent> ent, ref RMCFusionReactorOverloadDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } used)
            return;

        args.Handled = true;

        if (!CanToggleFusionReactorOverload(ent, args.User, used))
            return;

        var overloaded = !ent.Comp.Overloaded;
        SetFusionReactorOverloaded(ent, overloaded, args.User);

        var msg = Loc.GetString(overloaded
                ? "rmc-fusion-reactor-overload-enabled"
                : "rmc-fusion-reactor-overload-disabled",
            ("reactor", ent));
        _popup.PopupClient(msg, ent, args.User);
    }

    private bool CanToggleFusionReactorOverload(Entity<RMCFusionReactorComponent> ent, EntityUid user, EntityUid used)
    {
        if (!_tool.HasQuality(used, ent.Comp.OverloadQuality))
            return false;

        if (!_skills.HasSkill(user, ent.Comp.OverloadSkill, ent.Comp.OverloadSkillLevel))
            return false;

        if (ent.Comp.State != RMCFusionReactorState.Working)
            return false;

        if (!HasFusionReactorCell(ent))
            return false;

        var ev = new RMCFusionReactorCanOverloadEvent(ent, user);
        RaiseLocalEvent(ent.Owner, ref ev, true);
        if (!ev.CanOverload)
            return false;

        return true;
    }

    private bool HasFusionReactorCell(Entity<RMCFusionReactorComponent> ent)
    {
        return _container.TryGetContainer(ent, ent.Comp.CellContainerSlot, out var container) &&
               container.ContainedEntities.Count > 0;
    }

    private bool HasActiveFusionReactorDoAfter(EntityUid user)
    {
        if (!TryComp<DoAfterComponent>(user, out var doAfter))
            return false;

        foreach (var active in doAfter.DoAfters.Values)
        {
            if (active.Cancelled || active.Completed)
                continue;

            if (active.Args.Target is { } target &&
                HasComp<RMCFusionReactorComponent>(target))
            {
                return true;
            }

            if (active.Args.EventTarget is { } eventTarget &&
                HasComp<RMCFusionReactorComponent>(eventTarget))
            {
                return true;
            }
        }

        return false;
    }

    public void SetFusionReactorOverloaded(Entity<RMCFusionReactorComponent> ent, bool overloaded, EntityUid? user = null)
    {
        if (ent.Comp.Overloaded == overloaded)
            return;

        ent.Comp.Overloaded = overloaded;
        ent.Comp.OverloadNextFeedbackAt = TimeSpan.Zero;
        Dirty(ent);
        UpdateAppearance(ent);
        ReactorUpdated(ent);

        var ev = new RMCFusionReactorOverloadChangedEvent(ent, overloaded);
        RaiseLocalEvent(ent.Owner, ref ev, true);
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
        var desired = apcChannel.Button == RMCApcButtonState.Off
            ? false
            : on;

        if (apcChannel.On == desired)
        {
            UpdateApcChannelVisual(apc, channel);
            return;
        }

        apcChannel.On = desired;
        UpdateApcChannelVisual(apc, channel);

        PowerUpdated(area, channel, desired);
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

    private void OffsetApcVisual(EntityUid uid)
    {
        var sprite = EnsureComp<SpriteSetRenderOrderComponent>(uid);
        switch (Transform(uid).LocalRotation.GetDir())
        {
            case Direction.South:
                _sprite.SetOffset(uid, new Vector2(0.45f, -0.32f));
                break;
            case Direction.East:
                _sprite.SetOffset(uid, new Vector2(0.7f, -1.45f));
                break;
            case Direction.North:
                _sprite.SetOffset(uid, new Vector2(-0.5f, -1.5f));
                break;
            case Direction.West:
                _sprite.SetOffset(uid, new Vector2(-0.7f, -0.4f));
                break;
        }

        Dirty(uid, sprite);
    }

    private void UpdateConstructionApcVisual(EntityUid uid)
    {
        MoveConstructionApcOffWallTile(uid);
        OffsetApcVisual(uid);
    }

    private void MoveConstructionApcOffWallTile(EntityUid uid)
    {
        if (!TryComp(uid, out TransformComponent? xform))
            return;

        if (!HasWallAtCoordinates(xform.Coordinates))
            return;

        var direction = xform.LocalRotation.GetDir();
        if (direction == Direction.Invalid)
            return;

        var coordinates = xform.Coordinates.Offset(direction.ToVec());
        _transform.SetCoordinates((uid, xform, MetaData(uid)), coordinates, xform.LocalRotation, unanchor: false);
    }

    private bool HasWallAtCoordinates(EntityCoordinates coordinates)
    {
        foreach (var wall in _lookup.GetEntitiesInRange(coordinates, 0.2f, LookupFlags.Static))
        {
            if (_tags.HasTag(wall, WallTag))
                return true;
        }

        return false;
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
                while (lights.MoveNext(out var uid, out var poweredLight, out var xform))
                {
                    if (xform.MapID == map)
                    {
                        poweredLight.Enabled = powered;
                        Dirty(uid, poweredLight);
                        _appearance.SetData(uid, ToggleableVisuals.Enabled, powered);
                        Pointlight.SetEnabled(uid, powered);
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
