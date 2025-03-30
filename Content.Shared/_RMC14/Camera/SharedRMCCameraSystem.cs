using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Areas;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Camera;

// we would be using the upstream system for cameras IF IT WAS NOT ABOMINABLE DOGSHIT
public abstract class SharedRMCCameraSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<EntProtoId> _refresh = new();

    private readonly Dictionary<string, int> _cameraNames = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<RMCCameraComponent, MapInitEvent>(OnCameraMapInit, after: [typeof(AreaSystem)]);
        SubscribeLocalEvent<RMCCameraComponent, ComponentRemove>(OnCameraRemove);
        SubscribeLocalEvent<RMCCameraComponent, EntityTerminatingEvent>(OnCameraTerminating);

        SubscribeLocalEvent<RMCCameraComputerComponent, MapInitEvent>(OnComputerMapInit, after: [typeof(AreaSystem)]);

        SubscribeLocalEvent<RMCCameraWatcherComponent, ComponentRemove>(OnWatcherRemove);
        SubscribeLocalEvent<RMCCameraWatcherComponent, EntityTerminatingEvent>(OnWatcherTerminating);

        Subs.BuiEvents<RMCCameraComputerComponent>(RMCCameraUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnComputerBuiOpened);
                subs.Event<BoundUIClosedEvent>(OnComputerBuiClosed);
                subs.Event<RMCCameraWatchBuiMsg>(OnComputerWatchBuiMsg);
                subs.Event<RMCCameraPreviousBuiMsg>(OnComputerPreviousBuiMsg);
                subs.Event<RMCCameraNextBuiMsg>(OnComputerNextBuiMsg);
            });
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _cameraNames.Clear();
    }

    private void OnCameraMapInit(Entity<RMCCameraComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.Rename)
            return;

        if (ent.Comp.Id is { } id)
            _refresh.Add(id);

        if (!_area.TryGetArea(ent, out _, out var areaProto))
            return;

        var areaName = areaProto.Name;
        var count = _cameraNames.GetValueOrDefault(areaName);
        _metaData.SetEntityName(ent, $"{areaName} #{++count}");
        _cameraNames[areaName] = count;
    }

    private void OnCameraRemove(Entity<RMCCameraComponent> ent, ref ComponentRemove args)
    {
        OnCameraRemoved(ent);
    }

    private void OnCameraTerminating(Entity<RMCCameraComponent> ent, ref EntityTerminatingEvent args)
    {
        OnCameraRemoved(ent);
    }

    private void OnComputerMapInit(Entity<RMCCameraComputerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.CameraIds.Clear();
        ent.Comp.CameraNames.Clear();

        var query = EntityQueryEnumerator<RMCCameraComponent>();
        while (query.MoveNext(out var uid, out var camera))
        {
            if (camera.Id != ent.Comp.Id)
                continue;

            ent.Comp.CameraIds.Add(GetNetEntity(uid));
            ent.Comp.CameraNames.Add(Name(uid));
        }

        Dirty(ent);
    }

    private void OnWatcherRemove(Entity<RMCCameraWatcherComponent> ent, ref ComponentRemove args)
    {
        OnWatcherRemoved(ent);
    }

    private void OnWatcherTerminating(Entity<RMCCameraWatcherComponent> ent, ref EntityTerminatingEvent args)
    {
        OnWatcherRemoved(ent);
    }

    private void OnComputerBuiOpened(Entity<RMCCameraComputerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var actor = args.Actor;
        ent.Comp.Watchers.Add(actor);
        Dirty(ent);

        var watcher = EnsureComp<RMCCameraWatcherComponent>(actor);
        watcher.Computer = null;
        Dirty(actor, watcher);

        Refresh(ent, null);
    }

    private void OnComputerBuiClosed(Entity<RMCCameraComputerComponent> ent, ref BoundUIClosedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var actor = args.Actor;
        ent.Comp.Watchers.Remove(actor);
        Dirty(ent);

        RemCompDeferred<RMCCameraWatcherComponent>(actor);
    }

    private void OnComputerWatchBuiMsg(Entity<RMCCameraComputerComponent> ent, ref RMCCameraWatchBuiMsg args)
    {
        if (_timing.ApplyingState)
            return;

        if (!TryGetEntity(args.Camera, out var camera))
            return;

        if (!ent.Comp.CameraIds.Contains(args.Camera))
            return;

        var old = ent.Comp.CurrentCamera;
        ent.Comp.CurrentCamera = camera;
        Refresh(ent, old);
    }

    private void OnComputerPreviousBuiMsg(Entity<RMCCameraComputerComponent> ent, ref RMCCameraPreviousBuiMsg args)
    {
        var old = ent.Comp.CurrentCamera;
        var index = 0;
        if (old != null &&
            TryGetNetEntity(old, out var netCamera))
        {
            index = ent.Comp.CameraIds.IndexOf(netCamera.Value) - 1;
            if (index < 0 || index >= ent.Comp.CameraIds.Count)
                index = ent.Comp.CameraIds.Count - 1;
        }

        if (index >= 0 &&
            index < ent.Comp.CameraIds.Count &&
            TryGetEntity(ent.Comp.CameraIds[index], out var camera))
        {
            ent.Comp.CurrentCamera = camera;
        }

        Refresh(ent, old);
    }

    private void OnComputerNextBuiMsg(Entity<RMCCameraComputerComponent> ent, ref RMCCameraNextBuiMsg args)
    {
        var old = ent.Comp.CurrentCamera;
        var index = 0;
        if (old != null &&
            TryGetNetEntity(old, out var netCamera))
        {
            index = ent.Comp.CameraIds.IndexOf(netCamera.Value) + 1;
            if (index < 0 || index >= ent.Comp.CameraIds.Count)
                index = 0;
        }

        if (index >= 0 &&
            index < ent.Comp.CameraIds.Count &&
            TryGetEntity(ent.Comp.CameraIds[index], out var camera))
        {
            ent.Comp.CurrentCamera = camera;
        }

        Refresh(ent, old);
    }

    protected virtual void Refresh(Entity<RMCCameraComputerComponent> ent, EntityUid? old)
    {
        Dirty(ent);
    }

    protected virtual void OnWatcherRemoved(Entity<RMCCameraWatcherComponent> watcher)
    {
        if (TryComp(watcher.Comp.Computer, out RMCCameraComputerComponent? computer))
        {
            computer.Watchers.Remove(watcher);
            Dirty(watcher.Comp.Computer.Value, computer);
        }
    }

    public bool GetComputerCameraName(Entity<RMCCameraComputerComponent> computer, EntityUid camera, [NotNullWhen(true)] out string? name)
    {
        var index = computer.Comp.CameraIds.IndexOf(GetNetEntity(camera));
        if (index < 0)
        {
            name = default;
            return false;
        }

        name = computer.Comp.CameraNames[index];
        return true;
    }

    private void OnCameraRemoved(Entity<RMCCameraComponent> camera)
    {
        var netCamera = GetNetEntity(camera);
        var computers = EntityQueryEnumerator<RMCCameraComputerComponent>();
        while (computers.MoveNext(out var uid, out var comp))
        {
            if (comp.Id != camera.Comp.Id)
                continue;

            if (TerminatingOrDeleted(uid))
                continue;

            var index = comp.CameraIds.IndexOf(netCamera);
            if (index >= 0)
            {
                comp.CameraIds.RemoveAt(index);
                comp.CameraNames.RemoveAt(index);
            }

            if (comp.CurrentCamera == camera)
                comp.CurrentCamera = null;

            Dirty(uid, comp);
        }
    }

    public override void Update(float frameTime)
    {
        if (_refresh.Count == 0)
            return;

        if (_net.IsClient)
        {
            _refresh.Clear();
            return;
        }

        var monitors = new List<Entity<RMCCameraComputerComponent>>();
        foreach (var refresh in _refresh)
        {
            monitors.Clear();
            var monitorQuery = EntityQueryEnumerator<RMCCameraComputerComponent>();
            while (monitorQuery.MoveNext(out var uid, out var computer))
            {
                if (computer.Id == refresh)
                    monitors.Add((uid, computer));
            }

            if (monitors.Count == 0)
                continue;

            // TODO RMC14 consistent ordering
            var cameraIds = new List<NetEntity>();
            var cameraNames = new List<string>();
            var cameraQuery = EntityQueryEnumerator<RMCCameraComponent>();
            while (cameraQuery.MoveNext(out var uid, out var camera))
            {
                if (camera.Id != refresh)
                    continue;

                cameraIds.Add(GetNetEntity(uid));
                cameraNames.Add(Name(uid));
            }

            foreach (var monitor in monitors)
            {
                monitor.Comp.CameraIds = cameraIds;
                monitor.Comp.CameraNames = cameraNames;
                Dirty(monitor);
            }
        }

        _refresh.Clear();
    }
}
