using Content.Server.Doors.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared._RMC14.Dropship;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Dropship;

public sealed class RMCDockingPortAirlockControlSystem : EntitySystem
{
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly DropshipSystem _dropship = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCDockingPortAirlockControlComponent, DockEvent>(OnDocked);
        SubscribeLocalEvent<RMCDockingPortAirlockControlComponent, UndockEvent>(OnUndocked);
        SubscribeLocalEvent<RMCDockingPortAirlockControlComponent, DropshipRelayedEvent<FTLCompletedEvent>>(OnFTLCompleted);
        SubscribeLocalEvent<RMCDockingPortAirlockControlComponent, DropshipRelayedEvent<FTLStartedEvent>>(OnFTLStarted);
        SubscribeLocalEvent<DockEvent>(OnAnyDocked);
        SubscribeLocalEvent<UndockEvent>(OnAnyUndocked);
    }

    private void OnDocked(Entity<RMCDockingPortAirlockControlComponent> ent, ref DockEvent args)
    {
        if (ent.Comp.OpenOnDock)
            SetAirlocks(ent, open: true);
    }

    private void OnUndocked(Entity<RMCDockingPortAirlockControlComponent> ent, ref UndockEvent args)
    {
        if (ent.Comp.CloseOnUndock)
            SetAirlocks(ent, open: false);
    }

    private void OnFTLCompleted(Entity<RMCDockingPortAirlockControlComponent> ent, ref DropshipRelayedEvent<FTLCompletedEvent> args)
    {
        if (ent.Comp.OpenOnDock)
            SetAirlocks(ent, open: true);
    }

    private void OnFTLStarted(Entity<RMCDockingPortAirlockControlComponent> ent, ref DropshipRelayedEvent<FTLStartedEvent> args)
    {
        if (ent.Comp.CloseOnUndock)
            SetAirlocks(ent, open: false);
    }

    private void OnAnyDocked(DockEvent args)
    {
        RecalculateSafetyLocks(args.GridAUid);

        if (args.GridBUid != args.GridAUid)
            RecalculateSafetyLocks(args.GridBUid);
    }

    private void OnAnyUndocked(UndockEvent args)
    {
        RecalculateSafetyLocks(args.GridAUid);

        if (args.GridBUid != args.GridAUid)
            RecalculateSafetyLocks(args.GridBUid);
    }

    private void RecalculateSafetyLocks(EntityUid grid)
    {
        if (!HasComp<DropshipComponent>(grid))
            return;

        var anyDocked = false;
        var dockedPorts = new HashSet<EntityUid>();
        var dockDoors = new List<Entity<DoorBoltComponent>>();

        var enumerator = Transform(grid).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!TryComp(child, out DockingComponent? docking))
                continue;

            if (docking.DockedWith != null)
            {
                anyDocked = true;
                dockedPorts.Add(child);
            }

            if (TryComp(child, out DoorBoltComponent? bolt))
                dockDoors.Add((child, bolt));
        }

        foreach (var door in dockDoors)
        {
            if (!anyDocked)
            {
                ReleaseSafetyLock(door);
                continue;
            }

            if (dockedPorts.Contains(door.Owner))
            {
                var released = ReleaseSafetyLock(door, out var wasBolted);
                if (released && !wasBolted)
                    TryOpenDockedDoor(door.Owner);

                continue;
            }

            ApplySafetyLock(door);
        }
    }

    private void ApplySafetyLock(Entity<DoorBoltComponent> door)
    {
        if (!TryComp(door.Owner, out RMCDockingSafetyLockedComponent? safety))
        {
            safety = EnsureComp<RMCDockingSafetyLockedComponent>(door.Owner);
            safety.WasBoltedBeforeSafetyLock = door.Comp.BoltsDown;
        }

        safety.Active = true;
        _dropship.LockDoor(door.Owner);
    }

    private bool ReleaseSafetyLock(Entity<DoorBoltComponent> door)
    {
        return ReleaseSafetyLock(door, out _);
    }

    private bool ReleaseSafetyLock(Entity<DoorBoltComponent> door, out bool wasBolted)
    {
        wasBolted = false;

        if (!TryComp(door.Owner, out RMCDockingSafetyLockedComponent? safety))
            return false;

        wasBolted = safety.WasBoltedBeforeSafetyLock;
        RemComp<RMCDockingSafetyLockedComponent>(door.Owner);

        if (wasBolted)
            _dropship.LockDoor(door.Owner);
        else
            _dropship.UnlockDoor(door.Owner, forceSafetyUnlock: true);

        return true;
    }

    private void TryOpenDockedDoor(EntityUid door)
    {
        if (!TryComp(door, out DockingComponent? docking) ||
            docking.DockedWith == null ||
            !TryComp(door, out DoorComponent? doorComp) ||
            doorComp.State != DoorState.Closed)
        {
            return;
        }

        _door.TryOpen(door, doorComp);
    }

    private void SetAirlocks(Entity<RMCDockingPortAirlockControlComponent> ent, bool open)
    {
        var xform = Transform(ent);
        var grid = xform.GridUid;
        var tagged = new List<Entity<DoorComponent>>();
        var fallback = new List<Entity<DoorComponent>>();
        var scannedDoors = new List<string>();

        foreach (var door in _lookup.GetEntitiesInRange<DoorComponent>(xform.Coordinates, ent.Comp.SearchRadius))
        {
            var doorXform = Transform(door.Owner);
            var hasTags = TryComp(door.Owner, out TagComponent? tags);
            var tagText = hasTags ? string.Join(",", tags!.Tags) : "none";
            scannedDoors.Add($"{ToPrettyString(door.Owner)} grid:{ToPrettyString(doorXform.GridUid)} " +
                             $"pos:{doorXform.LocalPosition} tags:[{tagText}] state:{door.Comp.State}");

            if (doorXform.GridUid != grid)
            {
                continue;
            }

            if (ent.Comp.AirlockTags.Count == 0)
            {
                tagged.Add(door);
                continue;
            }

            if (tags != null &&
                _tag.HasAnyTag(tags, ent.Comp.AirlockTags))
            {
                tagged.Add(door);
                continue;
            }

            if (ent.Comp.FallbackToNearbyDoors)
                fallback.Add(door);
        }

        var doors = tagged.Count > 0 ? tagged : fallback;
        if (doors.Count == 0)
        {
            if (ent.Comp.WarnIfMissing)
                Log.Warning($"RMC docking port {ToPrettyString(ent.Owner)} found no airlocks to {(open ? "open" : "close")} " +
                            $"within {ent.Comp.SearchRadius} tiles. grid={ToPrettyString(grid)}, pos={xform.LocalPosition}, " +
                            $"requiredTags=[{string.Join(",", ent.Comp.AirlockTags)}], scanned=[{string.Join(" | ", scannedDoors)}]");

            return;
        }

        foreach (var door in doors)
        {
            if (open)
            {
                if (door.Comp.State == DoorState.Closed)
                    _door.TryOpen(door.Owner, door.Comp);
            }
            else if (door.Comp.State == DoorState.Open)
            {
                _door.TryClose(door.Owner, door.Comp);
            }

            Log.Info($"RMC docking port {ToPrettyString(ent.Owner)} {(open ? "opened" : "closed")} airlock {ToPrettyString(door.Owner)} " +
                     $"using {(tagged.Count > 0 ? "tagged" : "fallback")} match.");
        }
    }
}
