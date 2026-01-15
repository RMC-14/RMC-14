using System.Linq;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Survivor;
using Content.Shared.Body.Events;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Marines.ControlComputer;

public sealed class MarineControlComputerSystem : SharedMarineControlComputerSystem
{
    [Dependency] private readonly SharedRankSystem _rank = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SquadSystem _squads = default!;
    [Dependency] private readonly SharedCommendationSystem _commendation = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MarineComponent, BeingGibbedEvent>(OnMarineGibbed);
    }

    protected override MarineMedalsPanelBuiState BuildMedalsPanelState(Entity<MarineControlComputerComponent> ent, EntityUid? viewerActor = null)
    {
        var groups = new Dictionary<string, MarineRecommendationGroup>();

        // Get viewer's LastPlayerId to exclude recommendations made on them
        string? viewerLastPlayerId = null;
        if (viewerActor != null)
        {
            if (TryComp<CommendationReceiverComponent>(viewerActor, out var receiver) && receiver.LastPlayerId != null)
            {
                viewerLastPlayerId = receiver.LastPlayerId;
            }
            else if (TryComp<ActorComponent>(viewerActor, out var actorComp))
            {
                viewerLastPlayerId = actorComp.PlayerSession.UserId.UserId.ToString();
            }
        }

        // Collect recommendations from all computers to ensure they're visible from any device
        var allRecommendations = new HashSet<MarineAwardRecommendationInfo>();
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out _, out var computer))
        {
            allRecommendations.UnionWith(computer.AwardRecommendations);
        }

        var awardedLastPlayerIds = _commendation.GetCommendations()
            .Where(c => c.Type == CommendationType.Medal)
            .Select(c => c.Receiver)
            .ToHashSet();

        // Group recommendations by recommended marine, excluding those who already received medals or were rejected
        foreach (var recommendation in allRecommendations)
        {
            var recommendedId = recommendation.RecommendedLastPlayerId;

            if (awardedLastPlayerIds.Contains(recommendedId))
                continue;

            if (recommendation.IsRejected)
                continue;

            if (viewerLastPlayerId != null && recommendedId == viewerLastPlayerId)
                continue;

            if (!groups.TryGetValue(recommendedId, out var group))
            {
                group = new MarineRecommendationGroup
                {
                    LastPlayerId = recommendedId,
                    Recommendations = new List<MarineAwardRecommendationInfo>()
                };
                groups[recommendedId] = group;
            }

            group.Recommendations.Add(recommendation);
        }

        // Collect awarded medals from round commendations
        var awardedMedals = _commendation.GetRoundCommendationEntries()
            .Where(e => e.Commendation.Type == CommendationType.Medal)
            .ToList();

        return new MarineMedalsPanelBuiState(groups.Values.ToList(), awardedMedals);
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
        var squadName = _squads.GetSquadName(ent.Owner);

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
}
