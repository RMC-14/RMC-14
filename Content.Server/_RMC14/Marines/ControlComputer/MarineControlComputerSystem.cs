using System.Linq;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Examine;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Survivor;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared.Body.Events;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Marines.ControlComputer;

public sealed class MarineControlComputerSystem : SharedMarineControlComputerSystem
{
    [Dependency] private readonly SharedRankSystem _rank = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SquadSystem _squads = default!;
    [Dependency] private readonly SharedCommendationSystem _commendation = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MarineComponent, BeingGibbedEvent>(OnMarineGibbed);

        Subs.BuiEvents<MarineControlComputerComponent>(MarineControlComputerUi.MedalsPanel,
            subs =>
            {
                subs.Event<MarineControlComputerPrintCommendationMsg>(OnPrintCommendation);
            });
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

        var awardedLastPlayerIds = _commendation.GetRoundCommendationEntries()
            .Where(e => e.Commendation.Type == CommendationType.Medal && e.ReceiverLastPlayerId != null)
            .Select(e => e.ReceiverLastPlayerId!)
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

        return new MarineMedalsPanelBuiState(
            groups.Values.ToList(),
            awardedMedals,
            ent.Comp.CanPrintCommendations,
            ent.Comp.PrintedCommendationIds);
    }

    private void OnPrintCommendation(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerPrintCommendationMsg args)
    {
        if (_net.IsClient)
            return;

        // Check if printing is supported
        if (!ent.Comp.CanPrintCommendations)
            return;

        // Check if already printed
        if (ent.Comp.PrintedCommendationIds.Contains(args.CommendationId))
            return;

        // Find the commendation entry
        var entries = _commendation.GetRoundCommendationEntries()
            .Where(e => e.Commendation.Type == CommendationType.Medal)
            .ToList();

        RoundCommendationEntry? targetEntry = null;
        foreach (var entry in entries)
        {
            if (GetCommendationId(entry) == args.CommendationId)
            {
                targetEntry = entry;
                break;
            }
        }

        if (targetEntry == null)
            return;

        // Check if we have a prototype ID
        var prototypeId = targetEntry.Value.CommendationPrototypeId;
        if (prototypeId == null)
            return;

        // Update UI state immediately to disable button
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var uid, out var comp))
        {
            if (!comp.PrintedCommendationIds.Contains(args.CommendationId))
            {
                comp.PrintedCommendationIds.Add(args.CommendationId);
                Dirty(uid, comp);
            }

            if (_ui.IsUiOpen(uid, MarineControlComputerUi.MedalsPanel))
            {
                var state = BuildMedalsPanelState(new Entity<MarineControlComputerComponent>(uid, comp), null);
                _ui.SetUiState(uid, MarineControlComputerUi.MedalsPanel, state);
            }
        }

        // Store data for delayed spawn
        var coordinates = Transform(ent.Owner).Coordinates;
        var commendationId = args.CommendationId;
        var entityUid = ent.Owner;
        var receiverEntity = targetEntry.Value.ReceiverEntity;
        var receiver = targetEntry.Value.Commendation.Receiver;
        var giver = targetEntry.Value.Commendation.Giver;
        var text = targetEntry.Value.Commendation.Text;

        // Play print sound
        if (ent.Comp.PrintCommendationSound != null)
        {
            _audio.PlayPvs(ent.Comp.PrintCommendationSound, ent.Owner);
        }

        // Spawn medal after delay
        Timer.Spawn(ent.Comp.PrintCommendationDelay, () =>
        {
            if (!Exists(entityUid))
                return;

            if (!TryComp<MarineControlComputerComponent>(entityUid, out var computer))
                return;

            // Double-check that it's still marked as printed (in case of component removal)
            if (!computer.PrintedCommendationIds.Contains(commendationId))
                return;

            var medal = Spawn(prototypeId.Value, coordinates);

            // Set owner in accessory component if it exists
            if (receiverEntity != null && TryComp<UniformAccessoryComponent>(medal, out var accessory))
            {
                accessory.User = receiverEntity.Value;
                Dirty(medal, accessory);
            }

            if (!HasComp<RMCGenericExamineComponent>(medal))
            {
                var examine = EnsureComp<RMCGenericExamineComponent>(medal);
                string messageHeader = Loc.GetString("rmc-commendation-examine-1");
                string messageDesc = Loc.GetString("rmc-commendation-examine-2",
                    ("receiver", receiver),
                    ("giver", giver),
                    ("text", text));
                examine.Message = $"{messageHeader}\n{messageDesc}";
                examine.DisplayMode = RMCExamineDisplayMode.DetailedVerb;
                examine.DetailedVerbConfig = new RMCDetailedVerbConfig 
                {
                    HoverMessageId = "rmc-commendation-examine-hover",
                    Title = "rmc-commendation-examine-title"
                };
                examine.Blacklist = new EntityWhitelist { Components = ["Xeno"] };
                Dirty(medal, examine);
            }
        });
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
