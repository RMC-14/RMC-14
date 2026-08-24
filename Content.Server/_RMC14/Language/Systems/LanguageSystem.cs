using System.Linq;
using Content.Server.GameTicking.Events;
using Content.Shared._RMC14.Language;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Language.Systems;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly LanguageLearningSystem _learning = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    private EntityQuery<LanguageLearningComponent> _learningQuery;

    private readonly HashSet<ProtoId<LanguagePrototype>> _checkedLanguages = [];

    public override void Initialize()
    {
        base.Initialize();

        _learningQuery = GetEntityQuery<LanguageLearningComponent>();

        SubscribeLocalEvent<LanguageComponent, MapInitEvent>(OnInitLanguageSpeaker);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeNetworkEvent<LanguagesSetMessage>(OnClientSetLanguage);
    }

    /// <summary>
    /// Check whether <paramref name="recipient"/> should be able to see <see cref="ChatChannel.IC"/> chat messages from
    /// <paramref name="speaker"/> as a popup and in their chat box, based on the <see cref="LanguageComponent"/> of each.
    /// <para>
    /// This is primarily used to hide speech between different factions.
    /// </para>
    /// </summary>
    /// <param name="recipient">The entity hearing the message.</param>
    /// <param name="speaker">The entity that created the message.</param>
    /// <returns>
    /// <see langword="true"/> if both entities understand the same language, both understand a language with the same <see cref="LanguagePrototype.Category"/>,
    /// or if one entity has <see cref="LanguageLearningComponent"/> and is able to learn a language known by the other. Otherwise <see langword="false"/>.
    /// </returns>
    /// <seealso cref="LanguageCategory"/>
    public bool CanSeeICMessage(Entity<LanguageComponent?> recipient, Entity<LanguageComponent?> speaker)
    {
        // Always allow if there's no language barrier.
        if (!Resolve(recipient, ref recipient.Comp, false) || !Resolve(speaker, ref speaker.Comp, false))
            return true;

        var recipientLearning = _learningQuery.CompOrNull(recipient);
        var speakerLearning = _learningQuery.CompOrNull(speaker);

        _checkedLanguages.Clear();
        foreach (var language in recipient.Comp.UnderstoodLanguages)
        {
            _checkedLanguages.Add(language);

            // If `speaker` is able to learn a language understood by `recipient`.
            if (speakerLearning?.LearnableLanguages.Contains(language) == true)
                return true;
        }
        foreach (var language in speaker.Comp.UnderstoodLanguages)
        {
            // If this language is already present in the list from `recipient`.
            if (!_checkedLanguages.Add(language))
                // both sides understand the language!
                return true;

            // If `recipient` is able to learn a language understood by `speaker`.
            if (recipientLearning?.LearnableLanguages.Contains(language) == true)
                return true;
        }

        // If neither side directly understand each other's languages and can't learn them either, check if there's any languages with the same `Category`.
        // (E.g. English and German are both `LanguageCategory.Humanoid`, so should be visible to each other)
        return _checkedLanguages
            .Where(_prototypeManager.HasIndex)
            .Select(_prototypeManager.Index)
            .GroupBy(lang => lang.Category)
            .Any(group => group.Count() > 1);
        // (convert `ProtoId<LanguagePrototype>` -> `LanguagePrototype`, group them together by their `Category`, then check if any group has more than 1 member)
    }

    private void OnInitLanguageSpeaker(Entity<LanguageComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Preset is { } presetId && presetId.TryGet(out var preset, _prototypeManager, _compFactory))
        {
            ent.Comp.SpokenLanguages = new(preset.SpokenLanguages);
            ent.Comp.UnderstoodLanguages = new(preset.UnderstoodLanguages);
            ent.Comp.CurrentLanguage ??= preset.CurrentLanguage;
            ent.Comp.DefaultLanguage ??= preset.DefaultLanguage;
        }

        if (ent.Comp.CurrentLanguage == null)
            ent.Comp.CurrentLanguage = ent.Comp.DefaultLanguage ?? ent.Comp.SpokenLanguages.FirstOrDefault();

        UpdateEntityLanguages(ent.AsNullable());
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        ReseedObfuscationForRound();
    }

    private void OnClientSetLanguage(LanguagesSetMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } uid)
            return;

        if (!TryComp<LanguageComponent>(uid, out var component))
            return;

        if (!CanSpeak(uid, message.CurrentLanguage))
            return;

        SetLanguage(uid, message.CurrentLanguage);
    }

    public void SetLanguage(Entity<LanguageComponent?> ent, ProtoId<LanguagePrototype> language)
    {
        if (!CanSpeak(ent, language) || !Resolve(ent, ref ent.Comp) || ent.Comp.CurrentLanguage == language)
            return;

        ent.Comp.CurrentLanguage = language;
        var update = new LanguagesUpdateEvent();
        RaiseLocalEvent(ent, ref update, true);
        Dirty(ent);
    }

    public void AddLanguage(
        EntityUid uid,
        ProtoId<LanguagePrototype> language,
        bool addSpoken = true,
        bool addUnderstood = true)
    {
        if (!TryComp<LanguageComponent>(uid, out var component))
            return;

        if (addSpoken && !component.SpokenLanguages.Contains(language))
            component.SpokenLanguages.Add(language);

        if (addUnderstood && !component.UnderstoodLanguages.Contains(language))
            component.UnderstoodLanguages.Add(language);

        UpdateEntityLanguages((uid, component));
    }

    public void RemoveLanguage(
        Entity<LanguageComponent?> ent,
        ProtoId<LanguagePrototype> language,
        bool removeSpoken = true,
        bool removeUnderstood = true)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (removeSpoken)
            ent.Comp.SpokenLanguages.Remove(language);

        if (removeUnderstood)
            ent.Comp.UnderstoodLanguages.Remove(language);

        UpdateEntityLanguages(ent.Owner);
    }

    public bool TryFixCurrentLanguage(Entity<LanguageComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.CurrentLanguage == null ||
            !ent.Comp.SpokenLanguages.Contains(ent.Comp.CurrentLanguage.Value))
        {
            ent.Comp.CurrentLanguage = ent.Comp.DefaultLanguage ?? ent.Comp.SpokenLanguages.FirstOrDefault();
            var update = new LanguagesUpdateEvent();
            RaiseLocalEvent(ent, ref update);
            Dirty(ent);
            return true;
        }

        return false;
    }

    public void UpdateEntityLanguages(Entity<LanguageComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new DetermineEntityLanguagesEvent();

        foreach (var spoken in ent.Comp.SpokenLanguages)
            ev.SpokenLanguages.Add(spoken);

        foreach (var understood in ent.Comp.UnderstoodLanguages)
            ev.UnderstoodLanguages.Add(understood);

        RaiseLocalEvent(ent, ref ev);

        ent.Comp.SpokenLanguages.Clear();
        ent.Comp.UnderstoodLanguages.Clear();

        ent.Comp.SpokenLanguages.UnionWith(ev.SpokenLanguages);
        ent.Comp.UnderstoodLanguages.UnionWith(ev.UnderstoodLanguages);

        if (!TryFixCurrentLanguage(ent))
        {
            var update = new LanguagesUpdateEvent();
            RaiseLocalEvent(ent, ref update);
        }

        Dirty(ent);
    }

    public string ObfuscateMessageForSpeaker(EntityUid speaker, string message, ProtoId<LanguagePrototype> language)
    {
        if (CanUnderstand(speaker, language))
            return message;

        if (TryComp<LanguageLearningComponent>(speaker, out var learningComp) &&
            learningComp.Languages.ContainsKey(language))
        {
            return _learning.ProcessMessageForSpeaker(speaker, message, language);
        }

        var languageLearningEv = new ProcessSpeakerLanguageEvent(speaker, language, message);
        RaiseLocalEvent(speaker, ref languageLearningEv);
        return languageLearningEv.ProcessedMessage;
    }

    public string ObfuscateMessageForListener(EntityUid listener, string speakerMessage, ProtoId<LanguagePrototype> language)
    {
        if (CanUnderstand(listener, language))
            return speakerMessage;

        if (TryComp<LanguageLearningComponent>(listener, out var learningComp) &&
            learningComp.Languages.ContainsKey(language))
        {
            return _learning.ProcessMessageForListener(listener, speakerMessage, language);
        }

        return ObfuscateMessage(speakerMessage, language);
    }
}
