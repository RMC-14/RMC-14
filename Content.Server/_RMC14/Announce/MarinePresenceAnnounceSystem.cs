using System.Globalization;
using System.Linq;
using Robust.Shared.Timing;
using Content.Server._RMC14.Marines;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.ARES;
using Content.Shared.Radio;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Server.StationRecords;

namespace Content.Server._RMC14.Announce
{
    public sealed class MarinePresenceAnnounceSystem : EntitySystem
    {
        [Dependency] private readonly ARESSystem _ares = default!;
        [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
        [Dependency] private readonly SharedRankSystem _rankSystem = default!;
        [Dependency] private readonly SquadSystem _squad = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StationRecordsSystem _stationRecords = default!;

        [ValidatePrototypeId<RadioChannelPrototype>]
        public readonly ProtoId<RadioChannelPrototype> CommonChannel = "MarineCommon";

        public void AnnounceLateJoin(bool lateJoin, bool silent, EntityUid mob, string jobId, string jobName, JobPrototype jobPrototype)
        {
            var ares = _ares.EnsureARES();
            var fullRankName = _rankSystem.GetSpeakerFullRankName(mob) ?? Name(mob);
            var rankName = _rankSystem.GetSpeakerRankName(mob) ?? Name(mob);

            if (lateJoin && !silent)
            {
                if (jobPrototype.JoinNotifyCrew)
                {
                    _marineAnnounce.AnnounceARESStaging(ares,
                        Loc.GetString("rmc-latejoin-arrival-announcement-special",
                        ("character", fullRankName)),
                        jobPrototype.LatejoinArrivalSound,
                        null);
                }
                else
                {
                    // Getting all department prototypes
                    var departmentPrototypes = _prototypeManager.EnumeratePrototypes<DepartmentPrototype>().ToList();

                    // To track channels that have already been processed, prevents spam in the same channel multiple times
                    var processedChannels = new HashSet<ProtoId<RadioChannelPrototype>>();
                    bool departmentChannelFound = false;
                    bool isHead = false;

                    // We are trying to send a message about arrival to the radio channel of the departments to which the player belongs
                    foreach (var department in departmentPrototypes)
                    {
                        if (!department.Roles.Contains(jobId))
                            continue;

                        // Check if this role is a department head
                        if (department.HeadOfDepartment == jobId)
                        {
                            isHead = true;
                        }

                        var channelId = department.DepartmentRadio;

                        // If the department doesn't have a channel, but it's a combat marine, we try to get his squad channel
                        if (channelId == null && _squad.TryGetMemberSquad(mob, out var squad) && squad.Comp.Radio != null)
                        {
                            channelId = squad.Comp.Radio;
                        }

                        // If after all checks the channel is still not found or we have already processed this channel, skip it
                        if (channelId == null || !processedChannels.Add(channelId.Value))
                            continue;

                        departmentChannelFound = true;

                        _marineAnnounce.AnnounceRadio(ares,
                            Loc.GetString("rmc-latejoin-arrival-announcement",
                            ("character", rankName),
                            ("entity", mob),
                            ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))),
                            channelId.Value);
                    }

                    // If no department channel found OR the player is the head of the department, send to CommonChannel
                    if (!departmentChannelFound || isHead)
                    {
                        _marineAnnounce.AnnounceRadio(ares,
                            Loc.GetString("rmc-latejoin-arrival-announcement",
                            ("character", rankName),
                            ("entity", mob),
                            ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))),
                            CommonChannel);
                    }
                }
            }
        }

        public void AnnounceEarlyLeave(Entity<CryostorageContainedComponent> ent, uint? recordId, EntityUid? station, string jobName)
        {
            var ares = _ares.EnsureARES();
            var rankName = _rankSystem.GetSpeakerRankName(ent.Owner) ?? Name(ent.Owner);
            JobPrototype? jobProto = null;

            if (!TryComp<StationRecordsComponent>(station, out var stationRecords))
                return;

            if (recordId != null && station != null)
            {
                var key = new StationRecordKey(recordId.Value, station.Value);
                if (_stationRecords.TryGetRecord<GeneralStationRecord>(key, out var entry, stationRecords) && !string.IsNullOrWhiteSpace(entry.JobPrototype))
                    _prototypeManager.TryIndex(entry.JobPrototype, out jobProto);
            }

            // Getting all department prototypes
            var departmentPrototypes = _prototypeManager.EnumeratePrototypes<DepartmentPrototype>().ToList();

            // To track channels that have already been processed, prevents spam in the same channel multiple times
            var processedChannels = new HashSet<ProtoId<RadioChannelPrototype>>();
            bool departmentChannelFound = false;
            bool isHead = false;

            if (jobProto != null)
            {
                // We are trying to send a message about earlyleave to the radio channel of the departments to which the player belongs
                foreach (var department in departmentPrototypes)
                {
                    if (!department.Roles.Contains(jobProto.ID))
                        continue;

                    // Check if this role is a department head
                    if (department.HeadOfDepartment == jobProto.ID)
                    {
                        isHead = true;
                    }

                    var channelId = department.DepartmentRadio;

                    // If the department doesn't have a channel, but it's a combat marine, we try to get his squad channel
                    if (channelId == null && _squad.TryGetMemberSquad(ent.Owner, out var squad) && squad.Comp.Radio != null)
                    {
                        channelId = squad.Comp.Radio;
                    }

                    // If after all checks the channel is still not found or we have already processed this channel, skip it
                    if (channelId == null || !processedChannels.Add(channelId.Value))
                        continue;

                    departmentChannelFound = true;

                    _marineAnnounce.AnnounceRadio(ares,
                        Loc.GetString("rmc-earlyleave-cryo-announcement",
                        ("character", rankName),
                        ("entity", ent.Owner),
                        ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))),
                        channelId.Value);
                }

                // If no department channel found OR the player is the head of the department, send to CommonChannel
                if (!departmentChannelFound || isHead)
                {
                    _marineAnnounce.AnnounceRadio(ares,
                        Loc.GetString("rmc-earlyleave-cryo-announcement",
                        ("character", rankName),
                        ("entity", ent.Owner),
                        ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))),
                        CommonChannel);
                }
            }
            else
            {
                _marineAnnounce.AnnounceRadio(ares,
                        Loc.GetString("rmc-earlyleave-cryo-announcement",
                        ("character", rankName),
                        ("entity", ent.Owner),
                        ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))),
                        CommonChannel);
            }
        }
    }
}
