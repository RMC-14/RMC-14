using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Language;

[ByRefEvent]
public struct DetermineLanguageEvent
{
    public EntityUid Speaker;
    public ProtoId<LanguagePrototype> Language;

    public DetermineLanguageEvent(EntityUid speaker, ProtoId<LanguagePrototype> language)
    {
        Speaker = speaker;
        Language = language;
    }
}

[Serializable, NetSerializable]
public sealed class LanguagesSetMessage : EntityEventArgs
{
    public ProtoId<LanguagePrototype> CurrentLanguage;

    public LanguagesSetMessage(ProtoId<LanguagePrototype> currentLanguage)
    {
        CurrentLanguage = currentLanguage;
    }
}

[ByRefEvent]
public struct TransformLanguageMessageEvent
{
    public EntityUid Speaker;
    public ProtoId<LanguagePrototype> Language;
    public string Message;

    public TransformLanguageMessageEvent(EntityUid speaker, ProtoId<LanguagePrototype> language, string message)
    {
        Speaker = speaker;
        Language = language;
        Message = message;
    }
}

[ByRefEvent]
public struct CanUnderstandLanguageEvent
{
    public EntityUid Listener;
    public ProtoId<LanguagePrototype> Language;
    public bool CanUnderstand;

    public CanUnderstandLanguageEvent(EntityUid listener, ProtoId<LanguagePrototype> language)
    {
        Listener = listener;
        Language = language;
        CanUnderstand = false;
    }
}

public sealed class LanguagesUpdateEvent : EntityEventArgs
{
}

public sealed class LanguageChangeAttemptedEvent : EntityEventArgs
{
    public EntityUid Entity;
    public ProtoId<LanguagePrototype> OldLanguage;
    public ProtoId<LanguagePrototype> NewLanguage;
    public bool Success;

    public LanguageChangeAttemptedEvent(EntityUid entity, ProtoId<LanguagePrototype> oldLanguage, ProtoId<LanguagePrototype> newLanguage, bool success)
    {
        Entity = entity;
        OldLanguage = oldLanguage;
        NewLanguage = newLanguage;
        Success = success;
    }
}
