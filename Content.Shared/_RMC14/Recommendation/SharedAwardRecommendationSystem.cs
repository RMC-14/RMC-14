using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Radio;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Recommendation;

public sealed class SharedAwardRecommendationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedMarineControlComputerSystem _control = default!;
    [Dependency] private readonly SharedRankSystem _rank = default!;
    [Dependency] private readonly SquadSystem _squads = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    public int CharacterLimit { get; private set; }
    public int MinCharacterLimit { get; private set; }
    public TimeSpan RecommendationInitialDelay { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCHeadsetComponent, GetVerbsEvent<AlternativeVerb>>(OnGetHeadsetAlternativeVerb);
        SubscribeLocalEvent<RMCAwardRecommendationComponent, RMCAwardRecommendationSelectMarineEvent>(OnSelectMarine);
        SubscribeLocalEvent<RMCAwardRecommendationComponent, RMCAwardRecommendationReasonEvent>(OnSubmitRecommendation);

        Subs.CVar(_config, RMCCVars.RMCRecommendationMaxLength, v => CharacterLimit = v, true);
        Subs.CVar(_config, RMCCVars.RMCCommendationMinLength, v => MinCharacterLimit = v, true);
        Subs.CVar(_config, RMCCVars.RMCDropshipInitialDelayMinutes, v => RecommendationInitialDelay = TimeSpan.FromMinutes(v), true);
    }

    private void OnGetHeadsetAlternativeVerb(Entity<RMCHeadsetComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (HasComp<XenoComponent>(ent.Owner))
            return;

        var user = args.User;
        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("rmc-award-recommendation-verb"),
            Message = Loc.GetString("rmc-award-recommendation-verb-message"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/character.svg.192dpi.png")),
            Act = () => TryOpenRecommendationMenu(user)
        };

        args.Verbs.Add(verb);
    }

    private void TryOpenRecommendationMenu(EntityUid user)
    {
        if (!CanRecommendPopup(user))
            return;

        if (_net.IsClient)
            return;

        OpenRecommendationMenu(user);
    }

    private void OpenRecommendationMenu(EntityUid user)
    {
        if (!TryComp<RMCAwardRecommendationComponent>(user, out var comp))
            return;

        var options = new List<DialogOption>();
        var receivers = EntityQueryEnumerator<CommendationReceiverComponent>();
        while (receivers.MoveNext(out var uid, out var receiver))
        {
            if (uid == user)
                continue;

            if (receiver.LastPlayerId == null)
                continue;

            // Skip players that have already been recommended
            if (comp.RecommendedLastPlayerIds != null && comp.RecommendedLastPlayerIds.Contains(receiver.LastPlayerId))
                continue;

            options.Add(new DialogOption(Name(uid), new RMCAwardRecommendationSelectMarineEvent(GetNetEntity(user), GetNetEntity(uid), receiver.LastPlayerId)));
        }

        var allGibbed = _control.CollectGibbedMarines();
        foreach (var info in allGibbed)
        {
            if (info.LastPlayerId == null)
                continue;

            // Skip players that have already been recommended
            if (comp.RecommendedLastPlayerIds != null && comp.RecommendedLastPlayerIds.Contains(info.LastPlayerId))
                continue;

            options.Add(new DialogOption(info.Name, new RMCAwardRecommendationSelectMarineEvent(GetNetEntity(user), null, info.LastPlayerId)));
        }

        if (options.Count == 0)
        {
            _popup.PopupCursor(Loc.GetString("rmc-award-recommendation-no-targets"), user, PopupType.SmallCaution);
            return;
        }

        options.Sort(static (a, b) => string.Compare(a.Text, b.Text, StringComparison.CurrentCultureIgnoreCase));

        _dialog.OpenOptions(
            user,
            Loc.GetString("rmc-award-recommendation-title"),
            options,
            Loc.GetString("rmc-award-recommendation-prompt"));
    }

    private void OnSelectMarine(Entity<RMCAwardRecommendationComponent> ent, ref RMCAwardRecommendationSelectMarineEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryGetEntity(args.Actor, out var actor) || actor == null)
            return;

        if (actor != ent.Owner)
            return;

        if (!HasComp<ActorComponent>(actor))
            return;

        if (args.LastPlayerId == null)
            return;

        var inputEvent = new RMCAwardRecommendationReasonEvent(args.Actor, args.Marine, args.LastPlayerId);
        _dialog.OpenInput(
            actor.Value,
            Loc.GetString("rmc-award-recommendation-reason"),
            inputEvent,
            true,
            CharacterLimit,
            MinCharacterLimit,
            true);
    }

    private void OnSubmitRecommendation(Entity<RMCAwardRecommendationComponent> ent, ref RMCAwardRecommendationReasonEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryGetEntity(args.Actor, out var actor) || actor == null)
            return;

        if (actor != ent.Owner)
            return;

        if (!HasComp<ActorComponent>(actor))
            return;

        var message = args.Message.Trim();
        if (CharacterLimit > 0 && message.Length > CharacterLimit)
            message = message[..CharacterLimit];

        if (string.IsNullOrWhiteSpace(message))
        {
            _popup.PopupCursor(Loc.GetString("rmc-award-recommendation-empty"), actor.Value, PopupType.SmallCaution);
            return;
        }

        if (!CanRecommendPopup(ent))
            return;

        var recommendedName = string.Empty;
        var recommendedLastPlayerId = args.LastPlayerId;
        string? recommendedRank = null;
        string? recommendedSquad = null;
        string? recommendedJob = null;

        if (args.Marine is { } netMarine && TryGetEntity(netMarine, out var marine) && marine != null)
        {
            if (!TryComp(marine, out CommendationReceiverComponent? receiver) || receiver.LastPlayerId == null)
            {
                _popup.PopupCursor(Loc.GetString("rmc-award-recommendation-invalid"), actor.Value, PopupType.SmallCaution);
                return;
            }

            recommendedName = Name(marine.Value);
            recommendedLastPlayerId ??= receiver.LastPlayerId;

            // Get recommended info at creation time to preserve it even if player leaves body
            recommendedRank = _rank.GetRankString(marine.Value);
            recommendedSquad = _squads.GetSquadName(marine.Value);
            recommendedJob = GetJobName(marine.Value);
        }
        else if (recommendedLastPlayerId != null)
        {
            if (!_control.TryGetGibbedMarineInfo(recommendedLastPlayerId, out var info))
            {
                _popup.PopupCursor(Loc.GetString("rmc-award-recommendation-invalid"), actor.Value, PopupType.SmallCaution);
                return;
            }

            recommendedName = info.Name;
            recommendedRank = info.Rank;
            recommendedSquad = info.Squad;
            recommendedJob = info.Job;
        }
        else
        {
            _popup.PopupCursor(Loc.GetString("rmc-award-recommendation-invalid"), actor.Value, PopupType.SmallCaution);
            return;
        }

        var recommenderLastPlayerId = GetLastPlayerId(actor.Value);
        if (recommenderLastPlayerId == null)
        {
            _popup.PopupCursor(Loc.GetString("rmc-award-recommendation-invalid"), actor.Value, PopupType.SmallCaution);
            return;
        }

        // Get recommender info at creation time to preserve it even if player leaves body
        var recommenderInfo = GetRecommenderInfo(actor.Value);

        var recommendation = new MarineAwardRecommendationInfo
        {
            RecommendedLastPlayerId = recommendedLastPlayerId,
            RecommenderLastPlayerId = recommenderLastPlayerId,
            RecommendedName = recommendedName,
            RecommendedRank = recommendedRank,
            RecommendedSquad = recommendedSquad,
            RecommendedJob = recommendedJob,
            RecommenderName = recommenderInfo.Name,
            RecommenderRank = recommenderInfo.Rank,
            RecommenderSquad = recommenderInfo.Squad,
            RecommenderJob = recommenderInfo.Job,
            Reason = message
        };

        if (!_control.TryAddAwardRecommendation(recommendation))
        {
            _popup.PopupCursor(Loc.GetString("rmc-award-recommendation-no-computer"), actor.Value, PopupType.SmallCaution);
            return;
        }

        var actorComp = Comp<ActorComponent>(actor.Value);
        _adminLog.Add(
            LogType.RMCMedalRecommendation,
            $"{actorComp.PlayerSession.Name} ({Name(actor.Value)}) submitted a medal recommendation for {recommendedName} (lastPlayerId: {recommendedLastPlayerId}) with reason: {message}");

        if (recommendedLastPlayerId != null)
        {
            ent.Comp.RecommendedLastPlayerIds ??= new List<string>();
            ent.Comp.RecommendedLastPlayerIds.Add(recommendedLastPlayerId);
        }
        Dirty(ent);

        string count = ent.Comp.MaxRecommendations > 0 ? $"({(ent.Comp.RecommendedLastPlayerIds?.Count ?? 0)}/{ent.Comp.MaxRecommendations})" : string.Empty;

        _popup.PopupCursor(
            Loc.GetString("rmc-award-recommendation-success", ("name", recommendedName), ("count", count)),
            actor.Value,
            PopupType.Medium
        );
    }

    private bool CanRecommendPopup(EntityUid entity)
    {
        if (!TryComp<ActorComponent>(entity, out var actor) || actor.PlayerSession == null)
            return false;

        if (!TryComp<RMCAwardRecommendationComponent>(entity, out var component))
        {
            if (!_net.IsClient)
                _popup.PopupCursor(Loc.GetString("rmc-award-recommendation-no-authority"), entity, PopupType.SmallCaution);
            return false;
        }

        if (!component.CanRecommend)
        {
            if (!_net.IsClient)
                _popup.PopupCursor(Loc.GetString("rmc-award-recommendation-no-authority"), entity, PopupType.SmallCaution);
            return false;
        }

        var roundDuration = _gameTicker.RoundDuration();
        if (roundDuration < RecommendationInitialDelay)
        {
            var minutesLeft = Math.Max(1, (int)(RecommendationInitialDelay - roundDuration).TotalMinutes);
            var msg = Loc.GetString("rmc-award-recommendation-too-early", ("minutes", minutesLeft));
            if (!_net.IsClient)
                _popup.PopupCursor(msg, entity, PopupType.SmallCaution);
            return false;
        }

        if ((component.RecommendedLastPlayerIds?.Count ?? 0) >= component.MaxRecommendations)
        {
            if (!_net.IsClient)
                _popup.PopupCursor(Loc.GetString("rmc-award-recommendation-out"), entity, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private string? GetLastPlayerId(EntityUid actor)
    {
        if (TryComp<CommendationReceiverComponent>(actor, out var receiver) && receiver.LastPlayerId != null)
            return receiver.LastPlayerId;

        if (TryComp<ActorComponent>(actor, out var actorComp))
            return actorComp.PlayerSession.UserId.UserId.ToString();

        return null;
    }

    private (string Name, string? Rank, string? Squad, string Job) GetRecommenderInfo(EntityUid actor)
    {
        var name = Name(actor);
        var rank = _rank.GetRankString(actor);
        var squad = _squads.GetSquadName(actor);
        var job = GetJobName(actor);

        return (name, rank, squad, job);
    }

    private string GetJobName(EntityUid actor)
    {
        if (TryComp<MindContainerComponent>(actor, out var mind) && mind.Mind is { } mindId)
        {
            if (_jobs.MindTryGetJobName(mindId, out var jobName))
                return jobName;
        }

        return Loc.GetString("generic-unknown-title");
    }

    public void SetCanRecommend(EntityUid uid, bool canRecommend)
    {
        if (!TryComp<RMCAwardRecommendationComponent>(uid, out var comp))
            return;

        comp.CanRecommend = canRecommend;
        Dirty(uid, comp);
    }
}
