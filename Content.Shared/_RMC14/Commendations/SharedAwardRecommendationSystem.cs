using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Radio;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Commendations;

public sealed class SharedAwardRecommendationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRankSystem _rank = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;

    public int CharacterLimit { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCHeadsetComponent, GetVerbsEvent<InteractionVerb>>(OnGetHeadsetInteractionVerbs);
        SubscribeLocalEvent<RMCAwardRecommendationComponent, RMCAwardRecommendationSelectMarineEvent>(OnSelectMarine);
        SubscribeLocalEvent<RMCAwardRecommendationComponent, RMCAwardRecommendationReasonEvent>(OnSubmitRecommendation);

        Subs.CVar(_config, RMCCVars.RMCCommendationMaxLength, v => CharacterLimit = v, true);
    }

    private void OnGetHeadsetInteractionVerbs(Entity<RMCHeadsetComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (HasComp<XenoComponent>(ent.Owner))
            return;

        var user = args.User;
        var verb = new InteractionVerb
        {
            Text = Loc.GetString("rmc-award-recommendation-verb"),
            Message = Loc.GetString("rmc-award-recommendation-verb-message"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/star.svg")),
            Act = () => TryOpenRecommendationMenu(user)
        };

        args.Verbs.Add(verb);
    }

    private void TryOpenRecommendationMenu(EntityUid user)
    {
        if (!CanRecommendPopup(user))
            return;

        OpenRecommendationMenu(user);
    }

    private void OpenRecommendationMenu(EntityUid user)
    {
        var options = new List<DialogOption>();
        var receivers = EntityQueryEnumerator<CommendationReceiverComponent>();
        while (receivers.MoveNext(out var uid, out var receiver))
        {
            if (uid == user)
                continue;

            if (receiver.LastPlayerId == null)
                continue;

            options.Add(new DialogOption(Name(uid), new RMCAwardRecommendationSelectMarineEvent(GetNetEntity(user), GetNetEntity(uid))));
        }

        var allGibbed = CollectGibbedMarines();
        foreach (var (playerId, info) in allGibbed)
        {
            options.Add(new DialogOption(info.Name, new RMCAwardRecommendationSelectMarineEvent(GetNetEntity(user), null, playerId)));
        }

        if (options.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-award-recommendation-no-targets"), user, user, PopupType.SmallCaution);
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
        if (!TryGetEntity(args.Actor, out var actor))
            return;

        if (actor != ent.Owner)
            return;

        if (!HasComp<ActorComponent>(actor))
            return;

        var inputEvent = new RMCAwardRecommendationReasonEvent(args.Actor, args.Marine, args.LastPlayerId);
        _dialog.OpenInput(
            actor.Value,
            Loc.GetString("rmc-award-recommendation-reason"),
            inputEvent,
            true,
            CharacterLimit);
    }

    private void OnSubmitRecommendation(Entity<RMCAwardRecommendationComponent> ent, ref RMCAwardRecommendationReasonEvent args)
    {
        if (!TryGetEntity(args.Actor, out var actor) && actor == null)
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
            _popup.PopupClient(Loc.GetString("rmc-award-recommendation-empty"), ent.Owner, actor, PopupType.SmallCaution);
            return;
        }

        if (!CanRecommendPopup(ent))
            return;

        var recommendedName = string.Empty;
        var recommendedLastPlayerId = args.LastPlayerId;

        if (args.Marine is { } netMarine && TryGetEntity(netMarine, out var marine) && marine != null)
        {
            if (!TryComp(marine, out CommendationReceiverComponent? receiver) || receiver.LastPlayerId == null)
            {
                _popup.PopupClient(Loc.GetString("rmc-award-recommendation-invalid"), ent.Owner, actor, PopupType.SmallCaution);
                return;
            }

            recommendedName = Name(marine.Value);
            recommendedLastPlayerId ??= receiver.LastPlayerId;
        }
        else if (recommendedLastPlayerId != null)
        {
            if (!TryGetGibbedMarineInfo(recommendedLastPlayerId, out var info))
            {
                _popup.PopupClient(Loc.GetString("rmc-award-recommendation-invalid"), ent.Owner, actor, PopupType.SmallCaution);
                return;
            }

            recommendedName = info.Name;
        }
        else
        {
            _popup.PopupClient(Loc.GetString("rmc-award-recommendation-invalid"), ent.Owner, actor, PopupType.SmallCaution);
            return;
        }

        var recommenderName = Name(actor.Value);
        var recommenderRank = _rank.GetRankString(actor.Value) ?? Loc.GetString("rmc-award-recommendation-rank-unknown");
        var recommenderJob = GetJobName(actor.Value);
        var recommenderLastPlayerId = GetLastPlayerId(actor.Value);
        if (recommenderLastPlayerId == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-award-recommendation-invalid"), ent.Owner, actor, PopupType.SmallCaution);
            return;
        }

        var added = false;
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var computerId, out var computer))
        {
            computer.AwardRecommendations.Add(new MarineAwardRecommendationInfo
            {
                RecommenderName = recommenderName,
                RecommenderRank = recommenderRank,
                RecommenderJob = recommenderJob,
                RecommendedName = recommendedName,
                RecommendedLastPlayerId = recommendedLastPlayerId,
                RecommenderLastPlayerId = recommenderLastPlayerId,
                Reason = message
            });
            Dirty(computerId, computer);
            added = true;
        }

        if (!added)
        {
            _popup.PopupClient(Loc.GetString("rmc-award-recommendation-no-computer"), ent.Owner, actor, PopupType.SmallCaution);
            return;
        }

        ent.Comp.RecommendationsGiven++;
        Dirty(ent);

        string count = (ent.Comp.MaxRecommendations > 0 && !ent.Comp.CanAlwaysRecommend) ? $"({ent.Comp.RecommendationsGiven}/{ent.Comp.MaxRecommendations})" : string.Empty;

        _popup.PopupClient(
            Loc.GetString("rmc-award-recommendation-success", ("name", recommendedName), ("count", count)),
            ent.Owner,
            actor,
            PopupType.Medium
        );
    }

    private Dictionary<string, GibbedMarineInfo> CollectGibbedMarines()
    {
        var result = new Dictionary<string, GibbedMarineInfo>();
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out _, out var computer))
        {
            foreach (var (playerId, info) in computer.GibbedMarines)
            {
                if (info.LastPlayerId == null)
                    continue;

                result[playerId] = info;
            }
        }

        return result;
    }

    private bool TryGetGibbedMarineInfo(string playerId, out GibbedMarineInfo info)
    {
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out _, out var computer))
        {
            if (computer.GibbedMarines.TryGetValue(playerId, out info!))
                return true;
        }

        info = default!;
        return false;
    }

    private bool CanRecommendPopup(EntityUid entity)
    {
        if (!TryComp<ActorComponent>(entity, out var actor) || actor.PlayerSession == null)
            return false;

        if (!TryComp<RMCAwardRecommendationComponent>(entity, out var component))
        {
            _popup.PopupClient(Loc.GetString("rmc-award-recommendation-no-authority"), entity, entity, PopupType.SmallCaution);
            return false;
        }

        if (component.CanAlwaysRecommend)
            return true;

        if (!component.CanRecommend)
        {
            _popup.PopupClient(Loc.GetString("rmc-award-recommendation-no-authority"), entity, entity, PopupType.SmallCaution);
            return false;
        }

        if (component.MaxRecommendations > 0 && component.RecommendationsGiven >= component.MaxRecommendations)
        {
            _popup.PopupClient(Loc.GetString("rmc-award-recommendation-out"), entity, entity, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private string GetJobName(EntityUid actor)
    {
        if (TryComp<MindContainerComponent>(actor, out var mind) && mind.Mind is { } mindId)
            return _jobs.MindTryGetJobName(mindId);

        return Loc.GetString("generic-unknown-title");
    }

    private string? GetLastPlayerId(EntityUid actor)
    {
        if (TryComp<CommendationReceiverComponent>(actor, out var receiver) && receiver.LastPlayerId != null)
            return receiver.LastPlayerId;

        if (TryComp<ActorComponent>(actor, out var actorComp))
            return actorComp.PlayerSession.UserId.UserId.ToString();

        return null;
    }
}
