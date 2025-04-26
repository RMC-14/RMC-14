using Content.Client.Popups;
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
    [Dependency] private readonly PopupSystem _popup = default!;

    private float _originalVolume = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeafComponent, ComponentInit>(OnDeafInit);
        SubscribeLocalEvent<DeafComponent, ComponentShutdown>(OnDeafShutdown);

        SubscribeLocalEvent<DeafComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DeafComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnDeafInit(EntityUid uid, DeafComponent component, ComponentInit args)
    {
        SetOriginalVolume();
    }

    private void OnDeafShutdown(EntityUid uid, DeafComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
            _audio.SetMasterGain(_originalVolume);
    }

    private void OnPlayerAttached(EntityUid uid, DeafComponent component, LocalPlayerAttachedEvent args)
    {
        SetOriginalVolume();
    }

    private void OnPlayerDetached(EntityUid uid, DeafComponent component, LocalPlayerDetachedEvent args)
    {
        _audio.SetMasterGain(_originalVolume);
    }

    private void SetOriginalVolume()
    {
        _originalVolume = _cfg.GetCVar(CCVars.AudioMasterVolume);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalEntity is not { } player)
            return;

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<DeafComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (player != uid)
                continue;

            if (!_statusEffects.TryGetTime(player, DeafKey, out var time))
                continue;

            var statusTime = time.Value;

            var lastsFor = (float)(statusTime.Item2 - statusTime.Item1).TotalSeconds;
            var timeLeft = (float)(statusTime.Item2 - curTime).TotalSeconds;
            var timeDone = (float)(curTime - statusTime.Item1).TotalSeconds;

            var volume = 0f;

            var fadeOutDuration = Math.Clamp(lastsFor * 0.35f, 0.2f, 2f);
            var fadeInDuration = Math.Clamp(lastsFor * 0.15f, 0.1f, 1f);

            if (timeDone <= 2f) // Fade out during two seconds of deafness
            {
                var fadeOut = 1f - timeDone / fadeOutDuration;
                volume = fadeOut * _originalVolume;
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
