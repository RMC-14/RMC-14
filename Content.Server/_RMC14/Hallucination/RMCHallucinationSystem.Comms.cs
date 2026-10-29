using System.Linq;
using Content.Server.Radio.Components;
using Content.Shared._RMC14.Hallucination;
using Content.Shared._RMC14.Hallucination.Events;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Overwatch;
using Content.Shared.Chat;
using Content.Shared.Humanoid;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private const int RadioLines = 11;
    private const int OverwatchLines = 6;
    private const int BodyLines = 8;
    private const int CallLines = 6;

    private void InitializeComms()
    {
        SubscribeLocalEvent<RMCHallucinationFakeRadioComponent, RMCHallucinateEvent>(OnFakeRadio);
        SubscribeLocalEvent<RMCHallucinationFakeAnnouncementComponent, RMCHallucinateEvent>(OnFakeAnnouncement);
        SubscribeLocalEvent<RMCHallucinationBodyMessageComponent, RMCHallucinateEvent>(OnBodyMessage);
        SubscribeLocalEvent<RMCHallucinationCallNameComponent, RMCHallucinateEvent>(OnCallName);
    }

    private void OnCallName(Entity<RMCHallucinationCallNameComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeCallName(args.Victim);
    }

    private void OnFakeRadio(Entity<RMCHallucinationFakeRadioComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeRadio(args.Victim);
    }

    private void OnFakeAnnouncement(Entity<RMCHallucinationFakeAnnouncementComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeAnnouncement(args.Victim);
    }

    private void OnBodyMessage(Entity<RMCHallucinationBodyMessageComponent> ent, ref RMCHallucinateEvent args)
    {
        FakeBodyMessage(args.Victim);
        args.Handled = true;
    }

    private bool FakeRadio(Entity<RMCHallucinatingComponent> ent)
    {
        if (!TryComp(ent, out ActorComponent? actor) ||
            !TryComp(ent, out SquadMemberComponent? member) ||
            member.Squad is not { } squad ||
            !TryComp(squad, out SquadTeamComponent? team) ||
            team.Radio is not { } radio ||
            !_prototypes.TryIndex(radio, out var channel) ||
            !TryComp(ent, out WearingHeadsetComponent? wearing) ||
            !TryComp(wearing.Headset, out ActiveRadioComponent? activeRadio) ||
            !activeRadio.Channels.Contains(radio))
        {
            return false;
        }

        var speakers = team.Members.Where(m => m != ent.Owner && _mobState.IsAlive(m)).ToList();
        if (speakers.Count == 0)
            return false;

        var speaker = _random.Pick(speakers);
        var message = Loc.GetString($"rmc-hallucination-radio-{_random.Next(1, RadioLines + 1)}", ("victim", Name(ent)));
        var verb = _chat.GetSpeechVerb(speaker, message);
        var wrapped = Loc.GetString(verb.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
            ("color", channel.Color),
            ("fontType", verb.FontId),
            ("fontSize", verb.FontSize),
            ("verb", Loc.GetString(_random.Pick(verb.SpeechVerbStrings))),
            ("channel", $"\\[{channel.LocalizedName}\\]"),
            ("name", FormattedMessage.EscapeText(Name(speaker))),
            ("message", FormattedMessage.EscapeText(message)));

        _rmcChat.ChatMessageToOne(ChatChannel.Radio, message, wrapped, default, false, actor.PlayerSession.Channel);

        return true;
    }

    private bool FakeAnnouncement(Entity<RMCHallucinatingComponent> ent)
    {
        if (!TryComp(ent, out SquadMemberComponent? member) || member.Squad is not { } squad)
            return false;

        var netSquad = GetNetEntity(squad);
        var operators = new List<string>();
        var query = EntityQueryEnumerator<OverwatchConsoleComponent>();
        while (query.MoveNext(out var console))
        {
            if (console.Squad == netSquad && console.Operator is { } name)
                operators.Add(name);
        }

        if (operators.Count == 0)
            return false;

        var text = Loc.GetString($"rmc-hallucination-overwatch-{_random.Next(1, OverwatchLines + 1)}", ("victim", Name(ent)));
        var overwatch = Loc.GetString("rmc-overwatch-console-announce-message",
            ("operatorName", _random.Pick(operators)),
            ("message", FormattedMessage.EscapeText(text)));
        _announce.AnnounceSingle(overwatch, ent, SharedMarineAnnounceSystem.DefaultSquadSound);
        return true;
    }

    private bool FakeCallName(Entity<RMCHallucinatingComponent> ent)
    {
        if (PickUnseenSpeaker(ent) is not { } speaker)
            return false;

        var message = Loc.GetString($"rmc-hallucination-call-{_random.Next(1, CallLines + 1)}", ("victim", Name(ent)));
        var verb = _chat.GetSpeechVerb(speaker, message);
        var wrapped = Loc.GetString(verb.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message",
            ("entityName", FormattedMessage.EscapeText(Name(speaker))),
            ("verb", Loc.GetString(_random.Pick(verb.SpeechVerbStrings))),
            ("fontType", verb.FontId),
            ("fontSize", verb.FontSize),
            ("message", FormattedMessage.EscapeText(message)));

        SendToVictim(ent, new RMCHallucinationSpeechEvent(-1, false, ChatChannel.Local, message, wrapped));
        return true;
    }

    private EntityUid? PickUnseenSpeaker(Entity<RMCHallucinatingComponent> ent)
    {
        var speakers = new List<EntityUid>();
        if (TryComp(ent, out SquadMemberComponent? member) &&
            TryComp(member.Squad, out SquadTeamComponent? team))
        {
            foreach (var squadmate in team.Members)
            {
                if (IsUnseenSpeaker(ent, squadmate))
                    speakers.Add(squadmate);
            }
        }

        if (speakers.Count == 0)
        {
            var query = EntityQueryEnumerator<HumanoidAppearanceComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                if (IsUnseenSpeaker(ent, uid))
                    speakers.Add(uid);
            }
        }

        return speakers.Count == 0 ? null : _random.Pick(speakers);
    }

    private bool IsUnseenSpeaker(Entity<RMCHallucinatingComponent> ent, EntityUid speaker)
    {
        return speaker != ent.Owner &&
               _mobState.IsAlive(speaker) &&
               Transform(speaker).MapID == Transform(ent).MapID &&
               !_examine.InRangeUnOccluded(ent, speaker, ent.Comp.ViewRange + 1);
    }

    private void FakeBodyMessage(Entity<RMCHallucinatingComponent> ent)
    {
        var message = Loc.GetString($"rmc-hallucination-body-{_random.Next(1, BodyLines + 1)}");
        _rmcChat.ChatMessageToOne($"[color=red]{message}[/color]", ent);
    }
}
