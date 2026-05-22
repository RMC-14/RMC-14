using System.Text;
using System.Text.RegularExpressions;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Language.Systems;

public abstract class SharedLanguageLearningSystem : EntitySystem
{
    [Dependency] protected readonly SharedLanguageSystem _language = default!;
    [Dependency] protected readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;

    protected static readonly Regex WordRegex = new(@"\b[a-zA-Z']+\b", RegexOptions.Compiled);

    public string ProcessMessageForListener(EntityUid listener, string message, ProtoId<LanguagePrototype> language)
    {
        if (_language.CanUnderstand(listener, language))
            return message;

        if (!TryComp<LanguageLearningComponent>(listener, out var comp))
            return _language.ObfuscateMessage(message, language);

        if (!comp.Languages.ContainsKey(language))
            return _language.ObfuscateMessage(message, language);

        return ProcessMessageWordByWord(message, language, comp);
    }

    public string ProcessMessageForSpeaker(EntityUid speaker, string message, ProtoId<LanguagePrototype> language)
    {
        if (_language.CanUnderstand(speaker, language))
            return message;

        if (!TryComp<LanguageLearningComponent>(speaker, out var comp))
            return _language.ObfuscateMessage(message, language);

        if (!comp.Languages.ContainsKey(language))
            return _language.ObfuscateMessage(message, language);

        return ProcessMessageWordByWord(message, language, comp);
    }

    public string ProcessMessageWordByWord(string message, ProtoId<LanguagePrototype> language, LanguageLearningComponent learningComp)
    {
        var overallComprehension = CalculateOverallComprehension(learningComp, language);
        var defaultComprehension = GetDefaultWordComprehension(language);
        var thresholds = GetComprehensionThresholds(language);

        learningComp.Languages.TryGetValue(language, out var languageData);
        var learnedWords = languageData?.LearnedWords;
        var previewBoostedWordsRemaining = languageData?.BoostedWordsRemaining ?? 0;
        var previewBoostedWordComprehension = languageData?.InitialBoostedWordComprehension ?? 0f;
        HashSet<string>? previewBoostedWords = null;

        var result = new StringBuilder(message.Length);
        var lastIndex = 0;

        var matches = WordRegex.Matches(message);
        foreach (Match match in matches)
        {
            result.Append(message, lastIndex, match.Index - lastIndex);

            var word = match.Value;
            var wordLower = word.ToLowerInvariant();

            var wordComprehension = 0f;
            if (learnedWords?.ContainsKey(wordLower) == true)
                wordComprehension = learnedWords[wordLower];
            else
            {
                if (previewBoostedWordsRemaining > 0 && previewBoostedWordComprehension > 0f)
                {
                    previewBoostedWords ??= new HashSet<string>();

                    if (previewBoostedWords.Add(wordLower))
                        previewBoostedWordsRemaining--;

                    wordComprehension = Math.Max(previewBoostedWordComprehension, defaultComprehension);
                }
                else
                {
                    wordComprehension = Math.Max(overallComprehension, defaultComprehension);
                }
            }

            var effectiveComprehension = Math.Max(wordComprehension, overallComprehension);

            if (effectiveComprehension >= thresholds.Clear)
            {
                result.Append(word);
            }
            else if (effectiveComprehension >= thresholds.Partial)
            {
                result.Append(LightlyGarbleWord(word, language, effectiveComprehension));
            }
            else
            {
                result.Append(_language.ObfuscateMessage(word, language));
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

        return _language.ObfuscateMessageForDisplayWithComprehension(
            word,
            language,
            effectiveComprehension);
    }

    protected string LightlyGarbleWord(string word, ProtoId<LanguagePrototype> language, float comprehension)
    {
        var garbleCharacters = LanguagePrototype.DefaultPartialGarbleCharactersValue;
        var minimumGarbleRate = LanguagePrototype.DefaultMinimumPartialGarbleRateValue;
        var garbleRateMultiplier = LanguagePrototype.DefaultPartialGarbleRateMultiplierValue;

        if (_prototypeManager.TryIndex(language, out var languageProto))
        {
            if (!string.IsNullOrEmpty(languageProto.PartialGarbleCharacters))
                garbleCharacters = languageProto.PartialGarbleCharacters;

            minimumGarbleRate = languageProto.MinimumPartialGarbleRate;
            garbleRateMultiplier = languageProto.PartialGarbleRateMultiplier;
        }

        var garbleRate = Math.Max(minimumGarbleRate, (1.0f - comprehension) * garbleRateMultiplier);
        var result = new StringBuilder(word);

        for (var i = 1; i < word.Length - 1; i++)
        {
            if (!char.IsLetter(word[i]) || !_random.Prob(garbleRate))
                continue;

            var garbleChar = garbleCharacters[_random.Next(garbleCharacters.Length)];
            result[i] = char.IsUpper(word[i]) ? char.ToUpperInvariant(garbleChar) : garbleChar;
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
        if (!comp.Languages.TryGetValue(language, out var languageData) ||
            languageData.LearnedWords.Count == 0)
        {
            return 0f;
        }

        if (!_prototypeManager.TryIndex(language, out var languageProto))
            return 0f;

        var minimumRequiredWords = Math.Max(1, languageProto.MinimumRequiredLearnedWords);
        var expectedVocabSize = Math.Max(minimumRequiredWords, languageProto.ExpectedVocabularySize);

        if (languageData.LearnedWords.Count < minimumRequiredWords)
        {
            var wordCountFactor = (float) languageData.LearnedWords.Count / minimumRequiredWords;
            var baseComprehension = CalculateBaseComprehension(languageData);
            return Math.Min(languageProto.MaximumOverallComprehension, baseComprehension * wordCountFactor);
        }

        var vocabularyCompleteness = Math.Min(1.0f, (float) languageData.LearnedWords.Count / expectedVocabSize);
        var averageWordComprehension = CalculateBaseComprehension(languageData);
        var totalWeight = languageProto.LearnedWordComprehensionWeight + languageProto.VocabularyCompletenessWeight;
        var combinedComprehension = totalWeight > 0f
            ? ((averageWordComprehension * languageProto.LearnedWordComprehensionWeight) +
               (vocabularyCompleteness * languageProto.VocabularyCompletenessWeight)) / totalWeight
            : averageWordComprehension;

        return Math.Min(languageProto.MaximumOverallComprehension, combinedComprehension);
    }

    private static float CalculateBaseComprehension(LanguageLearningData languageData)
    {
        var totalComprehension = 0f;
        var totalWeight = 0f;

        foreach (var (word, comprehension) in languageData.LearnedWords)
        {
            var frequency = languageData.WordFrequency.GetValueOrDefault(word, 1);
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
            !comp.Languages.TryGetValue(language, out var languageData))
        {
            return new Dictionary<string, float>();
        }

        return new Dictionary<string, float>(languageData.LearnedWords);
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

        return (
            LanguagePrototype.DefaultClearComprehensionThresholdValue,
            LanguagePrototype.DefaultPartialComprehensionThresholdValue);
    }
}
