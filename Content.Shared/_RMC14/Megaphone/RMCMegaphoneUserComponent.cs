using Content.Shared.Speech;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Megaphone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCMegaphoneUserComponent : Component
{
    /// <summary>
    /// The sound played when the megaphone is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SpeechSoundsPrototype> MegaphoneSpeechSound = "RMCMegaphone";

    /// <summary>
    /// The verb used when the megaphone is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SpeechVerbPrototype> SpeechVerb = "Megaphone";

    /// <summary>
    /// The original verb used before the megaphone was used.
    /// Needed to restore the original verb when the megaphone is removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SpeechVerbPrototype>? OriginalSpeechVerb;

    /// <summary>
    /// The original sounds used before the megaphone was used.
    /// Needed to restore the original sound when the megaphone is removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SpeechSoundsPrototype>? OriginalSpeechSounds;

    /// <summary>
    /// The original suffix speech verbs used before the megaphone was used.
    /// Needed to restore the original verbs when the megaphone is removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, ProtoId<SpeechVerbPrototype>>? OriginalSuffixSpeechVerbs;

    /// <summary>
    /// Override the default suffix speech verbs to use megaphone verbs.
    /// Allows to clearly record the options used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, ProtoId<SpeechVerbPrototype>> SuffixSpeechVerbs = new()
    {
        { "chat-speech-verb-suffix-exclamation-strong", "Megaphone" },
        { "chat-speech-verb-suffix-exclamation", "Megaphone" },
        { "chat-speech-verb-suffix-question", "Megaphone" },
        { "chat-speech-verb-suffix-stutter", "Megaphone" },
        { "chat-speech-verb-suffix-mumble", "Megaphone" },
    };
}
