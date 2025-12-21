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

        // Group recommendations by recommended marine, excluding those who already received medals or were rejected
        foreach (var recommendation in allRecommendations)
        {
            var recommendedId = recommendation.RecommendedLastPlayerId;

            // Skip recommendations for marines who have already been awarded medals
            if (awardedLastPlayerIds.Contains(recommendedId))
                continue;

            // Skip rejected recommendations
            if (recommendation.IsRejected)
                continue;

            if (!groups.TryGetValue(recommendedId, out var group))
            {
                // Try to get current squad if marine is alive and update all recommendations for this marine
                var recommendedCurrentSquad = TryGetCurrentSquad(recommendedId);
                if (recommendedCurrentSquad != null && recommendedCurrentSquad != recommendation.RecommendedSquad)
                {
                    // Update all recommendations for this recommended marine in all computers
                    UpdateRecommendationSquad(recommendedId, recommendedCurrentSquad, true);
                    // Update current recommendation's squad to reflect the change
                    recommendation.RecommendedSquad = recommendedCurrentSquad;
                }

                group = new MarineRecommendationGroup
                {
                    LastPlayerId = recommendedId,
                    Recommendations = new List<MarineAwardRecommendationInfo>()
                };
                groups[recommendedId] = group;
            }

            // Try to get current squad if marine is alive and update all recommendations from this recommender
            var recommenderCurrentSquad = TryGetCurrentSquad(recommendation.RecommenderLastPlayerId);
            if (recommenderCurrentSquad != null && recommenderCurrentSquad != recommendation.RecommenderSquad)
            {
                // Update all recommendations from this recommender in all computers
                UpdateRecommendationSquad(recommendation.RecommenderLastPlayerId, recommenderCurrentSquad, false);
                // Update current recommendation's squad to reflect the change
                recommendation.RecommenderSquad = recommenderCurrentSquad;
            }

            group.Recommendations.Add(recommendation);
        }

        return new MarineMedalsPanelBuiState(groups.Values.ToList());
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

    private string? TryGetCurrentSquad(string lastPlayerId)
    {
        // Try to find alive marine by LastPlayerId
        var receivers = EntityQueryEnumerator<CommendationReceiverComponent, MarineComponent>();
        while (receivers.MoveNext(out var uid, out var receiver, out _))
        {
            if (receiver.LastPlayerId == lastPlayerId)
            {
                return GetSquadName(uid);
            }
        }

        return null;
    }

    private void UpdateRecommendationSquad(string lastPlayerId, string newSquad, bool isRecommended)
    {
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var uid, out var computer))
        {
            var updated = false;
            foreach (var rec in computer.AwardRecommendations)
            {
                if (isRecommended)
                {
                    if (rec.RecommendedLastPlayerId == lastPlayerId)
                    {
                        rec.RecommendedSquad = newSquad;
                        updated = true;
                    }
                }
                else
                {
                    if (rec.RecommenderLastPlayerId == lastPlayerId)
                    {
                        rec.RecommenderSquad = newSquad;
                        updated = true;
                    }
                }
            }
            if (updated)
                Dirty(uid, computer);
        }
    }
}
