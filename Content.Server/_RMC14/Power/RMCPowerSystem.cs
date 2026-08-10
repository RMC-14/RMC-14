using Content.Server.Administration.Logs;
using Content.Server.Electrocution;
using Content.Server.Emp;
using Content.Server.Kitchen.Components;
using Content.Server.Lathe.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Power;
using Content.Shared.Examine;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.PowerCell;
using Content.Shared.SMES;
using Content.Shared.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using System.Linq;

namespace Content.Server._RMC14.Power;

public sealed class RMCPowerSystem : SharedRMCPowerSystem
{
    // CM13 power runs every two seconds and converts a power tick to cell charge with CELLRATE 0.006.
    // RMC power is delta-time based, so APCs convert their cells' legacy charge units at this boundary.
    private const float Cm13PowerTickSeconds = 2f;
    private const float Cm13CellRate = 0.006f;
    private const float Cm13ChargeLevel = 0.001f;
    private const float ApcCellJoulesPerCharge = Cm13PowerTickSeconds / Cm13CellRate;
    private const float ApcFullCharge = 0.98f;
    private const float EquipmentCutoff = 0.25f;
    private const float EquipmentRestore = 0.30f;
    private const float LightingCutoff = 0.15f;
    private const float LightingRestore = 0.20f;

    private static readonly SoundSpecifier BlackoutAnnouncementSound =
        new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/attention_jingle.ogg");
    private static readonly SoundSpecifier RestoreAnnouncementSound =
        new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/ares_online.ogg");

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private readonly Dictionary<RMCPowerNetworkKey, EntityUid> _networkEntities = new();
    private readonly Dictionary<RMCPowerNetworkKey, RMCPowerNetworkStats> _networkStats = new();
    private readonly HashSet<EntityUid> _trackedApcs = new();
    private readonly HashSet<EntityUid> _trackedMonitors = new();
    private readonly HashSet<EntityUid> _trackedSources = new();
    private readonly HashSet<EntityUid> _trackedStorages = new();

    [ViewVariables]
    private TimeSpan _lastUpdate;

    [ViewVariables]
    private bool _updateInitialized;

    [ViewVariables]
    private TimeSpan _nextUpdate;

    [ViewVariables]
    private TimeSpan _updateEvery;

    [ViewVariables]
    private float _powerLoadMultiplier;

    private EntityQuery<AppearanceComponent> _appearanceQuery;
    private EntityQuery<BatteryComponent> _batteryQuery;
    private EntityQuery<RMCApcComponent> _apcQuery;
    private EntityQuery<RMCAreaPowerComponent> _areaPowerQuery;
    private EntityQuery<RMCPowerReceiverComponent> _receiverQuery;
    private EntityQuery<RMCPowerMonitorComponent> _monitorQuery;
    private EntityQuery<RMCFusionCellComponent> _fusionCellQuery;
    private EntityQuery<RMCFusionReactorComponent> _fusionReactorQuery;
    private EntityQuery<RMCPowerSourceComponent> _sourceQuery;
    private EntityQuery<RMCPowerStorageComponent> _storageQuery;
    private EntityQuery<RMCSmesComponent> _smesQuery;

    public override void Initialize()
    {
        base.Initialize();

        _appearanceQuery = GetEntityQuery<AppearanceComponent>();
        _batteryQuery = GetEntityQuery<BatteryComponent>();
        _apcQuery = GetEntityQuery<RMCApcComponent>();
        _areaPowerQuery = GetEntityQuery<RMCAreaPowerComponent>();
        _receiverQuery = GetEntityQuery<RMCPowerReceiverComponent>();
        _monitorQuery = GetEntityQuery<RMCPowerMonitorComponent>();
        _fusionCellQuery = GetEntityQuery<RMCFusionCellComponent>();
        _fusionReactorQuery = GetEntityQuery<RMCFusionReactorComponent>();
        _sourceQuery = GetEntityQuery<RMCPowerSourceComponent>();
        _storageQuery = GetEntityQuery<RMCPowerStorageComponent>();
        _smesQuery = GetEntityQuery<RMCSmesComponent>();

        SubscribeLocalEvent<RMCApcComponent, EmpPulseEvent>(OnApcEmp);

        SubscribeLocalEvent<RMCPowerMonitorComponent, ComponentStartup>(OnMonitorTracked);
        SubscribeLocalEvent<RMCPowerMonitorComponent, MapInitEvent>(OnMonitorTracked);
        SubscribeLocalEvent<RMCPowerMonitorComponent, EntParentChangedMessage>(OnMonitorTracked);
        SubscribeLocalEvent<RMCPowerMonitorComponent, AnchorStateChangedEvent>(OnMonitorTracked);
        SubscribeLocalEvent<RMCPowerMonitorComponent, ComponentRemove>(OnMonitorRemoved);
        SubscribeLocalEvent<RMCPowerMonitorComponent, EntityTerminatingEvent>(OnMonitorRemoved);

        SubscribeLocalEvent<RMCPowerSourceComponent, ComponentStartup>(OnSourceTracked);
        SubscribeLocalEvent<RMCPowerSourceComponent, MapInitEvent>(OnSourceTracked);
        SubscribeLocalEvent<RMCPowerSourceComponent, EntParentChangedMessage>(OnSourceTracked);
        SubscribeLocalEvent<RMCPowerSourceComponent, AnchorStateChangedEvent>(OnSourceTracked);
        SubscribeLocalEvent<RMCPowerSourceComponent, ComponentRemove>(OnSourceRemoved);
        SubscribeLocalEvent<RMCPowerSourceComponent, EntityTerminatingEvent>(OnSourceRemoved);

        SubscribeLocalEvent<RMCPowerStorageComponent, ComponentStartup>(OnStorageTracked);
        SubscribeLocalEvent<RMCPowerStorageComponent, MapInitEvent>(OnStorageTracked);
        SubscribeLocalEvent<RMCPowerStorageComponent, EntParentChangedMessage>(OnStorageTracked);
        SubscribeLocalEvent<RMCPowerStorageComponent, AnchorStateChangedEvent>(OnStorageTracked);
        SubscribeLocalEvent<RMCPowerStorageComponent, ComponentRemove>(OnStorageRemoved);
        SubscribeLocalEvent<RMCPowerStorageComponent, EntityTerminatingEvent>(OnStorageRemoved);
        SubscribeLocalEvent<RMCSmesComponent, EmpPulseEvent>(OnSmesEmp);
        SubscribeLocalEvent<RMCSmesComponent, InteractUsingEvent>(OnSmesInteractUsing);
        SubscribeLocalEvent<RMCPowerUsageDisplayComponent, ExaminedEvent>(OnUsageDisplayEvent);

        Subs.CVar(_config, RMCCVars.RMCPowerUpdateEverySeconds, OnUpdateEveryChanged, true);
        Subs.CVar(_config, RMCCVars.RMCPowerLoadMultiplier, OnPowerLoadMultiplierChanged, true);
    }

    private void OnUpdateEveryChanged(float value)
    {
        _updateEvery = TimeSpan.FromSeconds(SanitizeUpdateInterval(value));
    }

    private void OnPowerLoadMultiplierChanged(float value)
    {
        _powerLoadMultiplier = SanitizeLoadMultiplier(value);
    }

    internal static float SanitizeUpdateInterval(float value)
    {
        return float.IsFinite(value) && value > 0
            ? Math.Max(0.05f, value)
            : 1f;
    }

    internal static float SanitizeLoadMultiplier(float value)
    {
        return float.IsFinite(value) && value >= 0
            ? value
            : 1f;
    }

    internal static float ApcCellChargeToEnergy(float charge)
    {
        return Math.Max(0, charge) * ApcCellJoulesPerCharge;
    }

    internal static float ApcCellEnergyToCharge(float energy)
    {
        return Math.Max(0, energy) / ApcCellJoulesPerCharge;
    }

    internal static float GetApcCellChargeRate(float maxCharge)
    {
        return Math.Max(0, maxCharge) * Cm13ChargeLevel / Cm13CellRate;
    }

    protected override void ApcRegistryUpdated(Entity<RMCApcComponent> ent)
    {
        _trackedApcs.Add(ent);
    }

    protected override void ApcRegistryRemoved(Entity<RMCApcComponent> ent)
    {
        _trackedApcs.Remove(ent);
    }

    protected override void ApcStartingCellSpawned(Entity<RMCApcComponent> ent, EntityUid cell)
    {
        if (!_batteryQuery.TryComp(cell, out var battery))
            return;

        var charge = battery.MaxCharge * Math.Clamp(ent.Comp.StartingCellCharge, 0, 1);
        _battery.SetCharge(cell, charge, battery);
    }

    private void OnApcEmp(Entity<RMCApcComponent> ent, ref EmpPulseEvent args)
    {
        if (TryGetApcCell(ent) is { } cell)
        {
            var empCharge = ApcCellEnergyToCharge(args.EnergyConsumption);
            _battery.UseCharge(cell, Math.Max(empCharge, cell.Comp.MaxCharge * 0.5f), cell.Comp);
        }

        for (var i = 0; i < ent.Comp.Channels.Length; i++)
        {
            ent.Comp.Channels[i].Button = RMCApcButtonState.Off;
            ent.Comp.Channels[i].On = false;
        }

        ent.Comp.EmpRestoreAt = _timing.CurTime + TimeSpan.FromMinutes(1);
        Dirty(ent);
        args.Affected = true;
    }

    private void OnMonitorTracked<T>(Entity<RMCPowerMonitorComponent> ent, ref T args)
    {
        _trackedMonitors.Add(ent);
    }

    private void OnMonitorRemoved<T>(Entity<RMCPowerMonitorComponent> ent, ref T args)
    {
        _trackedMonitors.Remove(ent);
    }

    private void OnSourceTracked<T>(Entity<RMCPowerSourceComponent> ent, ref T args)
    {
        _trackedSources.Add(ent);
    }

    private void OnSourceRemoved<T>(Entity<RMCPowerSourceComponent> ent, ref T args)
    {
        _trackedSources.Remove(ent);
    }

    private void OnStorageTracked<T>(Entity<RMCPowerStorageComponent> ent, ref T args)
    {
        _trackedStorages.Add(ent);
    }

    private void OnStorageRemoved<T>(Entity<RMCPowerStorageComponent> ent, ref T args)
    {
        _trackedStorages.Remove(ent);
    }

    private void OnUsageDisplayEvent(Entity<RMCPowerUsageDisplayComponent> ent, ref ExaminedEvent args)
    {
        if (!_cell.TryGetBatteryFromSlot(ent, out var battery) || !TryComp<PowerCellDrawComponent>(ent, out var draw))
            return;

        var maxUses = (int) (battery.MaxCharge / draw.UseRate);
        var uses = (int) (battery.CurrentCharge / draw.UseRate);
        args.PushMarkup(Loc.GetString(ent.Comp.PowerText, ("uses", uses), ("maxuses", maxUses)));
    }

    protected override void OnReceiverMapInit(Entity<RMCPowerReceiverComponent> ent, ref MapInitEvent args)
    {
        base.OnReceiverMapInit(ent, ref args);

        if (!TryComp(ent, out ApcPowerReceiverComponent? receiver) || receiver.NeedsPower)
            return;

        receiver.Powered = true;
        Dirty(ent, receiver);

        var ev = new PowerChangedEvent(true, 0);
        RaiseLocalEvent(ent, ref ev);

        if (_appearanceQuery.TryComp(ent, out var appearance))
            _appearance.SetData(ent, PowerDeviceVisuals.Powered, true, appearance);
    }

    protected override void PowerUpdated(Entity<RMCAreaPowerComponent> area, RMCPowerChannel channel, bool on)
    {
        base.PowerUpdated(area, channel, on);

        var ev = new PowerChangedEvent(on, 0);
        foreach (var receiver in GetAreaReceivers(area, channel))
        {
            UpdateReceiverPower(receiver, ref ev);
        }
    }

    public override bool IsPowered(EntityUid ent)
    {
        return TryComp(ent, out ApcPowerReceiverComponent? receiver) && receiver.Powered;
    }

    public override RMCPowerNetworkStats GetNetworkStats(RMCPowerNetworkKey key)
    {
        return _networkStats.GetValueOrDefault(key);
    }

    public override void RecalculatePower()
    {
        _trackedApcs.Clear();
        _trackedMonitors.Clear();
        _trackedSources.Clear();
        _trackedStorages.Clear();

        var apcs = EntityQueryEnumerator<RMCApcComponent>();
        while (apcs.MoveNext(out var uid, out _))
        {
            _trackedApcs.Add(uid);
            if (!TryGetPowerNetwork(uid, out _))
                Log.Warning($"APC {ToPrettyString(uid)} is not inside a valid area and will not control power.");
        }

        var monitors = EntityQueryEnumerator<RMCPowerMonitorComponent>();
        while (monitors.MoveNext(out var uid, out _))
        {
            _trackedMonitors.Add(uid);
            if (!TryGetPowerNetwork(uid, out _))
                Log.Warning($"Power monitor {ToPrettyString(uid)} is not inside a valid area and will be disconnected.");
        }

        var receivers = EntityQueryEnumerator<RMCPowerReceiverComponent>();
        while (receivers.MoveNext(out var uid, out _))
        {
            if (!TryGetPowerNetwork(uid, out _))
                Log.Warning($"Power receiver {ToPrettyString(uid)} is not inside a valid area and will not receive power.");
        }

        var sources = EntityQueryEnumerator<RMCPowerSourceComponent>();
        while (sources.MoveNext(out var uid, out _))
        {
            _trackedSources.Add(uid);
            if (!TryGetPowerNetwork(uid, out _))
                Log.Warning($"Power source {ToPrettyString(uid)} is not inside a valid area and will not generate power.");
        }

        var storages = EntityQueryEnumerator<RMCPowerStorageComponent>();
        while (storages.MoveNext(out var uid, out _))
        {
            _trackedStorages.Add(uid);
            if (!TryGetPowerNetwork(uid, out _))
                Log.Warning($"Power storage {ToPrettyString(uid)} is not inside a valid area and will not join a network.");
        }

        base.RecalculatePower();
    }

    public bool BlackoutNetwork(RMCPowerNetworkKey key)
    {
        var changed = false;
        foreach (var uid in _trackedStorages)
        {
            if (!_storageQuery.TryComp(uid, out var storage) ||
                !_batteryQuery.TryComp(uid, out var battery) ||
                !TryGetPowerNetwork(uid, out var storageKey) ||
                storageKey != key)
            {
                continue;
            }

            storage.InputEnabled = false;
            storage.OutputEnabled = false;
            storage.CurrentInput = 0;
            storage.CurrentOutput = 0;
            _battery.SetCharge(uid, 0, battery);
            Dirty(uid, storage);
            changed = true;
        }

        foreach (var uid in _trackedApcs)
        {
            if (!_apcQuery.TryComp(uid, out var apc) ||
                !TryGetPowerNetwork(uid, out var apcKey) ||
                apcKey != key)
            {
                continue;
            }

            apc.MainBreakerButton = false;
            apc.ChargeModeButton = false;
            SetAllApcChannels((uid, apc), false);
            if (TryGetApcCell((uid, apc)) is { } cell)
                _battery.SetCharge(cell, 0, cell.Comp);
            Dirty(uid, apc);
            changed = true;
        }

        if (changed)
        {
            _adminLog.Add(LogType.Action, LogImpact.High,
                $"RMC power network '{key.PowerNet}' on map entity {key.Map} was blacked out.");
            _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-power-blackout-announcement",
                ("network", key.PowerNet)), BlackoutAnnouncementSound);
        }

        return changed;
    }

    public bool RestoreNetwork(RMCPowerNetworkKey key, bool advanced = false)
    {
        var changed = false;
        foreach (var uid in _trackedStorages)
        {
            if (!_storageQuery.TryComp(uid, out var storage) ||
                !_batteryQuery.TryComp(uid, out var battery) ||
                !TryGetPowerNetwork(uid, out var storageKey) ||
                storageKey != key)
            {
                continue;
            }

            storage.InputEnabled = false;
            storage.OutputEnabled = true;
            storage.InputLimit = Math.Min(storage.MaxInput, 200_000);
            storage.OutputLimit = Math.Min(storage.MaxOutput, 50_000);
            storage.CurrentInput = 0;
            storage.CurrentOutput = 0;
            _battery.SetCharge(uid, battery.MaxCharge, battery);
            Dirty(uid, storage);
            changed = true;
        }

        foreach (var uid in _trackedApcs)
        {
            if (!_apcQuery.TryComp(uid, out var apc) ||
                !TryGetPowerNetwork(uid, out var apcKey) ||
                apcKey != key)
            {
                continue;
            }

            apc.MainBreakerButton = true;
            apc.ChargeModeButton = true;
            for (var i = 0; i < apc.Channels.Length; i++)
                apc.Channels[i].Button = RMCApcButtonState.Auto;
            if (TryGetApcCell((uid, apc)) is { } cell)
                _battery.SetCharge(cell, cell.Comp.MaxCharge, cell.Comp);
            Dirty(uid, apc);
            changed = true;
        }

        if (advanced)
            changed |= RestoreReactors(key);

        if (changed)
        {
            _adminLog.Add(LogType.Action, LogImpact.High,
                $"RMC power network '{key.PowerNet}' on map entity {key.Map} was restored{(advanced ? " with reactor repair." : ".")}");
            _marineAnnounce.AnnounceToMarines(Loc.GetString(advanced
                    ? "rmc-power-advanced-restore-announcement"
                    : "rmc-power-restore-announcement",
                ("network", key.PowerNet)), RestoreAnnouncementSound);
        }

        return changed;
    }

    private bool RestoreReactors(RMCPowerNetworkKey key)
    {
        var changed = false;
        foreach (var uid in _trackedSources)
        {
            if (!_fusionReactorQuery.TryComp(uid, out var reactor) ||
                !TryGetPowerNetwork(uid, out var reactorKey) ||
                reactorKey != key)
            {
                continue;
            }

            reactor.State = RMCFusionReactorState.Working;
            reactor.TerminalFailure = false;
            reactor.Overloaded = false;
            reactor.FailureChance = reactor.BaseFailureChance;
            var cell = TryGetFusionCell((uid, reactor));
            if (cell == null &&
                TrySpawnInContainer("RMCGeneratorFusionCell", uid, reactor.CellContainerSlot, out var spawned) &&
                _fusionCellQuery.TryComp(spawned, out var spawnedCell))
            {
                cell = (spawned.Value, spawnedCell);
            }

            if (cell != null)
            {
                cell.Value.Comp.Fuel = cell.Value.Comp.MaxFuel;
                cell.Value.Comp.IsFresh = true;
                Dirty(cell.Value);
                UpdateFusionCellAppearance(cell.Value);
            }

            SetFusionReactorEnabled((uid, reactor), true, 98);
            changed = true;
        }

        return changed;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (!_updateInitialized)
        {
            _updateInitialized = true;
            _lastUpdate = now;
            _nextUpdate = now + _updateEvery;
            return;
        }

        if (_nextUpdate > now)
            return;

        var deltaTime = (float) (now - _lastUpdate).TotalSeconds;
        deltaTime = Math.Max(deltaTime, 0.001f);
        _lastUpdate = now;
        _nextUpdate = now + _updateEvery;

        UpdateOverloadedReactorFeedback(now);
        CleanRegistries();
        UpdateReactors(now);
        RestoreSmes(now);
        RestoreApcs(now);

        var networks = BuildNetworks(deltaTime);
        foreach (var network in networks.Values.OrderBy(network => network.Key.Map).ThenBy(network => network.Key.PowerNet))
        {
            DistributeNetwork(network, deltaTime);
            UpdateNetworkEntity(network);
        }

        foreach (var oldKey in _networkEntities.Keys.Where(key => !networks.ContainsKey(key)).ToArray())
        {
            QueueDel(_networkEntities[oldKey]);
            _networkEntities.Remove(oldKey);
            _networkStats.Remove(oldKey);
        }

        UpdateSmesState();
        UpdateMonitors(networks);
    }

    private void OnSmesEmp(Entity<RMCSmesComponent> ent, ref EmpPulseEvent args)
    {
        if (!_storageQuery.TryComp(ent, out var storage) || !_batteryQuery.TryComp(ent, out var battery))
            return;

        if (!ent.Comp.EmpDisabled)
        {
            ent.Comp.RestoreInput = storage.InputEnabled;
            ent.Comp.RestoreOutput = storage.OutputEnabled;
        }

        ent.Comp.EmpDisabled = true;
        ent.Comp.EmpRestoreAt = _timing.CurTime + ent.Comp.EmpDisableDuration;
        storage.InputEnabled = false;
        storage.OutputEnabled = false;
        storage.CurrentInput = 0;
        storage.CurrentOutput = 0;
        _battery.UseCharge(ent, Math.Max(args.EnergyConsumption, battery.MaxCharge * 0.1f), battery);
        Dirty(ent);
        Dirty(ent.Owner, storage);
        args.Affected = true;
        args.Disabled = true;
    }

    private void OnSmesInteractUsing(Entity<RMCSmesComponent> ent, ref InteractUsingEvent args)
    {
        if (!_batteryQuery.TryComp(ent, out var battery) ||
            battery.CurrentCharge <= 0 ||
            !TryComp(ent, out WiresPanelComponent? panel) ||
            !panel.Open ||
            !_random.Prob(0.25f))
        {
            return;
        }

        _electrocution.TryDoElectrocution(
            args.User,
            ent,
            20,
            TimeSpan.FromSeconds(2),
            refresh: true);
    }

    private void RestoreSmes(TimeSpan now)
    {
        foreach (var uid in _trackedStorages)
        {
            if (!_smesQuery.TryComp(uid, out var smes) ||
                !_storageQuery.TryComp(uid, out var storage) ||
                !smes.EmpDisabled ||
                now < smes.EmpRestoreAt)
            {
                continue;
            }

            smes.EmpDisabled = false;
            storage.InputEnabled = smes.RestoreInput;
            storage.OutputEnabled = smes.RestoreOutput;
            Dirty(uid, smes);
            Dirty(uid, storage);
        }
    }

    private void RestoreApcs(TimeSpan now)
    {
        foreach (var uid in _trackedApcs)
        {
            if (!_apcQuery.TryComp(uid, out var apc) ||
                apc.EmpRestoreAt == TimeSpan.Zero ||
                now < apc.EmpRestoreAt)
            {
                continue;
            }

            apc.EmpRestoreAt = TimeSpan.Zero;
            apc.Channels[(int) RMCPowerChannel.Equipment].Button = RMCApcButtonState.Auto;
            apc.Channels[(int) RMCPowerChannel.Environment].Button = RMCApcButtonState.Auto;
            Dirty(uid, apc);
        }
    }

    private void UpdateSmesState()
    {
        foreach (var uid in _trackedStorages)
        {
            if (!_smesQuery.TryComp(uid, out var smes) ||
                !_storageQuery.TryComp(uid, out var storage) ||
                !_batteryQuery.TryComp(uid, out var battery))
            {
                continue;
            }

            smes.Charge = battery.CurrentCharge;
            smes.MaxCharge = battery.MaxCharge;
            smes.ChargePercentage = battery.MaxCharge <= 0 ? 0 : battery.CurrentCharge / battery.MaxCharge;
            var level = smes.ChargePercentage <= 0
                ? 0
                : Math.Clamp((int) MathF.Ceiling(smes.ChargePercentage * 5), 1, 5);
            var chargeState = storage.CurrentOutput > 0
                ? ChargeState.Discharging
                : storage.CurrentInput > 0
                    ? ChargeState.Charging
                    : ChargeState.Still;
            _appearance.SetData(uid, SmesVisuals.LastChargeLevel, level);
            _appearance.SetData(uid, SmesVisuals.LastChargeState, chargeState);
            Dirty(uid, smes);
        }
    }

    private void CleanRegistries()
    {
        _trackedApcs.RemoveWhere(uid => TerminatingOrDeleted(uid) || !_apcQuery.HasComp(uid));
        _trackedMonitors.RemoveWhere(uid => TerminatingOrDeleted(uid) || !_monitorQuery.HasComp(uid));
        _trackedSources.RemoveWhere(uid => TerminatingOrDeleted(uid) || !_sourceQuery.HasComp(uid));
        _trackedStorages.RemoveWhere(uid => TerminatingOrDeleted(uid) || !_storageQuery.HasComp(uid));
    }

    private void UpdateMonitors(Dictionary<RMCPowerNetworkKey, Network> networks)
    {
        foreach (var uid in _trackedMonitors.Order())
        {
            if (!_monitorQuery.TryComp(uid, out var monitor) ||
                !_ui.IsUiOpen(uid, RMCPowerMonitorUiKey.Key))
            {
                continue;
            }

            if (!TryGetPowerNetwork(uid, out var key) || !networks.TryGetValue(key, out var network))
            {
                monitor.Connected = false;
                monitor.PowerNet = string.Empty;
                monitor.Stats = default;
                monitor.Storages = [];
                monitor.Apcs = [];
                Dirty(uid, monitor);
                continue;
            }

            monitor.Connected = true;
            monitor.PowerNet = key.PowerNet;
            monitor.Stats = network.Stats;
            monitor.Storages = network.Storages
                .OrderBy(storage => storage.Uid)
                .Select(storage => new RMCPowerMonitorStorage(
                    Name(storage.Uid),
                    storage.Battery.CurrentCharge,
                    storage.Battery.MaxCharge,
                    storage.Component.InputEnabled,
                    storage.Component.InputState,
                    storage.Component.InputLimit,
                    storage.Component.CurrentInput,
                    storage.Component.OutputEnabled,
                    storage.Component.OutputLimit,
                    storage.Component.CurrentOutput))
                .ToArray();
            monitor.Apcs = network.Apcs
                .OrderBy(apc => apc.Uid)
                .Select(apc => new RMCPowerMonitorApc(
                    Name(apc.Area),
                    GetApcChannelVisualState(apc.Component.Channels[(int) RMCPowerChannel.Equipment]),
                    GetApcChannelVisualState(apc.Component.Channels[(int) RMCPowerChannel.Lighting]),
                    GetApcChannelVisualState(apc.Component.Channels[(int) RMCPowerChannel.Environment]),
                    apc.Component.RequestedPower,
                    apc.Component.DeliveredPower,
                    apc.Cell != null,
                    apc.Component.ChargeStatus,
                    apc.Component.ChargePercentage))
                .ToArray();
            Dirty(uid, monitor);
        }
    }

    private void UpdateReactors(TimeSpan now)
    {
        foreach (var uid in _trackedSources.Order())
        {
            if (!_sourceQuery.TryComp(uid, out var source) || !_fusionReactorQuery.TryComp(uid, out var reactor))
                continue;

            var previousOutput = reactor.CurrentOutput;
            var cell = TryGetFusionCell((uid, reactor));
            if (cell is { Comp.IsFresh: true })
            {
                reactor.FailureChance = reactor.BaseFailureChance;
                cell.Value.Comp.IsFresh = false;
                Dirty(cell.Value);
            }

            if (!reactor.Enabled || reactor.TerminalFailure)
            {
                reactor.CurrentOutput = 0;
                reactor.OutputPercent = 0;
                reactor.NextRampAt = TimeSpan.Zero;
                reactor.NextFuelUseAt = TimeSpan.Zero;
                reactor.NextFailureCheckAt = TimeSpan.Zero;
                source.Enabled = false;
                source.AvailablePower = 0;
                Dirty(uid, reactor);
                Dirty(uid, source);
                UpdateAppearance((uid, reactor));
                if (previousOutput > 0)
                    ReactorUpdated((uid, reactor));
                continue;
            }

            source.Enabled = true;
            if (reactor.NextRampAt == TimeSpan.Zero)
                reactor.NextRampAt = now + reactor.RampInterval;

            while (reactor.OutputPercent < 100 && now >= reactor.NextRampAt)
            {
                reactor.OutputPercent = Math.Min(100, reactor.OutputPercent + 1);
                reactor.NextRampAt += reactor.RampInterval;
            }

            if (reactor.NextFuelUseAt == TimeSpan.Zero)
                reactor.NextFuelUseAt = now + reactor.FuelUseInterval;

            while (now >= reactor.NextFuelUseAt)
            {
                reactor.NextFuelUseAt += reactor.FuelUseInterval;
                if (cell == null || cell.Value.Comp.Fuel <= 0)
                {
                    reactor.FailureChance += 2.5f;
                    continue;
                }

                if (reactor.State == RMCFusionReactorState.Working)
                    continue;

                cell.Value.Comp.Fuel = Math.Max(0, cell.Value.Comp.Fuel - _random.Next(5, 21));
                Dirty(cell.Value);
                UpdateFusionCellAppearance(cell.Value);
            }

            var emergency = cell == null || cell.Value.Comp.Fuel <= 0;
            var failureInterval = emergency
                ? reactor.EmergencyFailureCheckInterval
                : reactor.FailureCheckInterval;
            if (reactor.NextFailureCheckAt == TimeSpan.Zero)
                reactor.NextFailureCheckAt = now + failureInterval;

            if (now >= reactor.NextFailureCheckAt)
            {
                reactor.NextFailureCheckAt = now + failureInterval;
                if (_random.Prob(Math.Clamp(reactor.FailureChance / 100, 0, 1)))
                    AdvanceReactorFailure((uid, reactor));
            }

            reactor.CurrentOutput = reactor.Enabled && !reactor.TerminalFailure
                ? reactor.Watts * reactor.OutputPercent / 100
                : 0;
            source.Enabled = reactor.Enabled && !reactor.TerminalFailure;
            source.AvailablePower = reactor.CurrentOutput;
            Dirty(uid, reactor);
            Dirty(uid, source);
            UpdateAppearance((uid, reactor));
            if (Math.Abs(previousOutput - reactor.CurrentOutput) > 0.001f)
                ReactorUpdated((uid, reactor));
        }
    }

    private Entity<RMCFusionCellComponent>? TryGetFusionCell(Entity<RMCFusionReactorComponent> reactor)
    {
        if (!_container.TryGetContainer(reactor, reactor.Comp.CellContainerSlot, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var cellUid) ||
            !_fusionCellQuery.TryComp(cellUid.Value, out var cell))
        {
            return null;
        }

        return (cellUid.Value, cell);
    }

    internal void AdvanceReactorFailure(Entity<RMCFusionReactorComponent> reactor)
    {
        SetFusionReactorOverloaded(reactor, false);
        if (reactor.Comp.State == RMCFusionReactorState.Weld)
        {
            reactor.Comp.TerminalFailure = true;
            reactor.Comp.Enabled = false;
            reactor.Comp.OutputPercent = 0;
            reactor.Comp.CurrentOutput = 0;
            reactor.Comp.NextRampAt = TimeSpan.Zero;
            reactor.Comp.NextFuelUseAt = TimeSpan.Zero;
            reactor.Comp.NextFailureCheckAt = TimeSpan.Zero;
            Dirty(reactor);
            UpdateAppearance(reactor);
            ReactorUpdated(reactor);
            return;
        }

        reactor.Comp.State++;
        Dirty(reactor);
        UpdateAppearance(reactor);
        ReactorUpdated(reactor);
    }

    private void UpdateFusionCellAppearance(Entity<RMCFusionCellComponent> cell)
    {
        var level = cell.Comp.FuelPercentage switch
        {
            >= 1 => RMCFusionCellFuelLevel.Full,
            >= 0.75f => RMCFusionCellFuelLevel.High,
            >= 0.25f => RMCFusionCellFuelLevel.Medium,
            > 0 => RMCFusionCellFuelLevel.Low,
            _ => RMCFusionCellFuelLevel.Empty,
        };
        _appearance.SetData(cell, RMCFusionCellVisuals.Fuel, level);
    }

    private Dictionary<RMCPowerNetworkKey, Network> BuildNetworks(float deltaTime)
    {
        var networks = new Dictionary<RMCPowerNetworkKey, Network>();

        foreach (var uid in _trackedSources.Order())
        {
            if (!_sourceQuery.TryComp(uid, out var source))
                continue;

            source.CurrentPower = 0;
            source.Area = null;
            Dirty(uid, source);

            if (!TryGetPowerNetwork(uid, out var key))
                continue;

            var network = GetOrCreateNetwork(networks, key);
            var entry = new Source(uid, source, source.Enabled ? Math.Max(0, source.AvailablePower) : 0);
            if (source.Scope == RMCPowerSourceScope.Area && TryGetPowerArea(uid, out var area))
            {
                source.Area = area;
                network.LocalSources.GetOrNew(area).Add(entry);
            }
            else
            {
                source.Area = null;
                network.Sources.Add(entry);
            }

        }

        foreach (var uid in _trackedStorages.Order())
        {
            if (!_storageQuery.TryComp(uid, out var storage) ||
                !_batteryQuery.TryComp(uid, out var battery))
            {
                continue;
            }

            storage.CurrentInput = 0;
            storage.CurrentOutput = 0;
            storage.InputState = RMCPowerStorageInputState.Off;
            storage.Area = null;
            Dirty(uid, storage);

            if (!TryGetPowerNetwork(uid, out var key))
                continue;

            if (TryGetPowerArea(uid, out var area))
                storage.Area = area;

            var network = GetOrCreateNetwork(networks, key);
            network.Storages.Add(new Storage(uid, storage, battery));
            Dirty(uid, storage);
        }

        foreach (var uid in _trackedApcs.Order())
        {
            if (!_apcQuery.TryComp(uid, out var apc) ||
                !_areaPowerQuery.TryComp(apc.Area, out var area) ||
                !TryGetPowerNetwork(uid, out var key))
            {
                continue;
            }

            var network = GetOrCreateNetwork(networks, key);
            if (network.ApcsByArea.TryGetValue(apc.Area.Value, out var duplicate))
            {
                DisableDuplicateApc((uid, apc), (apc.Area.Value, area));
                continue;
            }

            var cell = TryGetApcCell((uid, apc));
            var entry = new Apc(uid, apc, (apc.Area.Value, area), cell);
            network.Apcs.Add(entry);
            network.ApcsByArea.Add(apc.Area.Value, entry);
        }

        foreach (var network in networks.Values)
        {
            PrepareApcs(network, deltaTime);
        }

        return networks;
    }

    private static Network GetOrCreateNetwork(
        Dictionary<RMCPowerNetworkKey, Network> networks,
        RMCPowerNetworkKey key)
    {
        if (networks.TryGetValue(key, out var network))
            return network;

        network = new Network(key);
        networks.Add(key, network);
        return network;
    }

    private Entity<BatteryComponent>? TryGetApcCell(Entity<RMCApcComponent> apc)
    {
        if (!_container.TryGetContainer(apc, apc.Comp.CellContainerSlot, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var cellUid) ||
            !_batteryQuery.TryComp(cellUid, out var battery))
        {
            return null;
        }

        return (cellUid.Value, battery);
    }

    private void PrepareApcs(Network network, float deltaTime)
    {
        var hasNetworkPower = network.Sources.Any(source => source.Available > 0) ||
                              network.LocalSources.Values.SelectMany(source => source).Any(source => source.Available > 0) ||
                              network.Storages.Any(StorageCanOutput);

        foreach (var apc in network.Apcs)
        {
            var operational = CanApcOperate(apc.Uid, apc.Component) && apc.Cell != null;
            if (!operational)
            {
                apc.Component.PowerStatus = apc.Cell == null ? RMCApcPowerStatus.None : RMCApcPowerStatus.Faulted;
                apc.Component.ExternalPower = false;
                apc.Component.ChargeStatus = RMCApcChargeStatus.NotCharging;
                apc.Component.ChargePercentage = apc.Cell is { Comp.MaxCharge: > 0 } cell
                    ? cell.Comp.CurrentCharge / cell.Comp.MaxCharge
                    : 0;
                apc.Component.RequestedPower = 0;
                apc.Component.DeliveredPower = 0;
                apc.Component.LocalGeneration = 0;
                apc.Component.ExportedPower = 0;
                apc.Component.ChargeRequest = 0;
                apc.Component.ChargePower = 0;
                apc.Component.SurplusCycles = 0;
                UpdateApcChannel((apc.Uid, apc.Component), apc.Area, RMCPowerChannel.Equipment, false);
                UpdateApcChannel((apc.Uid, apc.Component), apc.Area, RMCPowerChannel.Lighting, false);
                UpdateApcChannel((apc.Uid, apc.Component), apc.Area, RMCPowerChannel.Environment, false);
                FinishApcVisuals(apc);
                continue;
            }

            var battery = apc.Cell!.Value;
            var charge = battery.Comp.MaxCharge <= 0 ? 0 : battery.Comp.CurrentCharge / battery.Comp.MaxCharge;
            var allAutomaticChannelsOn = apc.Component.Channels.All(channel =>
                channel.Button != RMCApcButtonState.Auto || channel.On);
            var stableExternalPower = allAutomaticChannelsOn &&
                                      apc.Component.PowerStatus == RMCApcPowerStatus.External &&
                                      apc.Component.DeliveredPower + 0.01f >= apc.Component.RequestedPower;
            var available = hasNetworkPower || battery.Comp.CurrentCharge > 0;
            var equipmentAuto = ShouldEnableApcAutoChannel(
                apc.Component.Channels[(int) RMCPowerChannel.Equipment].On,
                charge,
                EquipmentCutoff,
                EquipmentRestore,
                stableExternalPower);
            var lightingAuto = ShouldEnableApcAutoChannel(
                apc.Component.Channels[(int) RMCPowerChannel.Lighting].On,
                charge,
                LightingCutoff,
                LightingRestore,
                stableExternalPower);

            UpdateApcChannel((apc.Uid, apc.Component), apc.Area, RMCPowerChannel.Equipment,
                GetChannelTarget(apc.Component, RMCPowerChannel.Equipment, equipmentAuto, available), false);
            UpdateApcChannel((apc.Uid, apc.Component), apc.Area, RMCPowerChannel.Lighting,
                GetChannelTarget(apc.Component, RMCPowerChannel.Lighting, lightingAuto, available), false);
            UpdateApcChannel((apc.Uid, apc.Component), apc.Area, RMCPowerChannel.Environment,
                GetChannelTarget(apc.Component, RMCPowerChannel.Environment, available, available), false);

            var continuousLoad = 0f;
            for (var i = 0; i < apc.Area.Comp.Load.Length; i++)
            {
                var channelLoad = apc.Component.Channels[i].On
                    ? Math.Max(0, apc.Area.Comp.Load[i] * _powerLoadMultiplier)
                    : 0;
                apc.Component.Channels[i].Watts = (int) MathF.Round(channelLoad);

                if (!apc.Component.Channels[i].On)
                    continue;

                foreach (var receiverUid in GetAreaReceivers(apc.Area, (RMCPowerChannel) i))
                {
                    if (!_receiverQuery.TryComp(receiverUid, out var receiver) || receiver.PendingOneOffEnergy <= 0)
                        continue;

                    channelLoad += receiver.PendingOneOffEnergy / deltaTime;
                    receiver.PendingOneOffEnergy = 0;
                }

                apc.ChannelLoads[i] = channelLoad;
                continuousLoad += channelLoad;
            }

            apc.Load = continuousLoad;
            var roomPower = ApcCellChargeToEnergy(battery.Comp.MaxCharge - battery.Comp.CurrentCharge) / deltaTime;
            var chargeRate = GetApcCellChargeRate(battery.Comp.MaxCharge);
            apc.ChargeRequest = apc.Component.ChargeModeButton && apc.Component.SurplusCycles >= 10 && charge < ApcFullCharge
                ? Math.Min(chargeRate, roomPower)
                : 0;
            apc.Component.RequestedPower = apc.Load;
            apc.Component.ChargeRequest = apc.ChargeRequest;
            apc.Component.ChargePower = 0;
            apc.Component.DeliveredPower = 0;
            apc.Component.LocalGeneration = 0;
            apc.Component.ExportedPower = 0;
        }
    }

    internal static bool ShouldEnableApcAutoChannel(
        bool currentlyOn,
        float charge,
        float cutoff,
        float restore,
        bool stableExternalPower)
    {
        return stableExternalPower || (currentlyOn ? charge > cutoff : charge >= restore);
    }

    private void DistributeNetwork(Network network, float deltaTime)
    {
        var localSourceLists = network.LocalSources.Values.ToList();
        var localSourcesAll = localSourceLists.SelectMany(sources => sources).ToList();
        var allSources = network.Sources.Concat(localSourcesAll).ToList();
        var availableGeneration = allSources.Sum(source => source.Available);
        RefreshSourceExportAvailable(allSources);

        foreach (var apc in network.Apcs)
        {
            if (!network.LocalSources.TryGetValue(apc.Area, out var localSources))
                continue;

            var localAvailable = localSources.Sum(source => source.Available);
            var used = Math.Min(apc.Load, localAvailable);
            apc.LoadAllocated += used;
            apc.Component.LocalGeneration = used;
            DistributeSources(localSources, used, false);
            apc.Component.ExportedPower = Math.Max(0, localAvailable - used);
        }

        RefreshSourceExportAvailable(allSources);
        var networkLiveAvailable = allSources.Sum(source => source.ExportAvailable);
        var liveRequests = network.Apcs
            .Select(apc => (Uid: apc.Uid, Request: Math.Max(0, apc.Load - apc.LoadAllocated)))
            .ToList();
        var liveAllocations = AllocateMaxMin(liveRequests, networkLiveAvailable);
        var networkLiveUsed = 0f;
        foreach (var apc in network.Apcs)
        {
            var allocated = liveAllocations.GetValueOrDefault(apc.Uid);
            apc.LoadAllocated += allocated;
            networkLiveUsed += allocated;
        }

        DistributeSources(allSources, networkLiveUsed, true);
        RefreshSourceExportAvailable(allSources);

        var remainingDemand = network.Apcs.Sum(apc => Math.Max(0, apc.Load - apc.LoadAllocated));
        var storageAvailable = network.Storages.Sum(storage => GetStorageOutput(storage, deltaTime));
        var storageDischarge = Math.Min(remainingDemand, storageAvailable);
        if (remainingDemand > 0 && storageDischarge > 0)
        {
            foreach (var apc in network.Apcs)
            {
                var deficit = Math.Max(0, apc.Load - apc.LoadAllocated);
                apc.LoadAllocated += storageDischarge * deficit / remainingDemand;
            }

            DistributeStorageOutput(network.Storages, storageDischarge, deltaTime);
        }

        var liveSurplusAfterLoads = allSources.Sum(source => source.ExportAvailable);
        var chargeRequests = network.Apcs
            .Select(apc => (Uid: apc.Uid, Request: apc.ChargeRequest))
            .ToList();
        var chargeAllocations = AllocateMaxMin(chargeRequests, liveSurplusAfterLoads);
        var liveChargeUsed = 0f;
        foreach (var apc in network.Apcs)
        {
            apc.ChargeAllocated = chargeAllocations.GetValueOrDefault(apc.Uid);
            liveChargeUsed += apc.ChargeAllocated;
        }

        DistributeSources(allSources, liveChargeUsed, true);
        RefreshSourceExportAvailable(allSources);

        foreach (var apc in network.Apcs)
        {
            FinishApcPower(apc, deltaTime);
        }

        foreach (var apc in network.Apcs)
        {
            var loadSatisfied = apc.Component.DeliveredPower + 0.01f >= apc.Load;
            if (loadSatisfied && liveSurplusAfterLoads > 0.01f)
                apc.Component.SurplusCycles++;
            else
                apc.Component.SurplusCycles = 0;
        }

        var liveSurplus = allSources.Sum(source => source.ExportAvailable);
        var storageCharge = DistributeStorageInput(network.Storages, liveSurplus, deltaTime);
        DistributeSources(allSources, storageCharge, true);
        RefreshSourceExportAvailable(allSources);

        var demand = network.Apcs.Sum(apc => apc.Load + apc.ChargeRequest);
        var delivered = network.Apcs.Sum(apc => apc.Component.DeliveredPower + apc.Component.ChargePower);
        var deficitPower = Math.Max(0, demand - delivered);
        var generation = allSources.Sum(source => source.Used);
        var surplus = allSources.Sum(source => source.ExportAvailable);
        network.Stats = new RMCPowerNetworkStats(
            availableGeneration,
            generation,
            demand,
            delivered,
            deficitPower,
            surplus,
            storageCharge,
            storageDischarge);

        foreach (var source in network.LocalSources.Values.SelectMany(sources => sources).Concat(network.Sources))
        {
            source.Component.CurrentPower = source.Used;
            Dirty(source.Uid, source.Component);
        }
    }

    private static void RefreshSourceExportAvailable(List<Source> sources)
    {
        foreach (var source in sources)
        {
            source.ExportAvailable = Math.Max(0, source.Available - source.Used);
        }
    }

    private float FinishApcPower(Apc apc, float deltaTime)
    {
        if (apc.Cell == null || !CanApcOperate(apc.Uid, apc.Component))
        {
            FinishApcVisuals(apc);
            return 0;
        }

        var cell = apc.Cell.Value;
        var externalForLoad = Math.Min(apc.Load, apc.LoadAllocated);
        var missingLoad = Math.Max(0, apc.Load - externalForLoad);
        var batteryPower = Math.Min(missingLoad, ApcCellChargeToEnergy(cell.Comp.CurrentCharge) / deltaTime);
        if (batteryPower > 0)
        {
            var usedCharge = ApcCellEnergyToCharge(batteryPower * deltaTime);
            _battery.SetCharge(cell, cell.Comp.CurrentCharge - usedCharge, cell.Comp);
        }

        var chargePower = Math.Min(apc.ChargeRequest, apc.ChargeAllocated);
        if (chargePower > 0)
        {
            var addedCharge = ApcCellEnergyToCharge(chargePower * deltaTime);
            _battery.SetCharge(cell,
                Math.Min(cell.Comp.MaxCharge, cell.Comp.CurrentCharge + addedCharge),
                cell.Comp);
        }

        apc.Component.DeliveredPower = externalForLoad + batteryPower;
        apc.Component.ChargePower = chargePower;
        apc.Component.ExternalPower = externalForLoad > 0 || chargePower > 0;
        apc.Component.ChargePercentage = cell.Comp.MaxCharge <= 0 ? 0 : cell.Comp.CurrentCharge / cell.Comp.MaxCharge;
        apc.Component.ChargeStatus = apc.Component.ChargePercentage >= ApcFullCharge
            ? RMCApcChargeStatus.FullCharge
            : chargePower > 0
                ? RMCApcChargeStatus.Charging
                : RMCApcChargeStatus.NotCharging;

        if (apc.Component.DeliveredPower + 0.01f < apc.Load)
            apc.Component.PowerStatus = RMCApcPowerStatus.Low;
        else if (batteryPower > 0)
            apc.Component.PowerStatus = RMCApcPowerStatus.Local;
        else if (apc.Component.ExternalPower)
            apc.Component.PowerStatus = RMCApcPowerStatus.External;
        else
            apc.Component.PowerStatus = RMCApcPowerStatus.None;

        UpdateDeliveredChannelPower(apc);
        FinishApcVisuals(apc);
        return batteryPower;
    }

    private void UpdateDeliveredChannelPower(Apc apc)
    {
        var remaining = apc.Component.DeliveredPower;
        ReadOnlySpan<RMCPowerChannel> priority =
        [
            RMCPowerChannel.Environment,
            RMCPowerChannel.Lighting,
            RMCPowerChannel.Equipment,
        ];

        foreach (var channel in priority)
        {
            var index = (int) channel;
            var required = apc.ChannelLoads[index];
            var powered = apc.Component.Channels[index].On && required <= remaining + 0.01f;
            if (powered)
                remaining = Math.Max(0, remaining - required);

            PowerUpdated(apc.Area, channel, powered);
        }
    }

    private void FinishApcVisuals(Apc apc)
    {
        _appearance.SetData(apc.Uid, RMCApcVisualsLayers.Power, apc.Component.ChargeStatus);
        _light.SetEnabled(apc.Uid, CanApcOperate(apc.Uid, apc.Component));
        _light.SetColor(apc.Uid,
            apc.Component.ChargeStatus switch
            {
                RMCApcChargeStatus.FullCharge => Color.FromHex("#64C864"),
                RMCApcChargeStatus.Charging => Color.FromHex("#6496FA"),
                RMCApcChargeStatus.NotCharging => Color.FromHex("#ff3b3b"),
                _ => Color.White,
            });
        Dirty(apc.Uid, apc.Component);
    }

    private void DisableDuplicateApc(Entity<RMCApcComponent> apc, Entity<RMCAreaPowerComponent> area)
    {
        apc.Comp.ExternalPower = false;
        apc.Comp.PowerStatus = RMCApcPowerStatus.Faulted;
        apc.Comp.RequestedPower = 0;
        apc.Comp.DeliveredPower = 0;
        apc.Comp.LocalGeneration = 0;
        apc.Comp.ExportedPower = 0;
        apc.Comp.ChargeRequest = 0;
        apc.Comp.ChargePower = 0;
        apc.Comp.SurplusCycles = 0;
        UpdateApcChannel(apc, area, RMCPowerChannel.Equipment, false);
        UpdateApcChannel(apc, area, RMCPowerChannel.Lighting, false);
        UpdateApcChannel(apc, area, RMCPowerChannel.Environment, false);
        Dirty(apc);
    }

    internal static Dictionary<EntityUid, float> AllocateMaxMin(
        List<(EntityUid Uid, float Request)> requests,
        float available)
    {
        var result = new Dictionary<EntityUid, float>();
        var remaining = requests.Where(request => request.Request > 0).OrderBy(request => request.Uid).ToList();
        available = Math.Max(0, available);

        while (remaining.Count > 0 && available > 0)
        {
            var share = available / remaining.Count;
            var satisfied = remaining.Where(request => request.Request <= share).ToList();
            if (satisfied.Count == 0)
            {
                foreach (var request in remaining)
                {
                    result[request.Uid] = share;
                }

                break;
            }

            foreach (var request in satisfied)
            {
                result[request.Uid] = request.Request;
                available -= request.Request;
                remaining.Remove(request);
            }
        }

        return result;
    }

    private static void DistributeSources(List<Source> sources, float power, bool exported)
    {
        var available = sources.Sum(source => exported ? source.ExportAvailable : source.Available);
        if (available <= 0 || power <= 0)
            return;

        foreach (var source in sources.OrderBy(source => source.Uid))
        {
            var capacity = exported ? source.ExportAvailable : source.Available;
            source.Used += power * capacity / available;
        }
    }

    private static bool StorageCanOutput(Storage storage)
    {
        return storage.Component.OutputEnabled && storage.Component.OutputLimit > 0 && storage.Battery.CurrentCharge > 0;
    }

    private static float GetStorageOutput(Storage storage, float deltaTime)
    {
        if (!StorageCanOutput(storage))
            return 0;

        return Math.Min(
            Math.Min(storage.Component.MaxOutput, storage.Component.OutputLimit),
            storage.Battery.CurrentCharge / deltaTime);
    }

    private void DistributeStorageOutput(List<Storage> storages, float power, float deltaTime)
    {
        var available = storages.Sum(storage => GetStorageOutput(storage, deltaTime));
        if (available <= 0 || power <= 0)
            return;

        foreach (var storage in storages.OrderBy(storage => storage.Uid))
        {
            var output = power * GetStorageOutput(storage, deltaTime) / available;
            if (output <= 0)
                continue;

            storage.Component.CurrentOutput = output;
            _battery.SetCharge(storage.Uid,
                Math.Max(0, storage.Battery.CurrentCharge - output * deltaTime),
                storage.Battery);
            Dirty(storage.Uid, storage.Component);
        }
    }

    private float DistributeStorageInput(List<Storage> storages, float power, float deltaTime)
    {
        var capacities = new Dictionary<EntityUid, float>();
        foreach (var storage in storages)
        {
            if (!storage.Component.InputEnabled || storage.Component.CurrentOutput > 0)
                continue;

            var room = Math.Max(0, storage.Battery.MaxCharge - storage.Battery.CurrentCharge) / deltaTime;
            capacities[storage.Uid] = Math.Min(
                Math.Min(storage.Component.MaxInput, storage.Component.InputLimit),
                room);
        }

        var capacity = capacities.Values.Sum();
        var used = Math.Min(Math.Max(0, power), capacity);
        if (capacity <= 0 || used <= 0)
            return 0;

        foreach (var storage in storages.OrderBy(storage => storage.Uid))
        {
            if (!capacities.TryGetValue(storage.Uid, out var storageCapacity) || storageCapacity <= 0)
                continue;

            var input = used * storageCapacity / capacity;
            storage.Component.CurrentInput = input;
            storage.Component.InputState = input + 0.01f >= storageCapacity
                ? RMCPowerStorageInputState.Full
                : RMCPowerStorageInputState.Partial;
            _battery.SetCharge(storage.Uid,
                Math.Min(storage.Battery.MaxCharge, storage.Battery.CurrentCharge + input * deltaTime),
                storage.Battery);
            Dirty(storage.Uid, storage.Component);
        }

        return used;
    }

    private void UpdateNetworkEntity(Network network)
    {
        if (!_networkEntities.TryGetValue(network.Key, out var uid) || TerminatingOrDeleted(uid))
        {
            uid = Spawn(null, new EntityCoordinates(network.Key.Map, default));
            var component = EnsureComp<RMCPowerNetComponent>(uid);
            component.PowerNet = network.Key.PowerNet;
            _networkEntities[network.Key] = uid;
        }

        var powerNet = Comp<RMCPowerNetComponent>(uid);
        powerNet.Stats = network.Stats;
        Dirty(uid, powerNet);
        _networkStats[network.Key] = network.Stats;

        var ev = new RMCPowerNetworkUpdatedEvent(network.Key, network.Stats);
        RaiseLocalEvent(ev);
    }

    private void UpdateOverloadedReactorFeedback(TimeSpan time)
    {
        foreach (var uid in _trackedSources.Order())
        {
            if (TerminatingOrDeleted(uid) ||
                !_fusionReactorQuery.TryComp(uid, out var reactor) ||
                !TryComp(uid, out TransformComponent? xform))
            {
                continue;
            }

            if (!reactor.Overloaded)
            {
                reactor.OverloadNextFeedbackAt = TimeSpan.Zero;
                continue;
            }

            if (reactor.State != RMCFusionReactorState.Working || xform.MapUid == null)
                continue;

            if (reactor.OverloadNextFeedbackAt == TimeSpan.Zero)
            {
                reactor.OverloadNextFeedbackAt = time + GetOverloadFeedbackDelay(reactor);
                continue;
            }

            if (time < reactor.OverloadNextFeedbackAt)
                continue;

            reactor.OverloadNextFeedbackAt = time + GetOverloadFeedbackDelay(reactor);
            var hiss = _random.Prob(0.4f);
            _popup.PopupEntity(
                Loc.GetString(hiss
                    ? "rmc-fusion-reactor-overload-feedback-hiss"
                    : "rmc-fusion-reactor-overload-feedback-hum",
                    ("reactor", uid)),
                uid,
                PopupType.SmallCaution);
            _audio.PlayPvs(hiss ? reactor.OverloadHissSound : reactor.OverloadHumSound, uid);
        }
    }

    private TimeSpan GetOverloadFeedbackDelay(RMCFusionReactorComponent reactor)
    {
        var min = Math.Max(0, reactor.OverloadFeedbackMinDelay.TotalSeconds);
        var max = Math.Max(min, reactor.OverloadFeedbackMaxDelay.TotalSeconds);
        if (max <= min)
            return TimeSpan.FromSeconds(min);

        return TimeSpan.FromSeconds(_random.NextFloat((float) min, (float) max));
    }

    private static bool GetChannelTarget(RMCApcComponent apc, RMCPowerChannel channel, bool automatic, bool available)
    {
        return apc.Channels[(int) channel].Button switch
        {
            RMCApcButtonState.Off => false,
            RMCApcButtonState.On => available,
            _ => automatic,
        };
    }

    private sealed class Network(RMCPowerNetworkKey key)
    {
        public readonly List<Apc> Apcs = new();
        public readonly Dictionary<EntityUid, Apc> ApcsByArea = new();
        public readonly RMCPowerNetworkKey Key = key;
        public readonly Dictionary<EntityUid, List<Source>> LocalSources = new();
        public readonly List<Source> Sources = new();
        public readonly List<Storage> Storages = new();
        public RMCPowerNetworkStats Stats;
    }

    private sealed class Apc(
        EntityUid uid,
        RMCApcComponent component,
        Entity<RMCAreaPowerComponent> area,
        Entity<BatteryComponent>? cell)
    {
        public readonly Entity<RMCAreaPowerComponent> Area = area;
        public readonly Entity<BatteryComponent>? Cell = cell;
        public readonly float[] ChannelLoads = new float[Enum.GetValues<RMCPowerChannel>().Length];
        public readonly RMCApcComponent Component = component;
        public readonly EntityUid Uid = uid;
        public float ChargeAllocated;
        public float ChargeRequest;
        public float Load;
        public float LoadAllocated;
    }

    private sealed class Source(EntityUid uid, RMCPowerSourceComponent component, float available)
    {
        public readonly float Available = available;
        public readonly RMCPowerSourceComponent Component = component;
        public readonly EntityUid Uid = uid;
        public float ExportAvailable;
        public float Used;
    }

    private sealed class Storage(EntityUid uid, RMCPowerStorageComponent component, BatteryComponent battery)
    {
        public readonly BatteryComponent Battery = battery;
        public readonly RMCPowerStorageComponent Component = component;
        public readonly EntityUid Uid = uid;
    }
}
