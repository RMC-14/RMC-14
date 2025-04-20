using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.Doors;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Ghost;
using Content.Shared.Lock;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.AlertLevel;

public sealed class RMCAlertLevelSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ARESSystem _ares = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;

    private EntityQuery<GhostComponent> _ghostQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipHijackLandedEvent>(OnDropshipHijackLanded);

        _ghostQuery = GetEntityQuery<GhostComponent>();
    }

    private void OnDropshipHijackLanded(ref DropshipHijackLandedEvent ev)
    {
        // TODO RMC14 is this real
        // Set(RMCAlertLevels.Red);
    }

    private bool TryGetAlertLevel(out Entity<RMCAlertLevelComponent> alert)
    {
        var query = EntityQueryEnumerator<RMCAlertLevelComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            alert = (uid, comp);
            return true;
        }

        alert = default;
        return false;
    }

    private Entity<RMCAlertLevelComponent> EnsureAlertLevel()
    {
        if (TryGetAlertLevel(out var alert))
            return alert;

        var uid = Spawn();
        var comp = EnsureComp<RMCAlertLevelComponent>(uid);
        return (uid, comp);
    }

    public RMCAlertLevels? Get()
    {
        if (!TryGetAlertLevel(out var alert))
            return null;

        return alert.Comp.Level;
    }

    public bool IsRedOrDeltaAlert()
    {
        return Get() == RMCAlertLevels.Red || Get() ==  RMCAlertLevels.Delta;
    }

    public void Set(RMCAlertLevels level, EntityUid? user, bool playSound = true, bool sendAnnouncement = true)
    {
        var ent = EnsureAlertLevel();
        if (ent.Comp.Level == level)
            return;

        var (sound, message, announcement) = level switch
        {
            RMCAlertLevels.Green => (ent.Comp.GreenSound, ent.Comp.GreenMessage, null),
            RMCAlertLevels.Blue when ent.Comp.Level < RMCAlertLevels.Blue => (ent.Comp.BlueElevatedSound, ent.Comp.BlueElevatedMessage, null),
            RMCAlertLevels.Blue when ent.Comp.Level > RMCAlertLevels.Blue => (ent.Comp.BlueLoweredSound, ent.Comp.BlueLoweredMessage, null),
            RMCAlertLevels.Red when ent.Comp.Level < RMCAlertLevels.Red => (ent.Comp.RedElevatedSound, ent.Comp.RedElevatedMessage, null),
            RMCAlertLevels.Red when ent.Comp.Level > RMCAlertLevels.Red => (ent.Comp.RedLoweredSound, ent.Comp.RedLoweredMessage, null),
            RMCAlertLevels.Delta => (ent.Comp.DeltaSound, ent.Comp.DeltaAnnouncement, ent.Comp.DeltaAnnouncement),
            _ => (null, null, null),
        };

        ent.Comp.Level = level;
        Dirty(ent);

        _adminLog.Add(LogType.RMCAlertLevel, $"{ToPrettyString(user)} set alert level to {level}");

        var almayers = new HashSet<EntityUid>();
        var almayerQuery = EntityQueryEnumerator<AlmayerComponent>();
        while (almayerQuery.MoveNext(out var uid, out _))
        {
            almayers.Add(uid);
        }

        var transformQuery = EntityManager.TransformQuery;
        var filter = Filter.Empty()
            .AddWhereAttachedEntity(entity =>
            {
                if (transformQuery.CompOrNull(entity)?.MapUid is { } map && almayers.Contains(map))
                    return true;

                if (_ghostQuery.HasComp(entity))
                    return true;

                return false;
            });

        // Play alarm sound if playSound == true
        if (playSound && _net.IsServer)
        {
            _audio.PlayGlobal(sound, filter, true);
        }

        // Send announcement if sendAnnouncement == true
        if (sendAnnouncement)
        {
            if (announcement != null)
            {
                _marineAnnounce.AnnounceToMarines(Loc.GetString(announcement));
            }
            else if (message != null)
            {
                var ares = _ares.EnsureARES();
                _marineAnnounce.AnnounceRadio(ares, Loc.GetString(message.Value), ent.Comp.RadioChannel);
            }
        }

        var unlockQuery = EntityQueryEnumerator<RMCUnlockOnAlertLevelComponent, LockComponent>();
        while (unlockQuery.MoveNext(out var uid, out var unlock, out var lockComp))
        {
            if (unlock.Level <= level)
            {
                _lock.Unlock(uid, null, lockComp);
            }
            else
            {
                SharedEntityStorageComponent? entityStorageComp = null;
                if (_entityStorage.ResolveStorage(uid, ref entityStorageComp))
                    _entityStorage.CloseStorage(uid, entityStorageComp); // Close a locker before locking it.
                _lock.Lock(uid, null, lockComp);
            }
        }

        var openQuery = EntityQueryEnumerator<RMCOpenOnAlertLevelComponent, DoorComponent, RMCPodDoorComponent>();
        while (openQuery.MoveNext(out var uid, out var unlock, out var door, out var podDoor))
        {
            if (unlock.Id != podDoor.Id)
                continue;

            if (unlock.Level <= level)
                _door.TryOpen(uid, door);
            else
                _door.TryClose(uid, door);
        }

        var displays = EntityQueryEnumerator<RMCAlertLevelDisplayComponent>();
        while (displays.MoveNext(out var uid, out _))
        {
            _appearance.SetData(uid, RMCAlertLevelsVisuals.Alert, level);
        }
    }
}
