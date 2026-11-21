using System.Linq;
using Content.Server._RMC14.Marines;
using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Marines.Mutiny;

public sealed partial class MutinyRuleSystem : GameRuleSystem<MutinyRuleComponent>
{
    private static readonly EntProtoId MutinyRulePrototype = "RMCMutiny";
    private static readonly ProtoId<NpcFactionPrototype> DefaultFaction = "UNMC";

    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly RMCAlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly EuiManager _euis = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    private readonly Dictionary<EntityUid, MutineerInviteEui> _pendingInvites = new();
    private readonly Dictionary<EntityUid, MutinyBeginEui> _pendingBegins = new();
    private readonly Dictionary<EntityUid, MutinySideEui> _pendingSideChoices = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);

        SubscribeLocalEvent<MutineerLeaderComponent, ComponentShutdown>(OnLeaderShutdown);
        SubscribeLocalEvent<MutineerLeaderComponent, MutineerRecruitActionEvent>(OnRecruitAction);
        SubscribeLocalEvent<MutineerLeaderComponent, MutineerBeginActionEvent>(OnBeginAction);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<MindContainerComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    protected override void Started(
        EntityUid uid,
        MutinyRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var rules = QueryActiveRules();
        while (rules.MoveNext(out var otherUid, out _, out _, out _))
        {
            if (otherUid == uid)
                continue;

            GameTicker.EndGameRule(uid, gameRule);
            return;
        }
    }

    protected override void Ended(
        EntityUid uid,
        MutinyRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
        CleanupRule(uid);
    }

    public bool TryGetActiveMutiny(out Entity<MutinyRuleComponent> mutiny)
    {
        var rules = QueryActiveRules();
        while (rules.MoveNext(out var uid, out _, out var component, out _))
        {
            mutiny = (uid, component);
            return true;
        }

        mutiny = default;
        return false;
    }

    public bool TryAddLeader(EntityUid body, out string? error)
    {
        error = null;
        if (!_mind.TryGetMind(body, out var mindId, out var mind) ||
            !IsDefaultMutinyBody(body, requireAlive: true))
        {
            error = Loc.GetString("rmc-mutiny-error-invalid-member");
            return false;
        }

        if (!TryEnsureMutiny(out var rule))
        {
            error = Loc.GetString("rmc-mutiny-error-rule");
            return false;
        }

        var mutinyMind = EnsureComp<MutinyMindComponent>(mindId);
        if (mutinyMind.Rule.IsValid() && mutinyMind.Rule != rule.Owner)
        {
            error = Loc.GetString("rmc-mutiny-error-other-rule");
            return false;
        }

        var wasLeader = mutinyMind.IsLeader;
        mutinyMind.Rule = rule.Owner;
        mutinyMind.IsLeader = true;
        mutinyMind.AcceptedRecruit = false;

        if (rule.Comp.Phase == MutinyPhase.Active)
            SetSideInternal((mindId, mutinyMind, mind), rule, MutinySide.Mutineer);
        else
            ProjectMindToBody((mindId, mutinyMind, mind), rule);

        if (!wasLeader)
        {
            SendMindMessage((mindId, mind), "mutineer-leader-status-added");
            _chat.SendAdminAnnouncement(Loc.GetString("rmc-mutiny-admin-leader-added",
                ("player", MindName((mindId, mind)))));
            _adminLog.Add(LogType.Mind,
                LogImpact.High,
                $"{MindName((mindId, mind))} was made a mutiny leader.");
        }

        return true;
    }

    public bool TryRemoveLeader(EntityUid body, out string? error)
    {
        error = null;
        if (!_mind.TryGetMind(body, out var mindId, out var mind) ||
            !TryComp(mindId, out MutinyMindComponent? mutinyMind) ||
            !mutinyMind.IsLeader)
        {
            error = Loc.GetString("rmc-mutiny-error-not-leader");
            return false;
        }

        mutinyMind.IsLeader = false;

        if (TryComp(mutinyMind.Rule, out MutinyRuleComponent? rule) &&
            GameTicker.IsGameRuleActive(mutinyMind.Rule))
        {
            ProjectMindToBody((mindId, mutinyMind, mind), (mutinyMind.Rule, rule));
        }
        else if (mind.OwnedEntity is { } owned)
        {
            RemoveProjection(owned, mutinyMind.Rule);
        }

        SendMindPopup((mindId, mind), "mutineer-leader-status-removed");
        _chat.SendAdminAnnouncement(Loc.GetString("rmc-mutiny-admin-leader-removed",
            ("player", MindName((mindId, mind)))));
        _adminLog.Add(LogType.Mind,
            LogImpact.High,
            $"{MindName((mindId, mind))} is no longer a mutiny leader.");

        if (!mutinyMind.AcceptedRecruit && mutinyMind.Side == null)
            RemComp<MutinyMindComponent>(mindId);

        return true;
    }

    public bool TryMakeMutineer(EntityUid body, out string? error)
    {
        error = null;
        if (!TryGetActiveMutiny(out var rule))
        {
            error = Loc.GetString("rmc-mutiny-error-no-rule");
            return false;
        }

        if (!_mind.TryGetMind(body, out var mindId, out var mind) ||
            !IsMutinyBody(body, rule.Comp, requireAlive: false))
        {
            error = Loc.GetString("rmc-mutiny-error-invalid-member");
            return false;
        }

        var mutinyMind = EnsureComp<MutinyMindComponent>(mindId);
        if (mutinyMind.Rule.IsValid() && mutinyMind.Rule != rule.Owner)
        {
            error = Loc.GetString("rmc-mutiny-error-other-rule");
            return false;
        }

        mutinyMind.Rule = rule.Owner;
        if (rule.Comp.Phase == MutinyPhase.Recruiting)
        {
            mutinyMind.AcceptedRecruit = true;
            SendMindMessage((mindId, mind), "rmc-mutiny-recruit-accepted");
            return true;
        }

        return SetSideInternal((mindId, mutinyMind, mind), rule, MutinySide.Mutineer);
    }

    public bool TrySetSide(EntityUid body, MutinySide side, out string? error)
    {
        error = null;
        if (!TryGetActiveMutiny(out var rule) || rule.Comp.Phase != MutinyPhase.Active)
        {
            error = Loc.GetString("rmc-mutiny-error-not-active");
            return false;
        }

        if (!_mind.TryGetMind(body, out var mindId, out var mind) ||
            !IsMutinyBody(body, rule.Comp, requireAlive: false))
        {
            error = Loc.GetString("rmc-mutiny-error-invalid-member");
            return false;
        }

        var mutinyMind = EnsureComp<MutinyMindComponent>(mindId);
        if (mutinyMind.Rule.IsValid() && mutinyMind.Rule != rule.Owner)
        {
            error = Loc.GetString("rmc-mutiny-error-other-rule");
            return false;
        }

        if (mutinyMind.IsLeader && side != MutinySide.Mutineer)
        {
            error = Loc.GetString("rmc-mutiny-error-leader-side");
            return false;
        }

        mutinyMind.Rule = rule.Owner;
        return SetSideInternal((mindId, mutinyMind, mind), rule, side);
    }

    public bool TryBeginMutiny(EntityUid body, out string? error)
    {
        error = null;
        if (!_mind.TryGetMind(body, out var mindId, out var mind) ||
            mind.OwnedEntity != body ||
            !IsDefaultMutinyBody(body, requireAlive: true) ||
            !TryComp(mindId, out MutinyMindComponent? mutinyMind) ||
            !mutinyMind.IsLeader ||
            !TryComp(mutinyMind.Rule, out MutinyRuleComponent? rule) ||
            !GameTicker.IsGameRuleActive(mutinyMind.Rule) ||
            rule.Phase != MutinyPhase.Recruiting)
        {
            error = Loc.GetString("rmc-mutiny-error-not-recruiting");
            return false;
        }

        return BeginMutiny((mindId, mutinyMind, mind), (mutinyMind.Rule, rule));
    }

    public bool TryRemoveMutineer(EntityUid body, out string? error)
    {
        error = null;
        if (!_mind.TryGetMind(body, out var mindId, out var mind) ||
            !TryComp(mindId, out MutinyMindComponent? mutinyMind) ||
            !TryComp(mutinyMind.Rule, out MutinyRuleComponent? rule) ||
            !GameTicker.IsGameRuleActive(mutinyMind.Rule))
        {
            error = Loc.GetString("rmc-mutiny-error-not-mutineer");
            return false;
        }

        if (rule.Phase == MutinyPhase.Recruiting)
        {
            if (!mutinyMind.AcceptedRecruit)
            {
                error = Loc.GetString("rmc-mutiny-error-not-mutineer");
                return false;
            }

            mutinyMind.AcceptedRecruit = false;
            if (!mutinyMind.IsLeader && mutinyMind.Side == null)
                RemComp<MutinyMindComponent>(mindId);
            return true;
        }

        if (mutinyMind.Side != MutinySide.Mutineer)
        {
            error = Loc.GetString("rmc-mutiny-error-not-mutineer");
            return false;
        }

        if (mutinyMind.IsLeader)
        {
            error = Loc.GetString("rmc-mutiny-error-remove-leader-first");
            return false;
        }

        return SetSideInternal((mindId, mutinyMind, mind), (mutinyMind.Rule, rule), MutinySide.NonCombatant);
    }

    public bool EndMutiny(out string? error)
    {
        error = null;
        if (!TryGetActiveMutiny(out var rule))
        {
            error = Loc.GetString("rmc-mutiny-error-no-rule");
            return false;
        }

        return GameTicker.EndGameRule(rule.Owner);
    }

    public bool IsMutineer(EntityUid body)
    {
        return TryComp(body, out MutinyParticipantComponent? participant) &&
               participant.Side == MutinySide.Mutineer;
    }

    public IEnumerable<string> GetStatusLines()
    {
        if (!TryGetActiveMutiny(out var rule))
        {
            yield return Loc.GetString("rmc-mutiny-command-list-none");
            yield break;
        }

        yield return Loc.GetString("rmc-mutiny-command-list-header",
            ("phase", Loc.GetString(rule.Comp.Phase switch
            {
                MutinyPhase.Recruiting => "rmc-mutiny-phase-recruiting",
                MutinyPhase.Active => "rmc-mutiny-phase-active",
                _ => throw new ArgumentOutOfRangeException(),
            })));

        var query = EntityQueryEnumerator<MutinyMindComponent, MindComponent>();
        while (query.MoveNext(out _, out var mutinyMind, out var mind))
        {
            if (mutinyMind.Rule != rule.Owner)
                continue;

            var state = mutinyMind.Side is { } side
                ? Loc.GetString(side switch
                {
                    MutinySide.Mutineer => "rmc-mutiny-side-name-mutineer",
                    MutinySide.Loyalist => "rmc-mutiny-side-name-loyalist",
                    MutinySide.NonCombatant => "rmc-mutiny-side-name-noncombatant",
                    _ => throw new ArgumentOutOfRangeException(),
                })
                : mutinyMind.AcceptedRecruit
                    ? Loc.GetString("rmc-mutiny-command-list-recruit")
                    : Loc.GetString("rmc-mutiny-command-list-unassigned");
            var leader = mutinyMind.IsLeader
                ? Loc.GetString("rmc-mutiny-command-list-leader")
                : string.Empty;
            yield return Loc.GetString("rmc-mutiny-command-list-entry",
                ("player", mind.CharacterName ?? ToPrettyString(rule.Owner)),
                ("state", state),
                ("leader", leader));
        }
    }

    private bool TryEnsureMutiny(out Entity<MutinyRuleComponent> mutiny)
    {
        if (TryGetActiveMutiny(out mutiny))
            return true;

        var ruleUid = GameTicker.AddGameRule(MutinyRulePrototype);
        if (!GameTicker.StartGameRule(ruleUid) ||
            !TryComp(ruleUid, out MutinyRuleComponent? rule))
        {
            mutiny = default;
            return false;
        }

        mutiny = (ruleUid, rule);
        return true;
    }

    private bool SetSideInternal(
        Entity<MutinyMindComponent, MindComponent> mind,
        Entity<MutinyRuleComponent> rule,
        MutinySide side)
    {
        if (rule.Comp.Phase != MutinyPhase.Active ||
            !GameTicker.IsGameRuleActive(rule.Owner) ||
            mind.Comp1.Rule != rule.Owner)
        {
            return false;
        }

        if (mind.Comp1.IsLeader && side != MutinySide.Mutineer)
            return false;

        var changed = mind.Comp1.Side != side;
        mind.Comp1.Side = side;
        mind.Comp1.AcceptedRecruit = false;
        ProjectMindToBody(mind, rule);

        if (!changed)
            return true;

        var message = side switch
        {
            MutinySide.Mutineer => "mutineer-status-added",
            MutinySide.Loyalist => "rmc-mutiny-loyalist-status-added",
            MutinySide.NonCombatant => "rmc-mutiny-noncombatant-status-added",
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
        };
        SendMindMessage((mind.Owner, mind.Comp2), message);
        _adminLog.Add(LogType.Mind,
            LogImpact.Medium,
            $"{MindName((mind.Owner, mind.Comp2))} joined the mutiny as {side}.");
        return true;
    }

    private void ProjectMindToBody(
        Entity<MutinyMindComponent, MindComponent> mind,
        Entity<MutinyRuleComponent> rule)
    {
        if (mind.Comp2.OwnedEntity is not { } body)
            return;

        RemoveProjection(body, rule.Owner);
        if (!IsMutinyBody(body, rule.Comp, requireAlive: false))
            return;

        if (mind.Comp1.Side is { } side)
        {
            var participant = EnsureComp<MutinyParticipantComponent>(body);
            participant.Rule = rule.Owner;
            participant.Side = side;
            participant.IffFaction = rule.Comp.IffFaction;
            Dirty(body, participant);
        }

        if (!mind.Comp1.IsLeader)
            return;

        var leader = EnsureComp<MutineerLeaderComponent>(body);
        leader.Rule = rule.Owner;
        leader.Active = rule.Comp.Phase == MutinyPhase.Active;

        if (leader.Active)
        {
            RemoveLeaderActions(body, leader);
        }
        else
        {
            _actions.AddAction(body, ref leader.RecruitActionEntity, leader.RecruitAction);
            _actions.AddAction(body, ref leader.BeginActionEntity, leader.BeginAction);
        }

        Dirty(body, leader);
    }

    private void RemoveProjection(EntityUid body, EntityUid rule)
    {
        if (TryComp(body, out MutineerLeaderComponent? leader) && leader.Rule == rule)
        {
            RemoveLeaderActions(body, leader);
            RemComp<MutineerLeaderComponent>(body);
        }

        if (TryComp(body, out MutinyParticipantComponent? participant) && participant.Rule == rule)
            RemComp<MutinyParticipantComponent>(body);
    }

    private void RemoveLeaderActions(EntityUid body, MutineerLeaderComponent leader)
    {
        _actions.RemoveAction(body, leader.RecruitActionEntity);
        _actions.RemoveAction(body, leader.BeginActionEntity);
        leader.RecruitActionEntity = null;
        leader.BeginActionEntity = null;
    }

    private void OnLeaderShutdown(Entity<MutineerLeaderComponent> leader, ref ComponentShutdown args)
    {
        RemoveLeaderActions(leader.Owner, leader.Comp);
    }

    private bool IsDefaultMutinyBody(EntityUid body, bool requireAlive)
    {
        return HasComp<MarineComponent>(body) &&
               HasComp<HumanoidAppearanceComponent>(body) &&
               _npcFaction.IsMember(body, DefaultFaction) &&
               (!requireAlive || _mobState.IsAlive(body));
    }

    private bool IsMutinyBody(EntityUid body, MutinyRuleComponent rule, bool requireAlive)
    {
        return HasComp<MarineComponent>(body) &&
               HasComp<HumanoidAppearanceComponent>(body) &&
               _npcFaction.IsMember(body, rule.Faction) &&
               (!requireAlive || _mobState.IsAlive(body));
    }

    private void SendMindMessage(Entity<MindComponent> mind, string locId)
    {
        if (mind.Comp.UserId is { } userId && _players.TryGetSessionById(userId, out var session))
        {
            var message = Loc.GetString(locId);
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chat.ChatMessageToOne(
                ChatChannel.Server,
                FormattedMessage.RemoveMarkupOrThrow(message),
                wrappedMessage,
                default,
                false,
                session.Channel,
                recordReplay: true);
        }
    }

    private void SendMindPopup(Entity<MindComponent> mind, string locId, PopupType popupType = PopupType.Small)
    {
        if (mind.Comp.OwnedEntity is { } body)
            SendBodyPopup(body, locId, popupType);
    }

    private string MindName(Entity<MindComponent> mind)
    {
        if (mind.Comp.UserId is { } userId && _players.TryGetSessionById(userId, out var session))
            return $"{session.Name} ({mind.Comp.CharacterName})";

        return mind.Comp.CharacterName ?? ToPrettyString(mind.Owner);
    }

    private void CleanupRule(EntityUid rule)
    {
        foreach (var invite in _pendingInvites.Values.ToArray())
            invite.Cancel();
        foreach (var begin in _pendingBegins.Values.ToArray())
            begin.Cancel();
        foreach (var choice in _pendingSideChoices.Values.ToArray())
            choice.CancelWithoutChoice();

        _pendingInvites.Clear();
        _pendingBegins.Clear();
        _pendingSideChoices.Clear();

        var removeMinds = new List<EntityUid>();
        var minds = EntityQueryEnumerator<MutinyMindComponent, MindComponent>();
        while (minds.MoveNext(out var mindId, out var mutinyMind, out var mind))
        {
            if (mutinyMind.Rule != rule)
                continue;

            if (mind.OwnedEntity is { } body && !TerminatingOrDeleted(body))
                RemoveProjection(body, rule);

            removeMinds.Add(mindId);
        }

        foreach (var mindId in removeMinds)
            RemComp<MutinyMindComponent>(mindId);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _pendingInvites.Clear();
        _pendingBegins.Clear();
        _pendingSideChoices.Clear();
    }
}
