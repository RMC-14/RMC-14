using Content.Server.Administration.Logs;
using Content.Shared._RMC14.Language;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Content.Shared.Database;
using Content.Server.GameTicking.Events;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Text;

namespace Content.Server._RMC14.Language.Systems;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActorSystem _actorSystem = default!;
    [Dependency] private readonly LanguageLearningSystem _languageLearning = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageComponent, MapInitEvent>(OnInitLanguageSpeaker);
        SubscribeLocalEvent<LanguageComponent, ComponentGetState>(OnGetLanguageState);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeNetworkEvent<LanguagesSetMessage>(OnClientSetLanguage);
    }

    private void OnGetLanguageState(Entity<LanguageComponent> entity, ref ComponentGetState args)
    {
        args.State = new LanguageComponent.State(
            entity.Comp.CurrentLanguage,
            entity.Comp.SpokenLanguages.ToList(),
            entity.Comp.UnderstoodLanguages.ToList()
        );
    }

    private void OnInitLanguageSpeaker(Entity<LanguageComponent> ent, ref MapInitEvent args)
    {
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
        RaiseLocalEvent(ent, new LanguagesUpdateEvent(), true);
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
            RaiseLocalEvent(ent, new LanguagesUpdateEvent());
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
            RaiseLocalEvent(ent, new LanguagesUpdateEvent());

        Dirty(ent);
    }

    public string ObfuscateMessageForSpeaker(EntityUid speaker, string message, ProtoId<LanguagePrototype> language)
    {
        if (CanUnderstand(speaker, language))
            return message;

        if (TryComp<LanguageLearningComponent>(speaker, out var learningComp) &&
            learningComp.LearnableLanguages.Contains(language))
        {
            return _languageLearning.ProcessMessageForSpeaker(speaker, message, language);
        }

        var languageLearningEv = new ProcessSpeakerLanguageEvent(speaker, message, language);
        RaiseLocalEvent(speaker, ref languageLearningEv);
        return languageLearningEv.ProcessedMessage;
    }

    public string ObfuscateMessageForListener(EntityUid listener, string speakerMessage, ProtoId<LanguagePrototype> language)
    {

        if (CanUnderstand(listener, language))
        {
            return speakerMessage;
        }

        if (TryComp<LanguageLearningComponent>(listener, out var learningComp) &&
            learningComp.LearnableLanguages.Contains(language))
        {
            var result = _languageLearning.ProcessMessageForListener(listener, speakerMessage, language);
            return result;
        }

        var obfuscated = ObfuscateMessage(speakerMessage, language);
        return obfuscated;
    }
}

[ByRefEvent]
public struct DetermineEntityLanguagesEvent
{
    public HashSet<ProtoId<LanguagePrototype>> SpokenLanguages;
    public HashSet<ProtoId<LanguagePrototype>> UnderstoodLanguages;

    public DetermineEntityLanguagesEvent()
    {
        SpokenLanguages = new HashSet<ProtoId<LanguagePrototype>>();
        UnderstoodLanguages = new HashSet<ProtoId<LanguagePrototype>>();
    }
}

[ByRefEvent]
public struct ProcessSpeakerLanguageEvent
{
    public EntityUid Speaker;
    public ProtoId<LanguagePrototype> Language;
    public string ProcessedMessage;

    public ProcessSpeakerLanguageEvent(EntityUid speaker, string message, ProtoId<LanguagePrototype> language)
    {
        Speaker = speaker;
        ProcessedMessage = message;
        Language = language;
    }
}
