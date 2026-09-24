using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Language;

[ByRefEvent]
public record struct DetermineLanguageEvent(EntityUid Speaker, ProtoId<LanguagePrototype> Language);

[Serializable, NetSerializable]
public sealed class LanguagesSetMessage(ProtoId<LanguagePrototype> currentLanguage) : EntityEventArgs
{
    public ProtoId<LanguagePrototype> CurrentLanguage = currentLanguage;
}

[ByRefEvent]
public record struct TransformLanguageMessageEvent(
    EntityUid Speaker,
    ProtoId<LanguagePrototype> Language,
    string Message);

[ByRefEvent]
public record struct CanUnderstandLanguageEvent(
    EntityUid Listener,
    ProtoId<LanguagePrototype> Language,
    bool CanUnderstand = false);

[ByRefEvent]
public record struct DetermineEntityLanguagesEvent(
    HashSet<ProtoId<LanguagePrototype>> SpokenLanguages,
    HashSet<ProtoId<LanguagePrototype>> UnderstoodLanguages)
{
    public DetermineEntityLanguagesEvent() : this([], [])
    {
    }
}

[ByRefEvent]
public record struct ProcessSpeakerLanguageEvent(
    EntityUid Speaker,
    ProtoId<LanguagePrototype> Language,
    string ProcessedMessage);

[ByRefEvent]
public readonly record struct LanguagesUpdateEvent;

[ByRefEvent]
public readonly record struct LanguageChangeAttemptedEvent(
    EntityUid Entity,
    ProtoId<LanguagePrototype> OldLanguage,
    ProtoId<LanguagePrototype> NewLanguage,
    bool Success);
