using System.Text;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Language.Systems;

public class SharedLanguageSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public static readonly ProtoId<LanguagePrototype> CommonLanguage = "English";
    private static readonly IReadOnlySet<ProtoId<LanguagePrototype>> DefaultLanguages =
        new HashSet<ProtoId<LanguagePrototype>> { CommonLanguage };
    private int _roundSeed;

    public ProtoId<LanguagePrototype> GetCurrentLanguage(EntityUid entity)
    {
        if (!TryComp<LanguageComponent>(entity, out var component))
            return CommonLanguage;

        return component.CurrentLanguage ?? component.DefaultLanguage ?? CommonLanguage;
    }

    public bool CanSpeak(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        if (!TryComp<LanguageComponent>(entity, out var component))
            return language == CommonLanguage;

        return component.SpokenLanguages.Contains(language);
    }

    public bool CanUnderstand(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        if (!TryComp<LanguageComponent>(entity, out var component))
            return language == CommonLanguage;

        return component.UnderstoodLanguages.Contains(language);
    }

    public IReadOnlySet<ProtoId<LanguagePrototype>> GetSpokenLanguages(EntityUid entity)
    {
        if (!TryComp<LanguageComponent>(entity, out var component))
            return DefaultLanguages;

        return component.SpokenLanguages;
    }

    public IReadOnlySet<ProtoId<LanguagePrototype>> GetUnderstoodLanguages(EntityUid entity)
    {
        if (!TryComp<LanguageComponent>(entity, out var component))
            return DefaultLanguages;

        return component.UnderstoodLanguages;
    }

    public string ObfuscateMessage(string message, ProtoId<LanguagePrototype> language)
    {
        if (!_prototypeManager.TryIndex(language, out var languageProto))
            return message;

        return ObfuscateMessageInternal(message, languageProto.ObfuscationMethod, languageProto.RandomizeObfuscation);
    }

    public string ObfuscateMessageWithComprehension(string message, ProtoId<LanguagePrototype> language, float comprehension)
    {
        if (!_prototypeManager.TryIndex(language, out var languageProto))
            return message;

        return ObfuscateMessageInternalWithComprehension(
            message,
            languageProto.ObfuscationMethod,
            languageProto.RandomizeObfuscation,
            comprehension);
    }

    protected string ObfuscateMessageInternal(
        string message,
        ObfuscationMethod obfuscationMethod,
        bool randomize)
    {
        var builder = new StringBuilder(message.Length);
        obfuscationMethod.ObfuscateInternal(builder, message, this, randomize);
        return builder.ToString();
    }

    protected string ObfuscateMessageInternalWithComprehension(
        string message,
        ObfuscationMethod obfuscationMethod,
        bool randomize,
        float comprehension)
    {
        var builder = new StringBuilder(message.Length);
        obfuscationMethod.ObfuscateInternalWithComprehension(builder, message, this, randomize, comprehension);
        return builder.ToString();
    }

    public System.Random CreateRandom(int seed, bool randomize)
    {
        if (!randomize)
            return new System.Random(CombineSeed(seed, _roundSeed));

        return new System.Random(_random.Next());
    }

    protected void ReseedObfuscationForRound()
    {
        _roundSeed = _random.Next();
    }

    private static int CombineSeed(int seed, int roundSeed)
    {
        unchecked
        {
            return (seed * 397) ^ roundSeed;
        }
    }

    public int PseudoRandomNumber(int seed, int min, int max)
    {
        return PseudoRandomNumber(seed, min, max, false);
    }

    public int PseudoRandomNumber(int seed, int min, int max, bool randomize)
    {
        if (min >= max)
            return min;

        var random = CreateRandom(seed, randomize);
        if (max == int.MaxValue)
            return (int) random.NextInt64(min, (long) max + 1);

        return random.Next(min, max + 1);
    }
}
