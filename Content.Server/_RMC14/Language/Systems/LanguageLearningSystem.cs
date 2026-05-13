using System.Numerics;
using Content.Server._RMC14.Language.Systems;
using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Language;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Language.Systems;

public sealed class LanguageLearningSystem : SharedLanguageLearningSystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly LanguageSystem _languages = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float MaxHearingRange = 10.0f;
    private readonly HashSet<EntityUid> _potentialLearners = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<LanguageLearningComponent, MapInitEvent>(OnLearningMapInit);
        SubscribeLocalEvent<LanguageLearningComponent, DetermineEntityLanguagesEvent>(OnDetermineEntityLanguages);
    }

    private void OnLearningMapInit(Entity<LanguageLearningComponent> ent, ref MapInitEvent args)
    {
        foreach (var language in ent.Comp.LearnableLanguages)
        {
            var languageData = EnsureLanguageTracking(ent.Comp, language);

            if (ShouldStartEncountered(ent.Comp, language))
                languageData.Encountered = true;
        }

        SyncLanguageStates(ent.Comp);
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
            if (comprehension >= learningComp.MasteredComprehensionThreshold)
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

            if (languageProto.NeedsLOS &&
                !_examine.InRangeUnOccluded(args.Source, potentialLearner, MaxHearingRange))
            {
                continue;
            }

            TryHandleFirstContact((potentialLearner, learnerComp), args.Language);

            var distance = Vector2.Distance(Transform(args.Source).WorldPosition, Transform(potentialLearner).WorldPosition);

            if (distance > learnerComp.LearningRange)
                continue;

            TryLearnWords((potentialLearner, learnerComp), args.Source, args.Message, args.Language, distance);
        }
    }

    private void TryHandleFirstContact(
        Entity<LanguageLearningComponent> learner,
        ProtoId<LanguagePrototype> language)
    {
        if (!learner.Comp.FirstContactLanguages.Contains(language) ||
            !learner.Comp.Languages.TryGetValue(language, out var languageData) ||
            languageData.Encountered)
        {
            return;
        }

        languageData.Encountered = true;
        SyncLanguageState(learner.Comp, language);
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

            if (existing.BoostedWordsRemaining <= 0 &&
                existing.InitialBoostedWordCount > 0 &&
                existing.LearnedWords.Count == 0)
            {
                existing.BoostedWordsRemaining = existing.InitialBoostedWordCount;
            }

            return existing;
        }

        var created = new LanguageLearningData
        {
            RequiresFirstContact = comp.FirstContactLanguages.Contains(language),
            BoostedWordsRemaining = 0,
        };
        comp.Languages[language] = created;

        if (created.InitialBoostedWordCount > 0)
            created.BoostedWordsRemaining = created.InitialBoostedWordCount;

        return created;
    }

    private static void SyncLanguageState(LanguageLearningComponent comp, ProtoId<LanguagePrototype> language)
    {
        if (!comp.Languages.TryGetValue(language, out var data))
        {
            comp.LanguageStates.Remove(language);
            return;
        }

        comp.LanguageStates[language] = new LanguageLearningStateData(
            data.RequiresFirstContact,
            data.Encountered,
            data.Progress,
            new Dictionary<string, float>(data.LearnedWords));
    }

    private static void SyncLanguageStates(LanguageLearningComponent comp)
    {
        comp.LanguageStates.Clear();

        foreach (var language in comp.Languages.Keys)
        {
            SyncLanguageState(comp, language);
        }
    }

    private void TryLearnWords(Entity<LanguageLearningComponent> learner, EntityUid source, string messageText,
        ProtoId<LanguagePrototype> language, float distance)
    {
        var comp = learner.Comp;
        var currentTime = _timing.CurTime;

        if (currentTime - comp.LastLearningTime < comp.MinTimeBetweenLearning)
            return;

        var sourceNetEntity = GetNetEntity(source);
        var isTracked = comp.StudiedSources.ContainsKey(sourceNetEntity);

        if (!isTracked && comp.StudiedSources.Count >= comp.MaxStudiedSourcesTracked)
            return;

        var studyCount = comp.StudiedSources.GetValueOrDefault(sourceNetEntity, 0);

        if (studyCount >= comp.MaxLearningFromSameSource)
            return;

        if (!_prototypeManager.TryIndex(language, out var languageProto))
            return;

        var diminishingFactor = Math.Max(
            comp.MinimumDiminishingFactor,
            1.0f - (studyCount / (float) comp.MaxLearningFromSameSource));
        var distancePenalty = Math.Max(
            comp.MinimumDistancePenalty,
            1.0f - (distance / comp.LearningRange));

        var languageData = EnsureLanguageTracking(comp, language);

        var words = ExtractWords(messageText);

        var wordsLearned = 0;

        foreach (var word in words)
        {
            if (word.Length < comp.MinWordLengthToLearn)
                continue;

            if (wordsLearned >= comp.MaxWordsToLearnPerMessage)
                break;

            var wordLower = word.ToLower();
            var currentComprehension = languageData.LearnedWords.GetValueOrDefault(wordLower, 0f);

            if (currentComprehension == 0f &&
                languageData.BoostedWordsRemaining > 0 &&
                languageData.InitialBoostedWordComprehension > 0f)
            {
                currentComprehension = Math.Min(comp.MaxWordComprehension, languageData.InitialBoostedWordComprehension);
                languageData.LearnedWords[wordLower] = currentComprehension;
                languageData.BoostedWordsRemaining--;
            }

            if (currentComprehension >= comp.MaxWordComprehension)
                continue;

            languageData.WordFrequency[wordLower] = languageData.WordFrequency.GetValueOrDefault(wordLower, 0) + 1;

            var baseRate = currentComprehension == 0f ? comp.InitialWordLearningRate : comp.BaseWordLearningRate;
            var frequencyBonus = Math.Min(
                languageData.WordFrequency[wordLower] * comp.FrequencyLearningBonus,
                comp.MaxFrequencyLearningBonus);

            var learningRate = baseRate + frequencyBonus;
            var actualLearning = learningRate * diminishingFactor * distancePenalty;
            actualLearning *= languageProto.LearningRateMultiplier;
            actualLearning *= _random.NextFloat(0.9f, 1.1f);

            var newComprehension = Math.Min(comp.MaxWordComprehension, currentComprehension + actualLearning);
            languageData.LearnedWords[wordLower] = newComprehension;

            wordsLearned++;
        }

        if (wordsLearned > 0)
        {
            comp.StudiedSources[sourceNetEntity] = studyCount + 1;
            comp.LastLearningTime = currentTime;

            UpdateLanguageProgress(learner, language);

            SyncLanguageState(comp, language);
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
            _languages.AddLanguage(learner.Owner, language, addSpoken: true, addUnderstood: false);
        }

        if (comprehension >= comp.MasteredComprehensionThreshold && hasLearnedAnyWords)
        {
            if (TryComp<LanguageComponent>(learner.Owner, out var langComp))
            {
                if (!langComp.UnderstoodLanguages.Contains(language))
                    _languages.AddLanguage(learner.Owner, language, addSpoken: false, addUnderstood: true);
            }
        }
        else if (comprehension >= comp.FluentComprehensionThreshold && hasLearnedAnyWords && !languageData.FluentAnnounced)
        {
            languageData.FluentAnnounced = true;
        }

        SyncLanguageState(comp, language);
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
        SyncLanguageState(comp, language);
        Dirty(entity, comp);
    }

    public void AddLearnableLanguage(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        var comp = EnsureComp<LanguageLearningComponent>(entity);
        comp.LearnableLanguages.Add(language);
        var languageData = EnsureLanguageTracking(comp, language);

        if (ShouldStartEncountered(comp, language))
            languageData.Encountered = true;

        SyncLanguageState(comp, language);
        Dirty(entity, comp);
    }

    public void RemoveLearnableLanguage(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        if (!TryComp<LanguageLearningComponent>(entity, out var comp))
            return;

        comp.LearnableLanguages.Remove(language);
        comp.FirstContactLanguages.Remove(language);
        comp.Languages.Remove(language);
        comp.LanguageStates.Remove(language);
        Dirty(entity, comp);
    }
}
