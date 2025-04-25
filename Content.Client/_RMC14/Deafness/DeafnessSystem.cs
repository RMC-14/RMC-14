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

            var lastsFor = (float)(time.Value.Item2 - time.Value.Item1).TotalSeconds;
            var timeDone = (float)(curTime - time.Value.Item1).TotalSeconds;

            if (lastsFor < 0.3f) // So the audio doesn't immediately stop when you get the effect, needs to last atleast 0.3 seconds
                continue;

            _audio.SetMasterGain(0f);

            if (lastsFor - timeDone <= 1.5f)
            {
                var fadeInSpeed = 1.5f - (lastsFor - timeDone);
                var newVolume = fadeInSpeed * _originalVolume;
                _audio.SetMasterGain(newVolume);
            }
        }
    }
}
