using System.Linq;
using Content.Server._RMC14.Chat.Chat;
using Content.Server.GameTicking.Events;
using Content.Shared._RMC14.Language;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Language.Systems;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly LanguageLearningSystem _learning = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    private EntityQuery<LanguageComponent> _languageQuery;
    private EntityQuery<LanguageLearningComponent> _learningQuery;

    public override void Initialize()
    {
        base.Initialize();

        _languageQuery = GetEntityQuery<LanguageComponent>();
        _learningQuery = GetEntityQuery<LanguageLearningComponent>();

        SubscribeLocalEvent<LanguageComponent, MapInitEvent>(OnInitLanguageSpeaker);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeNetworkEvent<LanguagesSetMessage>(OnClientSetLanguage);
    }

    /// <summary>
    /// Check whether <paramref name="recipient"/> should be able to see a chat message from <paramref name="speaker"/>
    /// as a popup and in their chat box, based on the message's <paramref name="spokenLanguage"/>.
    /// <para>
    /// This is primarily used to hide speech between different factions.
    /// </para>
    /// </summary>
    /// <remarks>
    /// There is one special case included for if <paramref name="speaker"/> is able to learn <paramref name="recipient"/>'s languages.<br/>
    /// If this is the case then their speech will always be visible in order to flag them as someone of interest. (e.g. Xenos hearing a synth)
    /// </remarks>
    /// <param name="recipient">The entity hearing the message.</param>
    /// <param name="speaker">The entity that created the message.</param>
    /// <param name="spokenLanguage">The message's language.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="spokenLanguage"/> can be understood or learned by <paramref name="recipient"/>,
    /// and <see langword="false"/> otherwise.
    /// </returns>
    public bool CanSeeSpokenMessage(EntityUid recipient, EntityUid speaker, ProtoId<LanguagePrototype> spokenLanguage)
    {
        if (!_prototypeManager.TryIndex(spokenLanguage, out var spokenProto))
            return false;

        // If `recipient` understands the language, or understands a similar language in the same "category" (i.e. english and german)
        if (_languageQuery.TryComp(recipient, out var langComp))
        {
            foreach (var understood in langComp.UnderstoodLanguages)
            {
                if (understood == spokenLanguage)
                    return true;

                if (_prototypeManager.TryIndex(understood, out var understoodProto) &&
                    understoodProto.Category == spokenProto.Category)
                {
                    return true;
                }
            }

            // Special case for if `speaker` is able to learn a language understood by `recipient`.
            if (_learningQuery.TryComp(speaker, out var speakerLearning) &&
                speakerLearning.Languages.Keys.Intersect(langComp.UnderstoodLanguages).Any())
            {
                return true;
            }
        }

        // If `recipient` doesn't understand the language, but is able to learn it.
        if (_learningQuery.TryComp(recipient, out var learningComp) &&
            learningComp.LearnableLanguages.Contains(spokenLanguage))
        {
            return true;
        }

        return false;
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
