using Content.Server.Access.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.StationRecords;

namespace Content.Server._RMC14.StationRecords.Systems;

public sealed class RMCStationRecordsSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly StationRecordsSystem _record = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly SquadSystem _squad = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn, after: [typeof(StationRecordsSystem)]);
        SubscribeLocalEvent<SquadMemberAddedEvent>(OnSquadMemberAdded);
        SubscribeLocalEvent<SquadMemberRemovedEvent>(OnSquadMemberRemoved);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!TryComp<StationRecordsComponent>(args.Station, out var stationRecords))
            return;
        if (!_inventory.TryGetSlotEntity(args.Mob, "id", out var idUid))
            return;
        if (!TryComp(idUid, out StationRecordKeyStorageComponent? keyStorage)
            || keyStorage.Key is not { } key)
            return;
        if (!_record.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
            return;

        if (_squad.TryGetMemberSquad(args.Mob, out var memberSquad))
        {
            generalRecord.Squad = Name(memberSquad);
            generalRecord.SquadColor = memberSquad.Comp.Color;
        }

        _record.Synchronize(key);
    }

    private void OnSquadMemberAdded(ref SquadMemberAddedEvent ev)
    {
        if (!_idCard.TryFindIdCard(ev.Member, out var idCard))
            return;
        if (!TryComp(idCard, out StationRecordKeyStorageComponent? keyStorage)
            || keyStorage.Key is not { } key)
            return;
        if (_record.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
        {
            generalRecord.Squad = Name(ev.Squad);
        }

        _record.Synchronize(key);
    }

    private void OnSquadMemberRemoved(ref SquadMemberRemovedEvent ev)
    {
        if (!_idCard.TryFindIdCard(ev.Member, out var idCard))
            return;
        if (!TryComp(idCard, out StationRecordKeyStorageComponent? keyStorage)
            || keyStorage.Key is not { } key)
            return;
        if (_record.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
        {
            generalRecord.Squad = null;
        }

        _record.Synchronize(key);
    }
}
