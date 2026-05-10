using Robust.Shared.Audio;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
// RMC14
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Xenonids;
// RMC14
using Content.Shared.Speech;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server.Speech
{
    public sealed class SpeechSoundSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        // RMC14
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        // RMC14

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpeechComponent, EntitySpokeEvent>(OnEntitySpoke);
        }

        // RMC14
        public SoundSpecifier? GetSpeechSound(
            Entity<SpeechComponent> ent,
            string message,
            ProtoId<SpeechSoundsPrototype>? speechSoundsOverride = null)
        {
            var speechSounds = speechSoundsOverride ?? ent.Comp.SpeechSounds;
            if (speechSounds == null)
                return null;

            // Play speech sound
            SoundSpecifier? contextSound;
            var prototype = _protoManager.Index<SpeechSoundsPrototype>(speechSounds);

            // Different sounds for ask/exclaim based on last character
            contextSound = message[^1] switch
            {
                '?' => prototype.AskSound,
                '!' => prototype.ExclaimSound,
                _ => prototype.SaySound
            };

            // Use exclaim sound if most characters are uppercase.
            int uppercaseCount = 0;
            for (int i = 0; i < message.Length; i++)
            {
                if (char.IsUpper(message[i]))
                    uppercaseCount++;
            }
            if (uppercaseCount > (message.Length / 2))
            {
                contextSound = prototype.ExclaimSound;
            }

            var scale = (float) _random.NextGaussian(1, prototype.Variation);
            contextSound.Params = ent.Comp.AudioParams.WithPitchScale(scale);
            return contextSound;
        }
        // RMC14

        // RMC14
        private ProtoId<SpeechSoundsPrototype>? GetSpeechSoundOverride(EntityUid uid, ProtoId<LanguagePrototype>? language)
        {
            if (language == null ||
                !_protoManager.TryIndex(language.Value, out var languageProto))
            {
                return null;
            }

            if (HasComp<XenoComponent>(uid))
                return null;

            return languageProto.SpeechOverride.SpeechSoundsOverride;
        }
        // RMC14

        // RMC14
        private void OnEntitySpoke(Entity<SpeechComponent> ent, ref EntitySpokeEvent args)
        {
            var effectiveSpeechSounds = GetSpeechSoundOverride(ent.Owner, args.Language) ?? ent.Comp.SpeechSounds;
            if (effectiveSpeechSounds == null)
                return;

            var currentTime = _gameTiming.CurTime;
            var cooldown = TimeSpan.FromSeconds(ent.Comp.SoundCooldownTime);

            // Ensure more than the cooldown time has passed since last speaking
            if (currentTime - ent.Comp.LastTimeSoundPlayed < cooldown)
                return;

            var sound = GetSpeechSound(ent, args.Message, effectiveSpeechSounds);
            ent.Comp.LastTimeSoundPlayed = currentTime;
            _audio.PlayPvs(sound, ent.Owner);
        }
        // RMC14
    }
}
