using Content.Shared.Interaction.Events;
using Content.Shared._RMC14.Chat;
using Content.Shared.Speech;
using Robust.Shared.Timing;
using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Megaphone;

public sealed class RMCMegaphoneSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMegaphoneComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ActorComponent, MegaphoneInputEvent>(OnMegaphoneInput);
    }

    private void OnUseInHand(Entity<RMCMegaphoneComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        var ev = new MegaphoneInputEvent(GetNetEntity(args.User));
        _dialog.OpenInput(args.User, "Enter a message for the megaphone:", ev, largeInput: false, characterLimit: 100);
    }

    private void OnMegaphoneInput(Entity<ActorComponent> ent, ref MegaphoneInputEvent ev)
    {
        if (ev.Handled)
            return;
        ev.Handled = true;

        if (_timing.ApplyingState)
            return;

        if (string.IsNullOrWhiteSpace(ev.Message))
            return;

        var user = GetEntity(ev.Actor);
        EnsureComp<RMCSpeechBubbleSpecificStyleComponent>(user);
        var userComp = EnsureComp<RMCMegaphoneUserComponent>(user);

        if (TryComp<SpeechComponent>(user, out var speech))
        {
            userComp.OriginalSpeechVerb = speech.SpeechVerb;
            userComp.OriginalSpeechSounds = speech.SpeechSounds;
            userComp.OriginalSuffixSpeechVerbs = speech.SuffixSpeechVerbs;

            speech.SpeechVerb = userComp.SpeechVerb;
            speech.SpeechSounds = userComp.MegaphoneSpeechSound;
            speech.SuffixSpeechVerbs = userComp.SuffixSpeechVerbs;
            Dirty(user, speech);

            // Send a message to the chat
            RaiseNetworkEvent(new MegaphoneMessageEvent(ev.Message, GetNetEntity(user)));

            // Restore the original speech settings
            speech.SpeechVerb = userComp.OriginalSpeechVerb ?? "Default";
            speech.SpeechSounds = userComp.OriginalSpeechSounds;
            speech.SuffixSpeechVerbs = userComp.OriginalSuffixSpeechVerbs ?? new();
            Dirty(user, speech);
        }

        RemComp<RMCSpeechBubbleSpecificStyleComponent>(user);
        RemComp<RMCMegaphoneUserComponent>(user);
    }
}

[Serializable, NetSerializable]
public sealed record MegaphoneInputEvent(NetEntity Actor, string Message = "") : DialogInputEvent(Message)
{
    public bool Handled { get; set; }
}

[Serializable, NetSerializable]
public sealed class MegaphoneMessageEvent : HandledEntityEventArgs
{
    public string Message { get; }
    public NetEntity Actor { get; }

    public MegaphoneMessageEvent(string message, NetEntity actor)
    {
        Message = message;
        Actor = actor;
    }
}
