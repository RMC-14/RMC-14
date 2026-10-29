using Content.Shared._RMC14.Hallucination;
using Content.Shared._RMC14.Hallucination.Events;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Speech.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private static readonly ProtoId<EmotePrototype> GaspEmote = "Gasp";

    private const int BodyBegLines = 8;

    private void InitializeBodies()
    {
        SubscribeLocalEvent<RMCHallucinationFakeBodyComponent, RMCHallucinateEvent>(OnFakeBody);
    }

    private void OnFakeBody(Entity<RMCHallucinationFakeBodyComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeBodies(args.Victim);
    }

    private bool FakeBodies(Entity<RMCHallucinatingComponent> ent)
    {
        var free = ent.Comp.MaxFakeBodies - ent.Comp.FakeBodies.Count;
        if (free <= 0)
            return false;

        var clones = new List<EntityUid>();
        _humanoids.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.BodyCloneRange, _humanoids);
        foreach (var humanoid in _humanoids)
        {
            if (humanoid.Owner != ent.Owner && Transform(humanoid).MapID != MapId.Nullspace)
                clones.Add(humanoid);
        }

        if (clones.Count == 0)
            return false;

        var time = _timing.CurTime;
        var count = Math.Min(free, _random.Next(1, 4));
        var spawned = 0;
        for (var i = 0; i < count; i++)
        {
            if (!TryPickFloorInView(ent, out var coords, 2))
                break;

            var clone = clones.Count > 1 ? _random.PickAndTake(clones) : clones[0];
            var duration = RandomTime(ent.Comp.BodyDurationMin, ent.Comp.BodyDurationMax);
            var id = ent.Comp.NextBodyId++;
            ent.Comp.FakeBodies.Add(new RMCHallucinationBody
            {
                Id = id,
                Clone = clone,
                Coordinates = coords,
                End = time + duration,
                NextSpeak = time + RandomTime(1, 4),
            });

            SendToVictim(ent, new RMCHallucinationBodyEvent(id, GetNetEntity(clone), GetNetCoordinates(coords), duration));
            SendToVictim(ent, new RMCHallucinationVisualEvent(BloodVisual, $"mfloor{_random.Next(1, 8)}", GetNetCoordinates(coords), duration));
            spawned++;
        }

        return spawned > 0;
    }

    private void UpdateFakeBodies(Entity<RMCHallucinatingComponent> ent)
    {
        var time = _timing.CurTime;
        for (var i = ent.Comp.FakeBodies.Count - 1; i >= 0; i--)
        {
            var body = ent.Comp.FakeBodies[i];
            if (time >= body.End || Deleted(body.Clone) || !body.Coordinates.IsValid(EntityManager))
            {
                ent.Comp.FakeBodies.RemoveAt(i);
                continue;
            }

            if (time < body.NextSpeak)
                continue;

            body.NextSpeak = time + RandomTime(ent.Comp.BodySpeakMin, ent.Comp.BodySpeakMax);
            if (_random.Prob(ent.Comp.BodyBegChance))
                FakeBodyBeg(ent, body);
            else
                FakeBodyGasp(ent, body);
        }
    }

    private void FakeBodyBeg(Entity<RMCHallucinatingComponent> ent, RMCHallucinationBody body)
    {
        var name = Identity.Name(body.Clone, EntityManager, ent);
        var message = Loc.GetString($"rmc-hallucination-body-beg-{_random.Next(1, BodyBegLines + 1)}", ("victim", Name(ent)));
        var wrapped = Loc.GetString("chat-manager-entity-whisper-wrap-message",
            ("entityName", FormattedMessage.EscapeText(name)),
            ("message", FormattedMessage.EscapeText(message)));

        SendToVictim(ent, new RMCHallucinationSpeechEvent(body.Id, true, ChatChannel.Whisper, message, wrapped));
    }

    private void FakeBodyGasp(Entity<RMCHallucinatingComponent> ent, RMCHallucinationBody body)
    {
        var name = Identity.Name(body.Clone, EntityManager, ent);
        var message = Loc.GetString("chat-emote-msg-gasp");
        var wrapped = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", FormattedMessage.EscapeText(name)),
            ("entity", name),
            ("message", message));

        SendToVictim(ent, new RMCHallucinationSpeechEvent(body.Id, true, ChatChannel.Emotes, message, wrapped));

        if (TryComp(body.Clone, out VocalComponent? vocal) &&
            vocal.EmoteSounds is { } soundsId &&
            _prototypes.TryIndex(soundsId, out var sounds))
        {
            var sound = sounds.Sounds.GetValueOrDefault(GaspEmote) ?? sounds.FallbackSound;
            if (sound != null)
                _audio.PlayStatic(sound, ent, body.Coordinates, sounds.GeneralParams);
        }
    }
}
