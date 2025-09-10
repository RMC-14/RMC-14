using Content.Server.Access.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.StationRecords;

namespace Content.Server._RMC14.StationRecords.Systems;

public sealed class RMCStationRecordsSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _record = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SquadMemberAddedEvent>(OnSquadMemberAdded);
        SubscribeLocalEvent<SquadMemberRemovedEvent>(OnSquadMemberRemoved);
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
