using Content.Shared._RMC14.Language.Prototypes; // RMC14
using Content.Shared._RMC14.Language.Systems; // RMC14
using Robust.Shared.Prototypes; // RMC14

namespace Content.Server.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;
    public readonly ProtoId<LanguagePrototype> Language; // RMC14

    public ListenEvent(string message, EntityUid source, ProtoId<LanguagePrototype>? language = null) // RMC14
    {
        Message = message;
        Source = source;
        Language = language ?? SharedLanguageSystem.CommonLanguage; // RMC14
    }
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
