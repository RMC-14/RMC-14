using System.Collections.Frozen;
using System.Linq;
using System.Text;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Language.Systems;

public class SharedLanguageSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public static readonly ProtoId<LanguagePrototype> CommonLanguage = "English";

    private FrozenDictionary<char, LanguagePrototype> _languagePrefixes = FrozenDictionary<char, LanguagePrototype>.Empty;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
        CacheLanguagePrefixes();
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<LanguagePrototype>())
            CacheLanguagePrefixes();
    }

    private void CacheLanguagePrefixes()
    {
        var dict = new Dictionary<char, LanguagePrototype>();
        foreach (var language in _prototypeManager.EnumeratePrototypes<LanguagePrototype>())
        {
            if (language.ChatPrefix.HasValue)
                dict.TryAdd(language.ChatPrefix.Value, language);
        }
        _languagePrefixes = dict.ToFrozenDictionary();
    }

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

    public HashSet<ProtoId<LanguagePrototype>> GetSpokenLanguages(EntityUid entity)
    {
        if (!TryComp<LanguageComponent>(entity, out var component))
            return new HashSet<ProtoId<LanguagePrototype>> { CommonLanguage };

        return new HashSet<ProtoId<LanguagePrototype>>(component.SpokenLanguages);
    }

    public HashSet<ProtoId<LanguagePrototype>> GetUnderstoodLanguages(EntityUid entity)
    {
        if (!TryComp<LanguageComponent>(entity, out var component))
            return new HashSet<ProtoId<LanguagePrototype>> { CommonLanguage };

        return new HashSet<ProtoId<LanguagePrototype>>(component.UnderstoodLanguages);
    }

    public string ObfuscateMessage(string message, ProtoId<LanguagePrototype> language)
    {
        if (!_prototypeManager.TryIndex(language, out var languageProto))
            return message;

        return ObfuscateMessageInternal(message, languageProto.ObfuscationMethod);
    }

    public string ObfuscateMessageWithComprehension(string message, ProtoId<LanguagePrototype> language, float comprehension)
    {
        if (!_prototypeManager.TryIndex(language, out var languageProto))
            return message;

        return ObfuscateMessageInternalWithComprehension(message, languageProto.ObfuscationMethod, comprehension);
    }

    protected string ObfuscateMessageInternal(string message, ObfuscationMethod obfuscationMethod)
    {
        var builder = new StringBuilder();
        obfuscationMethod.ObfuscateInternal(builder, message, this);
        return builder.ToString();
    }

    protected string ObfuscateMessageInternalWithComprehension(string message, ObfuscationMethod obfuscationMethod, float comprehension)
    {
        var builder = new StringBuilder();
        obfuscationMethod.ObfuscateInternalWithComprehension(builder, message, this, comprehension);
        return builder.ToString();
    }

    public bool TryParseLanguagePrefix(string message, out ProtoId<LanguagePrototype> language, out string remainingMessage)
    {
        language = default;
        remainingMessage = message;

        if (message.Length < 2 || message[0] != '#')
            return false;

        var prefix = char.ToLower(message[1]);

        if (!_languagePrefixes.TryGetValue(prefix, out var languageProto))
            return false;

        language = languageProto.ID;
        remainingMessage = message[2..].TrimStart();
        return true;
    }

    public int PseudoRandomNumber(int seed, int min, int max)
    {
        if (min >= max)
            return min;

        var random = new System.Random(seed);
        return random.Next(min, max + 1);
    }

    public Color? GetLanguageColor(ProtoId<LanguagePrototype> language)
    {
        return _prototypeManager.TryIndex(language, out var languageProto) ? languageProto.TextColor : null;
    }

    public string? GetLanguageTypeface(ProtoId<LanguagePrototype> language)
    {
        return _prototypeManager.TryIndex(language, out var languageProto) ? languageProto.TypefaceId : null;
    }

    public int? GetLanguageTextSize(ProtoId<LanguagePrototype> language)
    {
        return _prototypeManager.TryIndex(language, out var languageProto) ? languageProto.TextSize : null;
    }

    public string? GetLanguageBoldTypeface(ProtoId<LanguagePrototype> language)
    {
        return _prototypeManager.TryIndex(language, out var languageProto) ? languageProto.BoldTypefaceId : null;
    }

    public bool DoesLanguageShowName(ProtoId<LanguagePrototype> language)
    {
        return _prototypeManager.TryIndex(language, out var languageProto) && languageProto.ShowLanguageName;
    }

    public string? GetLanguageIcon(ProtoId<LanguagePrototype> language)
    {
        return _prototypeManager.TryIndex(language, out var languageProto) ? languageProto.LanguageIcon : null;
    }
}
