using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Shared._RMC14.Language.Prototypes; // RMC14
using Content.Shared._RMC14.Mentor.ImaginaryFriend;
using Content.Shared._RMC14.Radio;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Prototypes; // RMC14

namespace Content.Server.Speech.EntitySystems;

/// <summary>
///     This system redirects local chat messages to listening entities (e.g., radio microphones).
/// </summary>
public sealed class ListeningSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntitySpokeEvent>(OnSpeak);
    }

    private void OnSpeak(EntitySpokeEvent ev)
    {
        // RMC14 - Direct intercom speech is routed by RMCIntercomSystem, not ambient microphones.
        if (ev.Channel?.ID == RMCIntercomConstants.Channel.Id)
            return;
        // RMC14

        PingListeners(ev.Source, ev.Message, ev.ObfuscatedMessage, ev.Language); // RMC14
    }

    public void PingListeners(
        EntityUid source,
        string message,
        string? obfuscatedMessage,
        ProtoId<LanguagePrototype>? language = null) // RMC14
    {
        // RMC14
        if (HasComp<XenoComponent>(source) || HasComp<ImaginaryFriendComponent>(source))
            return;
        // RMC14

        // TODO whispering / audio volume? Microphone sensitivity?
        // for now, whispering just arbitrarily reduces the listener's max range.

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourceXform = xformQuery.GetComponent(source);
        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);

        var attemptEv = new ListenAttemptEvent(source);
        var ev = new ListenEvent(message, source, language); // RMC14
        var obfuscatedEv = obfuscatedMessage == null ? null : new ListenEvent(obfuscatedMessage, source, language); // RMC14
        var query = EntityQueryEnumerator<ActiveListenerComponent, TransformComponent>();

        while (query.MoveNext(out var listenerUid, out var listener, out var xform))
        {
            if (xform.MapID != sourceXform.MapID)
                continue;

            // range checks
            // TODO proper speech occlusion
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).LengthSquared();
            if (distance > listener.Range * listener.Range)
                continue;

            RaiseLocalEvent(listenerUid, attemptEv);
            if (attemptEv.Cancelled)
            {
                attemptEv.Uncancel();
                continue;
            }

            if (obfuscatedEv != null && distance > ChatSystem.WhisperClearRange)
                RaiseLocalEvent(listenerUid, obfuscatedEv);
            else
                RaiseLocalEvent(listenerUid, ev);
        }
    }
}
