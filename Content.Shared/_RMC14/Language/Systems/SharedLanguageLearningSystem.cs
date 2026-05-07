using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Shared._RMC14.Language.Systems;

public abstract class SharedLanguageLearningSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;
    [Dependency] protected readonly SharedLanguageSystem _languageSystem = default!;

    protected const float MinComprehensionToSpeak = 0.2f;
    protected static readonly Regex WordRegex = new(@"\b[\w']+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    protected static readonly char[] GarbleChars = { '~', '?', '*', '#', '¿', '§' };

    public string ProcessMessageForListener(EntityUid listener, string message, ProtoId<LanguagePrototype> language)
    {
        if (_languageSystem.CanUnderstand(listener, language))
            return message;

        if (!TryComp<LanguageLearningComponent>(listener, out var comp))
            return _languageSystem.ObfuscateMessage(message, language);

        if (!comp.LearnableLanguages.Contains(language))
            return _languageSystem.ObfuscateMessage(message, language);

        return ProcessMessageWordByWord(message, language, comp);
    }

    public string ProcessMessageForSpeaker(EntityUid speaker, string message, ProtoId<LanguagePrototype> language)
    {
        if (_languageSystem.CanUnderstand(speaker, language))
            return message;

        if (!TryComp<LanguageLearningComponent>(speaker, out var comp))
            return _languageSystem.ObfuscateMessage(message, language);

        if (!comp.LearnableLanguages.Contains(language))
            return _languageSystem.ObfuscateMessage(message, language);

        return ProcessMessageWordByWord(message, language, comp);
    }

    public string ProcessMessageWordByWord(string message, ProtoId<LanguagePrototype> language, LanguageLearningComponent learningComp)
    {
        var overallComprehension = CalculateOverallComprehension(learningComp, language);
        var defaultComprehension = GetDefaultWordComprehension(language);
        var thresholds = GetComprehensionThresholds(language);

        learningComp.LearnedWords.TryGetValue(language, out var learnedWords);

        var result = new StringBuilder(message.Length);
        var lastIndex = 0;

        var matches = WordRegex.Matches(message);
        foreach (Match match in matches)
        {
            result.Append(message, lastIndex, match.Index - lastIndex);

            var word = match.Value;
            var wordLower = word.ToLower();

            var wordComprehension = 0f;
            if (learnedWords?.ContainsKey(wordLower) == true)
            {
                wordComprehension = learnedWords[wordLower];
            }
            else
            {
                wordComprehension = Math.Max(overallComprehension, defaultComprehension);
            }

            var effectiveComprehension = Math.Max(wordComprehension, overallComprehension);

            if (effectiveComprehension >= thresholds.Clear)
            {
                result.Append(word);
            }
            else if (effectiveComprehension >= thresholds.Partial)
            {
                result.Append(LightlyGarbleWord(word, effectiveComprehension));
            }
            else
            {
                result.Append(_languageSystem.ObfuscateMessage(word, language));
            }

            lastIndex = match.Index + match.Length;
        }

        result.Append(message, lastIndex, message.Length - lastIndex);
        return result.ToString();
    }

    public string ProcessWordForDisplay(
        string word,
        ProtoId<LanguagePrototype> language,
        float wordComprehension,
        float overallComprehension)
    {
        var thresholds = GetComprehensionThresholds(language);
        var effectiveComprehension = Math.Max(wordComprehension, overallComprehension);

        if (effectiveComprehension >= thresholds.Clear)
            return word;

        return _languageSystem.ObfuscateMessageForDisplayWithComprehension(
            word,
            language,
            effectiveComprehension);
    }

    protected string LightlyGarbleWord(string word, float comprehension)
    {
        var garbleRate = Math.Max(0.1f, (1.0f - comprehension) * 0.75f);

        var result = new StringBuilder(word);
        for (int i = 1; i < word.Length - 1; i++)
        {
            if (char.IsLetter(word[i]) && _random.Prob(garbleRate))
            {
                var garbleChar = _random.Pick(GarbleChars);
                result[i] = char.IsUpper(word[i]) ? char.ToUpperInvariant(garbleChar) : garbleChar;
            }
        }

        return result.ToString();
    }

    public List<string> ExtractWords(string message)
    {
        var matches = WordRegex.Matches(message);
        var words = new List<string>(matches.Count);
        foreach (Match match in matches)
            words.Add(match.Value);
        return words;
    }

    public float CalculateOverallComprehension(LanguageLearningComponent comp, ProtoId<LanguagePrototype> language)
    {
        if (!comp.LearnedWords.TryGetValue(language, out var learnedWords) || learnedWords.Count == 0)
            return 0f;

        if (!_prototypeManager.TryIndex(language, out var languageProto))
            return 0f;

        const int MinRequiredWords = 15;
        var expectedVocabSize = Math.Max(MinRequiredWords, languageProto.ExpectedVocabularySize);

        if (learnedWords.Count < MinRequiredWords)
        {
            var wordCountFactor = (float)learnedWords.Count / MinRequiredWords;
            var baseComprehension = CalculateBaseComprehension(comp, language, learnedWords);
            return Math.Min(0.95f, baseComprehension * wordCountFactor);
        }

        var vocabularyCompleteness = Math.Min(1.0f, (float)learnedWords.Count / expectedVocabSize);
        var averageWordComprehension = CalculateBaseComprehension(comp, language, learnedWords);

        var combinedComprehension = (averageWordComprehension * 0.7f) + (vocabularyCompleteness * 0.3f);
        return Math.Min(0.95f, combinedComprehension);
    }

    private float CalculateBaseComprehension(LanguageLearningComponent comp, ProtoId<LanguagePrototype> language, Dictionary<string, float> learnedWords)
    {
        var totalComprehension = 0f;
        var totalWeight = 0f;
        var wordFreq = comp.WordFrequency.GetValueOrDefault(language, new Dictionary<string, int>());

        foreach (var (word, comprehension) in learnedWords)
        {
            var frequency = wordFreq.GetValueOrDefault(word, 1);
            var weight = Math.Min(frequency, 10) + 1;
            totalComprehension += comprehension * weight;
            totalWeight += weight;
        }

        return totalWeight > 0 ? totalComprehension / totalWeight : 0f;
    }

    public float GetComprehensionLevel(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        if (!TryComp<LanguageLearningComponent>(entity, out var comp))
            return 0.0f;

        return CalculateOverallComprehension(comp, language);
    }

    public Dictionary<string, float> GetLearnedWords(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        if (!TryComp<LanguageLearningComponent>(entity, out var comp) ||
            !comp.LearnedWords.TryGetValue(language, out var words))
            return new Dictionary<string, float>();

        return new Dictionary<string, float>(words);
    }

    public float GetDefaultWordComprehension(ProtoId<LanguagePrototype> language)
    {
        if (_prototypeManager.TryIndex(language, out var languageProto))
            return languageProto.DefaultWordComprehension;
        return 0.0f;
    }

    public (float Clear, float Partial) GetComprehensionThresholds(ProtoId<LanguagePrototype> language)
    {
        if (_prototypeManager.TryIndex(language, out var languageProto))
            return (languageProto.ClearComprehensionThreshold, languageProto.PartialComprehensionThreshold);
        return (0.6f, 0.2f);
    }
}
