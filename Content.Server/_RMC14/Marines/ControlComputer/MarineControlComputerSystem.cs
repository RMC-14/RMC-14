using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Survivor;
using Content.Shared.Body.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Jobs;
using System.Linq;
using Robust.Shared.Localization;

namespace Content.Server._RMC14.Marines.ControlComputer;

public sealed class MarineControlComputerSystem : SharedMarineControlComputerSystem
{
    [Dependency] private readonly SharedRankSystem _rank = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SquadSystem _squads = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MarineComponent, BeingGibbedEvent>(OnMarineGibbed);
    }

    protected override MarineMedalsPanelBuiState BuildMedalsPanelState(Entity<MarineControlComputerComponent> ent)
    {
        var groups = new Dictionary<string, MarineRecommendationGroup>();
        
        // Collect recommendations from all computers to ensure they're visible from any device
        var allRecommendations = new HashSet<MarineAwardRecommendationInfo>();
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out _, out var computer))
        {
            allRecommendations.UnionWith(computer.AwardRecommendations);
        }

        // Get all LastPlayerIds who have already been awarded medals
        var awardedLastPlayerIds = GetAllAwardedMedalLastPlayerIds();

        // Group recommendations by recommended marine, excluding those who already received medals
        foreach (var recommendation in allRecommendations)
        {
            var recommendedId = recommendation.RecommendedLastPlayerId;
            
            // Skip recommendations for marines who have already been awarded medals
            if (awardedLastPlayerIds.Contains(recommendedId))
                continue;
            
            if (!groups.TryGetValue(recommendedId, out var group))
            {
                // Get marine info
                var marineInfo = GetMarineInfo(recommendedId);
                
                group = new MarineRecommendationGroup
                {
                    LastPlayerId = recommendedId,
                    Name = marineInfo.Name,
                    Rank = marineInfo.Rank,
                    Squad = marineInfo.Squad,
                    Job = marineInfo.Job,
                    Recommendations = new List<MarineRecommendationInfo>()
                };
                groups[recommendedId] = group;
            }

            // Get recommender info
            var recommenderInfo = GetMarineInfo(recommendation.RecommenderLastPlayerId);
            
            group.Recommendations.Add(new MarineRecommendationInfo
            {
                RecommenderLastPlayerId = recommendation.RecommenderLastPlayerId,
                RecommenderName = recommenderInfo.Name,
                RecommenderRank = recommenderInfo.Rank,
                RecommenderSquad = recommenderInfo.Squad,
                RecommenderJob = recommenderInfo.Job,
                Reason = recommendation.Reason
            });
        }

        return new MarineMedalsPanelBuiState(groups.Values.ToList());
    }

    private (string Name, string? Rank, string? Squad, string Job) GetMarineInfo(string lastPlayerId)
    {
        // Try to find alive marine
        var receivers = EntityQueryEnumerator<CommendationReceiverComponent, MarineComponent>();
        while (receivers.MoveNext(out var uid, out var receiver, out _))
        {
            if (receiver.LastPlayerId == lastPlayerId)
            {
                var rank = _rank.GetRankString(uid);
                var job = GetJobName(uid);
                var squad = GetSquadName(uid);
                return (Name(uid), rank, squad, job);
            }
        }

        // Try to find gibbed marine
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out _, out var computer))
        {
            if (computer.GibbedMarines.FirstOrDefault(info => info.LastPlayerId == lastPlayerId) is { } gibbedInfo)
            {
                return (gibbedInfo.Name, gibbedInfo.Rank, gibbedInfo.Squad, gibbedInfo.Job);
            }
        }

        // Fallback
        return ("Unknown", null, null, "Unknown");
    }

    private void OnMarineGibbed(Entity<MarineComponent> ent, ref BeingGibbedEvent ev)
    {
        if (HasComp<RMCSurvivorComponent>(ent))
        {
            return;
        }
        // The entity being gibbed is the one that raised the event (ent)
        if (!TryComp(ent, out CommendationReceiverComponent? receiver) ||
            receiver.LastPlayerId == null || receiver.LastPlayerId == string.Empty)
        {
            return;
        }

        var rank = _rank.GetRankString(ent.Owner);
        var job = GetJobName(ent.Owner);
        var squadName = GetSquadName(ent.Owner);

        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var computerId, out var computer))
        {
            var info = new GibbedMarineInfo
            {
                Name = Name(ent),
                LastPlayerId = receiver.LastPlayerId,
                Rank = rank,
                Job = job,
                Squad = squadName
            };

            computer.GibbedMarines.Add(info);
            Dirty(computerId, computer);
        }
    }

    private string GetJobName(EntityUid actor)
    {
        if (TryComp<MindContainerComponent>(actor, out var mind) && mind.Mind is { } mindId)
            return _jobs.MindTryGetJobName(mindId);

        return Loc.GetString("generic-unknown-title");
    }

    private string? GetSquadName(EntityUid marine)
    {
        if (_squads.TryGetMemberSquad((marine, null), out var squad))
            return Name(squad);

        return null;
    }
}
