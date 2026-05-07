using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Language;
using Content.Server._RMC14.Language.Systems;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Content.Shared.Popups;
using System.Linq;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Language.Systems;

public sealed class LanguageLearningSystem : SharedLanguageLearningSystem
{
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float MaxHearingRange = 10.0f;
    private readonly HashSet<EntityUid> _potentialLearners = [];

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
            var languageData = EnsureLanguageTracking(ent.Comp, language);

            if (ShouldStartEncountered(ent.Comp, language))
                languageData.Encountered = true;
        }
    }

    private void OnGetLearningState(Entity<LanguageLearningComponent> ent, ref ComponentGetState args)
    {
        var languagesForState = ent.Comp.Languages.ToDictionary(
            kvp => kvp.Key,
            kvp => new LanguageLearningStateData(
                kvp.Value.RequiresFirstContact,
                kvp.Value.Encountered,
                kvp.Value.Progress,
                new Dictionary<string, float>(kvp.Value.LearnedWords))
        );

        args.State = new LanguageLearningComponent.State(languagesForState);
    }

    private void OnDetermineEntityLanguages(Entity<LanguageLearningComponent> learner, ref DetermineEntityLanguagesEvent args)
    {
        var learningComp = learner.Comp;

        foreach (var (language, languageData) in learningComp.Languages)
        {
            if (languageData.LearnedWords.Count > 0)
            {
                args.SpokenLanguages.Add(language);
            }

            var comprehension = languageData.Progress;
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

        _potentialLearners.Clear();
        _lookup.GetEntitiesInRange(Transform(args.Source).Coordinates, MaxHearingRange, _potentialLearners);

        foreach (var potentialLearner in _potentialLearners)
        {
            if (potentialLearner == args.Source)
                continue;

            if (!TryComp<LanguageLearningComponent>(potentialLearner, out var learnerComp))
                continue;

            if (!learnerComp.Languages.ContainsKey(args.Language))
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
            !learner.Comp.Languages.TryGetValue(language, out var languageData) ||
            languageData.Encountered)
        {
            return;
        }

        languageData.Encountered = true;

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

        if (!comp.Languages.TryGetValue(language, out var languageData))
            return false;

        if (languageData.Progress > 0f)
            return true;

        return languageData.LearnedWords.Count > 0;
    }

    private static LanguageLearningData EnsureLanguageTracking(LanguageLearningComponent comp, ProtoId<LanguagePrototype> language)
    {
        if (comp.Languages.TryGetValue(language, out var existing))
        {
            existing.RequiresFirstContact = comp.FirstContactLanguages.Contains(language);
            return existing;
        }

        var created = new LanguageLearningData
        {
            RequiresFirstContact = comp.FirstContactLanguages.Contains(language),
        };
        comp.Languages[language] = created;
        return created;
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

        var languageData = EnsureLanguageTracking(comp, language);

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
            var currentComprehension = languageData.LearnedWords.GetValueOrDefault(wordLower, 0f);

            if (currentComprehension >= comp.MaxWordComprehension)
                continue;

            languageData.WordFrequency[wordLower] = languageData.WordFrequency.GetValueOrDefault(wordLower, 0) + 1;

            var baseRate = currentComprehension == 0f ? 0.25f : comp.BaseWordLearningRate;
            var frequencyBonus = Math.Min(languageData.WordFrequency[wordLower] * comp.FrequencyLearningBonus, 0.2f);

            var learningRate = baseRate + frequencyBonus;
            var actualLearning = learningRate * diminishingFactor * distancePenalty;
            actualLearning *= _random.NextFloat(0.9f, 1.1f);

            var newComprehension = Math.Min(comp.MaxWordComprehension, currentComprehension + actualLearning);
            var learningGain = newComprehension - currentComprehension;

            languageData.LearnedWords[wordLower] = newComprehension;

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
                var uniqueWords = languageData.LearnedWords.Count;

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
        var languageData = EnsureLanguageTracking(comp, language);

        languageData.Progress = comprehension;

        var hasLearnedAnyWords = languageData.LearnedWords.Count > 0;

        if (hasLearnedAnyWords)
        {
            _language.AddLanguage(learner.Owner, language, addSpoken: true, addUnderstood: false);
        }

        if (comprehension >= 0.8f && hasLearnedAnyWords)
        {
            if (TryComp<LanguageComponent>(learner.Owner, out var langComp))
            {
                if (!langComp.UnderstoodLanguages.Contains(language))
                {
                    _language.AddLanguage(learner.Owner, language, addSpoken: false, addUnderstood: true);
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

        var languageData = EnsureLanguageTracking(comp, language);
        var wordLower = word.ToLower();
        languageData.LearnedWords[wordLower] = Math.Clamp(level, 0.0f, comp.MaxWordComprehension);

        if (ShouldStartEncountered(comp, language))
            languageData.Encountered = true;

        languageData.Progress = CalculateOverallComprehension(comp, language);
        Dirty(entity, comp);
    }

    public void AddLearnableLanguage(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        var comp = EnsureComp<LanguageLearningComponent>(entity);
        comp.LearnableLanguages.Add(language);
        var languageData = EnsureLanguageTracking(comp, language);

        if (ShouldStartEncountered(comp, language))
            languageData.Encountered = true;

        Dirty(entity, comp);
    }

    public void RemoveLearnableLanguage(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        if (!TryComp<LanguageLearningComponent>(entity, out var comp))
            return;

        comp.LearnableLanguages.Remove(language);
        comp.FirstContactLanguages.Remove(language);
        comp.Languages.Remove(language);
        Dirty(entity, comp);
    }
}
