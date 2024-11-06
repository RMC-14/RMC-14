using System.Runtime.InteropServices;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Power;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.PowerCell;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Power;

public sealed class RMCPowerSystem : SharedRMCPowerSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;

    [ViewVariables]
    private TimeSpan _nextUpdate;

    [ViewVariables]
    private TimeSpan _updateEvery;

    [ViewVariables]
    private float _powerLoadMultiplier;

    private EntityQuery<ApcPowerReceiverComponent> _apcPowerReceiverQuery;
    private EntityQuery<RMCApcComponent> _apcQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;
    private EntityQuery<RMCAreaPowerComponent> _areaPowerQuery;
    private EntityQuery<BatteryComponent> _batteryQuery;

    private readonly Dictionary<EntityUid, List<(Entity<RMCApcComponent, TransformComponent> Apc, Entity<BatteryComponent>? Cell)>> _apcs = new();
    private readonly List<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        _apcPowerReceiverQuery = GetEntityQuery<ApcPowerReceiverComponent>();
        _apcQuery = GetEntityQuery<RMCApcComponent>();
        _appearanceQuery = GetEntityQuery<AppearanceComponent>();
        _areaPowerQuery = GetEntityQuery<RMCAreaPowerComponent>();
        _batteryQuery = GetEntityQuery<BatteryComponent>();

        SubscribeLocalEvent<RMCPowerReceiverComponent, PowerChangedEvent>(OnReceiverPowerChanged);
        SubscribeLocalEvent<ApcPowerReceiverComponent, MapInitEvent>(ReceiverOnMapInit);
        SubscribeLocalEvent<RMCPowerUsageDisplayComponent, ExaminedEvent>(OnUsageDisplayEvent);

        Subs.CVar(_config, RMCCVars.RMCPowerUpdateEverySeconds, v => _updateEvery = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCPowerLoadMultiplier, v => _powerLoadMultiplier = v, true);
    }

    private void OnUsageDisplayEvent(Entity<RMCPowerUsageDisplayComponent> ent, ref ExaminedEvent args)
    {
        if (!_cell.TryGetBatteryFromSlot(ent, out var battery) || !TryComp<PowerCellDrawComponent>(ent, out var draw))
            return;

        int maxUses = (int)(battery.MaxCharge / draw.UseRate);
        int uses = (int)(battery.CurrentCharge / draw.UseRate);

        args.PushMarkup(Loc.GetString(ent.Comp.PowerText, ("uses", uses), ("maxuses", maxUses)));
    }
    private void OnReceiverPowerChanged(Entity<RMCPowerReceiverComponent> ent, ref PowerChangedEvent args)
    {
        ent.Comp.Mode = args.Powered ? RMCPowerMode.Active : RMCPowerMode.Off;
        ToUpdate.Add(ent);
    }

    private void ReceiverOnMapInit(Entity<ApcPowerReceiverComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.NeedsPower)
        {
            ent.Comp.Powered = true;

            Dirty(ent, ent.Comp);

            var ev = new PowerChangedEvent(true, 0);
            RaiseLocalEvent(ent, ref ev);

            if (_appearanceQuery.TryComp(ent, out var appearance))
                _appearance.SetData(ent, PowerDeviceVisuals.Powered, true, appearance);
        }
    }

    protected override void PowerUpdated(Entity<RMCAreaPowerComponent> area, RMCPowerChannel channel, bool on)
    {
        base.PowerUpdated(area, channel, on);

        var receivers = GetAreaReceivers(area, channel);
        var ev = new PowerChangedEvent(on, 0);
        foreach (var receiver in receivers)
        {
            if (!_apcPowerReceiverQuery.TryComp(receiver, out var receiverComp))
                continue;

            if (receiverComp.Powered == on)
                continue;

            if (!receiverComp.NeedsPower)
                continue;

            receiverComp.Powered = on;
            Dirty(receiver, receiverComp);

            RaiseLocalEvent(receiver, ref ev);

            if (_appearanceQuery.TryComp(receiver, out var appearance))
                _appearance.SetData(receiver, PowerDeviceVisuals.Powered, on, appearance);
        }
    }

    public override bool IsPowered(EntityUid ent)
    {
        return TryComp(ent, out ApcPowerReceiverComponent? receiver) && receiver.Powered;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_nextUpdate > _timing.CurTime)
            return;

        _nextUpdate = _timing.CurTime + _updateEvery;

        _toRemove.Clear();
        foreach (var (map, apcs) in _apcs)
        {
            if (TerminatingOrDeleted(map))
                _toRemove.Add(map);

            apcs.Clear();
        }

        foreach (var remove in _toRemove)
        {
            _apcs.Remove(remove);
        }

        _toRemove.Clear();

        var power = new Dictionary<EntityUid, float>();
        var generators = EntityQueryEnumerator<RMCFusionReactorComponent, TransformComponent>();
        while (generators.MoveNext(out var generator, out var xform))
        {
            if (generator.State != RMCFusionReactorState.Working ||
                xform.MapUid is not { } map)
            {
                continue;
            }

            ref var mapPower = ref CollectionsMarshal.GetValueRefOrAddDefault(power, map, out _);
            mapPower += generator.Watts;
        }

        var areas = EntityQueryEnumerator<RMCAreaPowerComponent>();
        while (areas.MoveNext(out var uid, out var areaPower))
        {
            foreach (var apc in areaPower.Apcs)
            {
                if (TerminatingOrDeleted(apc) ||
                    !TryComp(apc, out TransformComponent? xform))
                {
                    _toRemove.Add(apc);
                    continue;
                }

                if (xform.MapUid is not { } map)
                    continue;

                if (!_apcQuery.TryComp(apc, out var apcComp))
                    continue;

                Entity<BatteryComponent>? cell = null;
                if (_container.TryGetContainer(apc, apcComp.CellContainerSlot, out var container) &&
                    container.ContainedEntities.TryFirstOrNull(out var cellId) &&
                    _batteryQuery.TryComp(cellId, out var battery))
                {
                    cell = (cellId.Value, battery);
                }

                _apcs.GetOrNew(map).Add(((apc, apcComp, xform), cell));
            }

            foreach (var remove in _toRemove)
            {
                areaPower.Apcs.Remove(remove);
            }

            if (_toRemove.Count > 0)
                Dirty(uid, areaPower);

            _toRemove.Clear();
        }

        foreach (var (map, apcList) in _apcs)
        {
            var wattsPer = 0f;
            if (power.TryGetValue(map, out var watts))
                wattsPer = watts / apcList.Count;

            var apcs = CollectionsMarshal.AsSpan(apcList);
            foreach (ref var tuple in apcs)
            {
                ref var apc = ref tuple.Apc;
                ref var cell = ref tuple.Cell;
                var apcComp = apc.Comp1;
                if (cell == null)
                {
                    apcComp.ExternalPower = false;
                    apcComp.ChargeStatus = RMCApcChargeStatus.NotCharging;
                    var channels = apcComp.Channels.AsSpan();
                    foreach (ref var channel in channels)
                    {
                        channel.Watts = 0;
                    }

                    Dirty(apc, apcComp);
                    continue;
                }

                if (!_areaPowerQuery.TryComp(apcComp.Area, out var areaComp))
                    continue;

                var area = new Entity<RMCAreaPowerComponent>(apcComp.Area.Value, areaComp);
                if (apcComp.Broken)
                {
                    UpdateApcChannel(apc, area, RMCPowerChannel.Equipment, false);
                    UpdateApcChannel(apc, area, RMCPowerChannel.Lighting, false);
                    UpdateApcChannel(apc, area, RMCPowerChannel.Environment, false);
                    continue;
                }

                var loadSpan = area.Comp.Load.AsSpan();
                var totalLoad = 0;
                for (var i = 0; i < loadSpan.Length; i++)
                {
                    var load = (int) (loadSpan[i] * _powerLoadMultiplier);
                    totalLoad += load;
                    apcComp.Channels[i].Watts = load;
                }

                var battery = new Entity<BatteryComponent>(cell.Value, cell.Value.Comp);
                var drawn = wattsPer;
                drawn -= totalLoad;
                if (drawn < 0)
                {
                    apcComp.ChargeStatus = RMCApcChargeStatus.NotCharging;
                    _battery.UseCharge(battery, -drawn, battery);
                }
                else
                {
                    _battery.SetCharge(battery, battery.Comp.CurrentCharge + drawn, battery);

                    apcComp.ChargeStatus = _battery.IsFull(battery, battery)
                        ? RMCApcChargeStatus.FullCharge
                        : RMCApcChargeStatus.Charging;
                }

                apcComp.ChargePercentage = battery.Comp.CurrentCharge / battery.Comp.MaxCharge;
                switch (apcComp.ChargePercentage)
                {
                    case > 0.33f:
                        UpdateApcChannel(apc, area, RMCPowerChannel.Equipment, true);
                        UpdateApcChannel(apc, area, RMCPowerChannel.Lighting, true);
                        UpdateApcChannel(apc, area, RMCPowerChannel.Environment, true);
                        break;
                    case > 0.16f:
                        UpdateApcChannel(apc, area, RMCPowerChannel.Equipment, false);
                        UpdateApcChannel(apc, area, RMCPowerChannel.Lighting, true);
                        UpdateApcChannel(apc, area, RMCPowerChannel.Environment, true);
                        break;
                    case > 0.01f:
                        UpdateApcChannel(apc, area, RMCPowerChannel.Equipment, false);
                        UpdateApcChannel(apc, area, RMCPowerChannel.Lighting, false);
                        UpdateApcChannel(apc, area, RMCPowerChannel.Environment, true);
                        break;
                    default:
                        UpdateApcChannel(apc, area, RMCPowerChannel.Equipment, false);
                        UpdateApcChannel(apc, area, RMCPowerChannel.Lighting, false);
                        UpdateApcChannel(apc, area, RMCPowerChannel.Environment, false);
                        break;
                }

                Dirty(apc, apcComp);
            }
        }
    }
}
