using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared._RMC14.Synth;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Marines.Mutiny;

public sealed partial class MutinyRuleSystem
{
    private void OnRecruitAction(Entity<MutineerLeaderComponent> leader, ref MutineerRecruitActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (!_mind.TryGetMind(leader.Owner, out var leaderMindId, out _) ||
            !TryComp(leaderMindId, out MutinyMindComponent? leaderMind) ||
            !leaderMind.IsLeader ||
            !TryComp(leaderMind.Rule, out MutinyRuleComponent? rule) ||
            !GameTicker.IsGameRuleActive(leaderMind.Rule) ||
            rule.Phase != MutinyPhase.Recruiting)
        {
            SendBodyPopup(leader.Owner, "rmc-mutiny-error-not-recruiting", PopupType.SmallCaution);
            return;
        }

        if (!TryGetRecruitTarget(leader.Owner, args.Target, (leaderMind.Rule, rule), out var targetMindId, out var session, out var error))
        {
            SendBodyPopup(leader.Owner, error, PopupType.SmallCaution);
            return;
        }

        var invite = new MutineerInviteEui(leaderMindId, targetMindId, leaderMind.Rule, this);
        _pendingInvites[targetMindId] = invite;
        _euis.OpenEui(invite, session);
        SendBodyPopup(leader.Owner, "rmc-mutiny-recruit-sent");
    }

    private void OnBeginAction(Entity<MutineerLeaderComponent> leader, ref MutineerBeginActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (!_mind.TryGetMind(leader.Owner, out var mindId, out _) ||
            !TryComp(mindId, out MutinyMindComponent? mutinyMind) ||
            !mutinyMind.IsLeader ||
            !TryComp(mutinyMind.Rule, out MutinyRuleComponent? rule) ||
            !GameTicker.IsGameRuleActive(mutinyMind.Rule) ||
            rule.Phase != MutinyPhase.Recruiting ||
            !TryComp(leader.Owner, out ActorComponent? actor))
        {
            SendBodyPopup(leader.Owner, "rmc-mutiny-error-not-recruiting", PopupType.SmallCaution);
            return;
        }

        if (_pendingBegins.ContainsKey(mindId))
            return;

        var eui = new MutinyBeginEui(mindId, mutinyMind.Rule, this);
        _pendingBegins[mindId] = eui;
        _euis.OpenEui(eui, actor.PlayerSession);
    }

    internal bool TryAcceptRecruit(
        EntityUid leaderMindId,
        EntityUid targetMindId,
        EntityUid ruleId,
        MutineerInviteEui eui)
    {
        if (!_pendingInvites.TryGetValue(targetMindId, out var pending) ||
            !ReferenceEquals(pending, eui))
        {
            return false;
        }

        _pendingInvites.Remove(targetMindId);
        if (!TryComp(ruleId, out MutinyRuleComponent? rule) ||
            !GameTicker.IsGameRuleActive(ruleId) ||
            rule.Phase != MutinyPhase.Recruiting ||
            !TryComp(leaderMindId, out MutinyMindComponent? leaderMutiny) ||
            !leaderMutiny.IsLeader ||
            leaderMutiny.Rule != ruleId ||
            !TryComp(leaderMindId, out MindComponent? leaderMind) ||
            leaderMind.OwnedEntity is not { } leader ||
            !IsMutinyBody(leader, rule, requireAlive: true) ||
            !HasComp<ActorComponent>(leader) ||
            !TryComp(targetMindId, out MindComponent? targetMind) ||
            targetMind.OwnedEntity is not { } target ||
            !CanAcceptRecruit(target, targetMindId, (ruleId, rule)))
        {
            return false;
        }

        var mutinyMind = EnsureComp<MutinyMindComponent>(targetMindId);
        mutinyMind.Rule = ruleId;
        mutinyMind.AcceptedRecruit = true;
        mutinyMind.Side = null;

        SendMindMessage((targetMindId, targetMind), "rmc-mutiny-recruit-accepted");
        var leaderName = MindName((leaderMindId, leaderMind));
        var targetName = MindName((targetMindId, targetMind));
        _chat.SendAdminAnnouncement(Loc.GetString("rmc-mutiny-admin-recruit-accepted",
            ("target", targetName),
            ("leader", leaderName)));
        _adminLog.Add(LogType.Mind,
            LogImpact.High,
            $"{targetName} accepted a mutiny invitation from {leaderName}.");
        return true;
    }

    internal void OnInviteClosed(EntityUid targetMindId, MutineerInviteEui eui)
    {
        if (_pendingInvites.TryGetValue(targetMindId, out var pending) && ReferenceEquals(pending, eui))
            _pendingInvites.Remove(targetMindId);
    }

    internal bool TryBeginMutiny(EntityUid leaderMindId, EntityUid ruleId, MutinyBeginEui eui)
    {
        if (!_pendingBegins.TryGetValue(leaderMindId, out var pending) ||
            !ReferenceEquals(pending, eui))
        {
            return false;
        }

        _pendingBegins.Remove(leaderMindId);
        if (!TryComp(ruleId, out MutinyRuleComponent? rule) ||
            !GameTicker.IsGameRuleActive(ruleId) ||
            rule.Phase != MutinyPhase.Recruiting ||
            !TryComp(leaderMindId, out MutinyMindComponent? leaderMutiny) ||
            !leaderMutiny.IsLeader ||
            leaderMutiny.Rule != ruleId ||
            !TryComp(leaderMindId, out MindComponent? leaderMind) ||
            leaderMind.OwnedEntity is not { } leader ||
            !IsMutinyBody(leader, rule, requireAlive: true) ||
            !HasComp<ActorComponent>(leader))
        {
            return false;
        }

        return BeginMutiny((leaderMindId, leaderMutiny, leaderMind), (ruleId, rule));
    }

    private bool BeginMutiny(
        Entity<MutinyMindComponent, MindComponent> leader,
        Entity<MutinyRuleComponent> rule)
    {
        if (rule.Comp.Phase != MutinyPhase.Recruiting ||
            !GameTicker.IsGameRuleActive(rule.Owner) ||
            !leader.Comp1.IsLeader ||
            leader.Comp1.Rule != rule.Owner)
        {
            return false;
        }

        rule.Comp.Phase = MutinyPhase.Active;
        CloseRecruitingEuis();

        var minds = EntityQueryEnumerator<MutinyMindComponent, MindComponent>();
        while (minds.MoveNext(out var mindId, out var mutinyMind, out var mind))
        {
            if (mutinyMind.Rule != rule.Owner || (!mutinyMind.IsLeader && !mutinyMind.AcceptedRecruit))
                continue;

            SetSideInternal((mindId, mutinyMind, mind), rule, MutinySide.Mutineer);
        }

        var players = EntityQueryEnumerator<ActorComponent, MindContainerComponent>();
        while (players.MoveNext(out var body, out _, out _))
        {
            ClassifyBody(body, rule);
        }

        _marineAnnounce.AnnounceToMarines(Loc.GetString("rmc-mutiny-announcement"));
        if (_alertLevel.Get() is not { } level || level < RMCAlertLevels.Red)
            _alertLevel.Set(RMCAlertLevels.Red, leader.Comp2.OwnedEntity, playSound: false, sendAnnouncement: false);

        var leaderName = MindName((leader.Owner, leader.Comp2));
        _chat.SendAdminAnnouncement(Loc.GetString("rmc-mutiny-admin-begun", ("leader", leaderName)));
        _adminLog.Add(LogType.Mind, LogImpact.High, $"{leaderName} began the mutiny.");
        return true;
    }

    internal void OnBeginClosed(EntityUid leaderMindId, MutinyBeginEui eui)
    {
        if (_pendingBegins.TryGetValue(leaderMindId, out var pending) && ReferenceEquals(pending, eui))
            _pendingBegins.Remove(leaderMindId);
    }

    internal bool TryChooseSide(
        EntityUid mindId,
        EntityUid ruleId,
        MutinySide side,
        MutinySideEui eui)
    {
        if (!_pendingSideChoices.TryGetValue(mindId, out var pending) ||
            !ReferenceEquals(pending, eui) ||
            !Enum.IsDefined(side) ||
            !TryComp(ruleId, out MutinyRuleComponent? rule) ||
            !GameTicker.IsGameRuleActive(ruleId) ||
            rule.Phase != MutinyPhase.Active ||
            !TryComp(mindId, out MindComponent? mind) ||
            !TryComp(mindId, out MutinyMindComponent? mutinyMind) ||
            mutinyMind.Rule != ruleId ||
            mutinyMind.Side != null)
        {
            return false;
        }

        if (side == MutinySide.Mutineer)
        {
            if (mind.OwnedEntity is not { } body || !CanJoinMutineers(body))
                return false;
        }

        _pendingSideChoices.Remove(mindId);
        return SetSideInternal((mindId, mutinyMind, mind), (ruleId, rule), side);
    }

    internal void OnSideChoiceClosed(EntityUid mindId, MutinySideEui eui)
    {
        if (_pendingSideChoices.TryGetValue(mindId, out var pending) && ReferenceEquals(pending, eui))
            _pendingSideChoices.Remove(mindId);
    }

    private void ResolveSideChoiceTimeout(EntityUid mindId, MutinySideEui eui)
    {
        if (_pendingSideChoices.TryGetValue(mindId, out var pending) && ReferenceEquals(pending, eui))
            eui.ResolveDefault();
    }

    private bool TryGetRecruitTarget(
        EntityUid leader,
        EntityUid target,
        Entity<MutinyRuleComponent> rule,
        out EntityUid targetMindId,
        out ICommonSession session,
        out string error)
    {
        targetMindId = default;
        session = default!;
        error = "rmc-mutiny-error-invalid-recruit";

        if (leader == target ||
            !_mind.TryGetMind(target, out targetMindId, out _) ||
            !TryComp(target, out ActorComponent? actor) ||
            !CanAcceptRecruit(target, targetMindId, rule))
        {
            return false;
        }

        session = actor.PlayerSession;
        return true;
    }

    private bool CanAcceptRecruit(EntityUid target, EntityUid targetMindId, Entity<MutinyRuleComponent> rule)
    {
        if (!IsMutinyBody(target, rule.Comp, requireAlive: true) ||
            !HasComp<MutinyEligibleComponent>(target) ||
            !CanJoinMutineers(target) ||
            _pendingInvites.ContainsKey(targetMindId))
        {
            return false;
        }

        return !TryComp(targetMindId, out MutinyMindComponent? mutinyMind) ||
               mutinyMind.Rule != rule.Owner ||
               !mutinyMind.IsLeader && !mutinyMind.AcceptedRecruit && mutinyMind.Side == null;
    }

    private bool IsForcedLoyalist(EntityUid body)
    {
        return HasComp<MutinyForcedLoyalistComponent>(body);
    }

    private bool CanJoinMutineers(EntityUid body)
    {
        return !HasComp<SynthComponent>(body) &&
               !HasComp<MutinyForcedLoyalistComponent>(body) &&
               !HasComp<MutinyLoyalistOrNeutralComponent>(body);
    }

    private void ClassifyBody(EntityUid body, Entity<MutinyRuleComponent> rule)
    {
        if (rule.Comp.Phase != MutinyPhase.Active ||
            !IsMutinyBody(body, rule.Comp, requireAlive: true) ||
            !TryComp(body, out ActorComponent? actor) ||
            !_mind.TryGetMind(body, out var mindId, out var mind))
        {
            return;
        }

        var mutinyMind = EnsureComp<MutinyMindComponent>(mindId);
        if (mutinyMind.Rule.IsValid() && mutinyMind.Rule != rule.Owner)
            return;

        mutinyMind.Rule = rule.Owner;

        if (mutinyMind.Side is not null)
        {
            ProjectMindToBody((mindId, mutinyMind, mind), rule);
            return;
        }

        if (mutinyMind.IsLeader || mutinyMind.AcceptedRecruit)
        {
            SetSideInternal((mindId, mutinyMind, mind), rule, MutinySide.Mutineer);
            return;
        }

        if (IsForcedLoyalist(body))
        {
            SetSideInternal((mindId, mutinyMind, mind), rule, MutinySide.Loyalist);
            return;
        }

        if (_pendingSideChoices.ContainsKey(mindId))
            return;

        var choice = new MutinySideEui(mindId, rule.Owner, CanJoinMutineers(body), this);
        _pendingSideChoices[mindId] = choice;
        _euis.OpenEui(choice, actor.PlayerSession);
        Timer.Spawn(rule.Comp.ChoiceDuration, () => ResolveSideChoiceTimeout(mindId, choice));
    }

    private void CloseRecruitingEuis()
    {
        foreach (var invite in _pendingInvites.Values.ToArray())
            invite.Cancel();
        foreach (var begin in _pendingBegins.Values.ToArray())
            begin.Cancel();
    }

    private void SendBodyPopup(EntityUid body, string locId, PopupType popupType = PopupType.Small)
    {
        if (HasComp<ActorComponent>(body))
            _popup.PopupEntity(Loc.GetString(locId), body, body, popupType);
    }
}
