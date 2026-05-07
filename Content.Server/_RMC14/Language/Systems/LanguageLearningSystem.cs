using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Systems;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language;
using Content.Shared.Popups;
using Content.Server._RMC14.Language.Systems;
using Robust.Shared.Timing;
using System.Linq;
using Robust.Shared.Prototypes;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.GameStates;

namespace Content.Server._RMC14.Language.Systems;

public sealed class LanguageLearningSystem : SharedLanguageLearningSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly LanguageSystem _languageSystem = default!;

    private const float MaxHearingRange = 10.0f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<LanguageLearningComponent, MapInitEvent>(OnLearningMapInit);
        SubscribeLocalEvent<LanguageLearningComponent, DetermineEntityLanguagesEvent>(OnDetermineEntityLanguages);
        SubscribeLocalEvent<LanguageLearningComponent, ComponentGetState>(OnGetLearningState);
    }

    private void OnLearningMapInit(Entity<LanguageLearningComponent> ent, ref MapInitEvent args)
    {
        foreach (var language in ent.Comp.LearnableLanguages)
        {
            EnsureLanguageTracking(ent.Comp, language);

            if (ShouldStartEncountered(ent.Comp, language))
                ent.Comp.EncounteredLanguages.Add(language);
        }
    }

    private void OnGetLearningState(EntityUid uid, LanguageLearningComponent component, ref ComponentGetState args)
    {
        var learnedWordsForState = new Dictionary<ProtoId<LanguagePrototype>, Dictionary<string, float>>();

        foreach (var language in component.LearnableLanguages)
        {
            if (component.LearnedWords.TryGetValue(language, out var words))
            {
                learnedWordsForState[language] = new Dictionary<string, float>(words);
            }
            else
            {
                learnedWordsForState[language] = new Dictionary<string, float>();
            }
        }

        var progressForState = new Dictionary<ProtoId<LanguagePrototype>, float>();
        foreach (var language in component.LearnableLanguages)
        {
            progressForState[language] = component.LanguageProgress.GetValueOrDefault(language, 0f);
        }

        args.State = new LanguageLearningComponent.State(
            new HashSet<ProtoId<LanguagePrototype>>(component.LearnableLanguages),
            new HashSet<ProtoId<LanguagePrototype>>(component.FirstContactLanguages),
            new HashSet<ProtoId<LanguagePrototype>>(component.EncounteredLanguages),
            progressForState,
            learnedWordsForState
        );
    }

    private void OnDetermineEntityLanguages(Entity<LanguageLearningComponent> learner, ref DetermineEntityLanguagesEvent args)
    {
        var learningComp = learner.Comp;

        foreach (var language in learningComp.LearnableLanguages)
        {
            if (learningComp.LearnedWords.TryGetValue(language, out var words) && words.Count > 0)
            {
                args.SpokenLanguages.Add(language);
            }

            var comprehension = learningComp.LanguageProgress.GetValueOrDefault(language, 0f);
            if (comprehension >= learningComp.ComprehensionThreshold)
            {
                args.UnderstoodLanguages.Add(language);
            }
        }
    }

    private void OnEntitySpoke(EntitySpokeEvent args)
    {
        if (!_prototypeManager.TryIndex(args.Language, out var languageProto))
            return;

        var potentialLearners = new HashSet<EntityUid>();
        _lookupSystem.GetEntitiesInRange(Transform(args.Source).Coordinates, MaxHearingRange, potentialLearners);

        foreach (var potentialLearner in potentialLearners)
        {
            if (potentialLearner == args.Source)
                continue;

            if (!TryComp<LanguageLearningComponent>(potentialLearner, out var learnerComp))
                continue;

            if (!learnerComp.LearnableLanguages.Contains(args.Language))
                continue;

            if (TryComp<LanguageComponent>(potentialLearner, out var langComp))
            {
                var canSpeak = langComp.SpokenLanguages.Contains(args.Language);
                var canUnderstand = langComp.UnderstoodLanguages.Contains(args.Language);

                if (canSpeak && canUnderstand)
                    continue;
            }

            TryHandleFirstContact((potentialLearner, learnerComp), args.Language, languageProto);

            var distance = Vector2.Distance(Transform(args.Source).WorldPosition, Transform(potentialLearner).WorldPosition);

            if (distance > learnerComp.LearningRange)
                continue;

            TryLearnWords((potentialLearner, learnerComp), args.Source, args.Message, args.Language, distance);
        }
    }

    private void TryHandleFirstContact(
        Entity<LanguageLearningComponent> learner,
        ProtoId<LanguagePrototype> language,
        LanguagePrototype languageProto)
    {
        if (!learner.Comp.FirstContactLanguages.Contains(language) ||
            learner.Comp.EncounteredLanguages.Contains(language))
        {
            return;
        }

        learner.Comp.EncounteredLanguages.Add(language);

        var popup = languageProto.FirstContactMeaning != null
            ? Loc.GetString(
                "language-learning-first-contact-with-meaning",
                ("language", languageProto.LocalizedName),
                ("meaning", Loc.GetString(languageProto.FirstContactMeaning.Value)))
            : Loc.GetString("language-learning-first-contact", ("language", languageProto.LocalizedName));

        _popup.PopupEntity(popup, learner, learner, PopupType.Medium);
        Dirty(learner.Owner, learner.Comp);
    }

    private bool ShouldStartEncountered(LanguageLearningComponent comp, ProtoId<LanguagePrototype> language)
    {
        if (!comp.FirstContactLanguages.Contains(language))
            return true;

        if (comp.LanguageProgress.GetValueOrDefault(language) > 0f)
            return true;

        return comp.LearnedWords.TryGetValue(language, out var words) && words.Count > 0;
    }

    private static void EnsureLanguageTracking(LanguageLearningComponent comp, ProtoId<LanguagePrototype> language)
    {
        comp.LearnedWords.TryAdd(language, new Dictionary<string, float>());
        comp.WordFrequency.TryAdd(language, new Dictionary<string, int>());
        comp.LanguageProgress.TryAdd(language, 0f);
    }

    private void TryLearnWords(Entity<LanguageLearningComponent> learner, EntityUid source, string messageText,
        ProtoId<LanguagePrototype> language, float distance)
    {
        var comp = learner.Comp;
        var currentTime = _timing.CurTime;

        if (currentTime - comp.LastLearningTime < comp.MinTimeBetweenLearning)
            return;

        var sourceNetEntity = GetNetEntity(source);
        var studyCount = comp.StudiedSources.GetValueOrDefault(sourceNetEntity, 0);

        if (studyCount >= comp.MaxLearningFromSameSource)
            return;

        var diminishingFactor = Math.Max(0.2f, 1.0f - (studyCount / (float)comp.MaxLearningFromSameSource));
        var distancePenalty = Math.Max(0.5f, 1.0f - (distance / comp.LearningRange));

        if (!comp.LearnedWords.ContainsKey(language))
            comp.LearnedWords[language] = new Dictionary<string, float>();
        if (!comp.WordFrequency.ContainsKey(language))
            comp.WordFrequency[language] = new Dictionary<string, int>();

        var words = ExtractWords(messageText);

        var wordsLearned = 0;
        var significantLearning = false;

        foreach (var word in words)
        {
            if (word.Length < comp.MinWordLengthToLearn)
                continue;

            if (wordsLearned >= comp.MaxWordsToLearnPerMessage)
                break;

            var wordLower = word.ToLower();
            var currentComprehension = comp.LearnedWords[language].GetValueOrDefault(wordLower, 0f);

            if (currentComprehension >= comp.MaxWordComprehension)
                continue;

            comp.WordFrequency[language][wordLower] = comp.WordFrequency[language].GetValueOrDefault(wordLower, 0) + 1;

            var baseRate = currentComprehension == 0f ? 0.25f : comp.BaseWordLearningRate;
            var frequencyBonus = Math.Min(comp.WordFrequency[language][wordLower] * comp.FrequencyLearningBonus, 0.2f);

            var learningRate = baseRate + frequencyBonus;
            var actualLearning = learningRate * diminishingFactor * distancePenalty;
            actualLearning *= _random.NextFloat(0.9f, 1.1f);

            var newComprehension = Math.Min(comp.MaxWordComprehension, currentComprehension + actualLearning);
            var learningGain = newComprehension - currentComprehension;

            comp.LearnedWords[language][wordLower] = newComprehension;

            if (learningGain > 0.05f)
                significantLearning = true;

            wordsLearned++;
        }

        if (wordsLearned > 0)
        {
            comp.StudiedSources[sourceNetEntity] = studyCount + 1;
            comp.LastLearningTime = currentTime;

            UpdateLanguageProgress(learner, language);

            if (significantLearning)
            {
                var overallComprehension = CalculateOverallComprehension(comp, language);
                var percentage = (int)(overallComprehension * 100);
                var uniqueWords = comp.LearnedWords[language].Count;

                var progressMessage = wordsLearned == 1 ?
                    $"Language analysis improved. Progress: {percentage}% ({uniqueWords} words)" :
                    $"Analyzed {wordsLearned} terms. Progress: {percentage}% ({uniqueWords} words)";

                _popup.PopupEntity(progressMessage, learner, learner, PopupType.Medium);
            }

            Dirty(learner.Owner, comp);
        }
    }

    private void UpdateLanguageProgress(Entity<LanguageLearningComponent> learner, ProtoId<LanguagePrototype> language)
    {
        var comp = learner.Comp;
        var comprehension = CalculateOverallComprehension(comp, language);

        comp.LanguageProgress[language] = comprehension;

        var hasLearnedAnyWords = comp.LearnedWords.TryGetValue(language, out var words) && words.Count > 0;

        if (hasLearnedAnyWords)
        {
            _languageSystem.AddLanguage(learner.Owner, language, addSpoken: true, addUnderstood: false);
        }

        if (comprehension >= 0.8f && hasLearnedAnyWords)
        {
            if (TryComp<LanguageComponent>(learner.Owner, out var langComp))
            {
                if (!langComp.UnderstoodLanguages.Contains(language))
                {
                    _languageSystem.AddLanguage(learner.Owner, language, addSpoken: false, addUnderstood: true);
                    _popup.PopupEntity($"You have mastered {language}!", learner, learner, PopupType.Large);
                }
            }
        }
        else if (comprehension >= 0.6f && hasLearnedAnyWords)
        {
            _popup.PopupEntity($"You are becoming fluent in {language}!", learner, learner, PopupType.Large);
        }
    }

    public void SetWordComprehension(EntityUid entity, ProtoId<LanguagePrototype> language, string word, float level)
    {
        if (!TryComp<LanguageLearningComponent>(entity, out var comp))
            return;

        EnsureLanguageTracking(comp, language);
        var wordLower = word.ToLower();
        comp.LearnedWords[language][wordLower] = Math.Clamp(level, 0.0f, comp.MaxWordComprehension);

        if (ShouldStartEncountered(comp, language))
            comp.EncounteredLanguages.Add(language);

        comp.LanguageProgress[language] = CalculateOverallComprehension(comp, language);
        Dirty(entity, comp);
    }

    public void AddLearnableLanguage(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        var comp = EnsureComp<LanguageLearningComponent>(entity);
        comp.LearnableLanguages.Add(language);
        EnsureLanguageTracking(comp, language);

        if (ShouldStartEncountered(comp, language))
            comp.EncounteredLanguages.Add(language);

        Dirty(entity, comp);
    }

    public void RemoveLearnableLanguage(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        if (!TryComp<LanguageLearningComponent>(entity, out var comp))
            return;

        comp.LearnableLanguages.Remove(language);
        comp.EncounteredLanguages.Remove(language);
        comp.LanguageProgress.Remove(language);
        comp.LearnedWords.Remove(language);
        comp.WordFrequency.Remove(language);
        Dirty(entity, comp);
    }
}
