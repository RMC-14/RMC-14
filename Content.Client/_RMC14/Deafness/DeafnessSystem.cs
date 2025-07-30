using Content.Shared._RMC14.Deafness;
using Content.Shared.CCVar;
using Content.Shared.StatusEffect;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Deafness;

public sealed class DeafnessSystem : SharedDeafnessSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IAudioManager _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _originalVolume = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeafComponent, ComponentShutdown>(OnDeafShutdown);
        SubscribeLocalEvent<DeafComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        Subs.CVar(_cfg, CCVars.AudioMasterVolume, value => _originalVolume = value, true);
    }

    private void OnDeafShutdown(EntityUid uid, DeafComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
            _audio.SetMasterGain(_originalVolume);
    }

    private void OnPlayerDetached(EntityUid uid, DeafComponent component, LocalPlayerDetachedEvent args)
    {
        _audio.SetMasterGain(_originalVolume);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalEntity is not { } player)
            return;

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<DeafComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (player != uid)
                continue;

            (TimeSpan, TimeSpan)? time = null;
            (TimeSpan, TimeSpan)? time2 = null;

            if (!_statusEffects.TryGetTime(player, DeafKey, out time) && !_statusEffects.TryGetTime(player, "Unconscious", out time2))
                continue;

            if (time2 != null && (time == null || time.Value.Item2 < time2.Value.Item2))
                time = time2.Value;

            if (time == null)
                continue;

            var statusTime = time.Value;

            var lastsFor = (float)(statusTime.Item2 - statusTime.Item1).TotalSeconds;
            var timeLeft = (float)(statusTime.Item2 - curTime).TotalSeconds;
            var timeDone = (float)(curTime - statusTime.Item1).TotalSeconds;

            var volume = 0f;

            var fadeOutDuration = Math.Clamp(lastsFor * 0.35f, 0.2f, 2f);
            var fadeInDuration = Math.Clamp(lastsFor * 0.15f, 0.1f, 1f);

            if (timeDone <= 2f && !comp.DidFadeOut) // Fade out during two seconds of deafness
            {
                var fadeOut = 1f - timeDone / fadeOutDuration;
                volume = fadeOut * _originalVolume;

                if (volume <= 0.1f) // this is so audio doesn't clip out if a status effect refreshes
                {
                    volume = 0f;
                    comp.DidFadeOut = true;
                }
            }
            else if (timeLeft <= 1f) // Fade in during last second of deafness
            {
                var fadeIn = 1f - timeLeft / fadeInDuration;
                volume = fadeIn * _originalVolume;
            }

            volume = Math.Max(0f, volume); // prevents negative volume
            _audio.SetMasterGain(volume);
        }
    }
}
