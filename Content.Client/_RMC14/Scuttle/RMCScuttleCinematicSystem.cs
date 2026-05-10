using Content.Shared._RMC14.Rules;
using Content.Shared.GameTicking;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Scuttle;

public sealed class RMCScuttleCinematicSystem : EntitySystem
{
    private static readonly SoundSpecifier CinematicExplosionSound =
        new SoundCollectionSpecifier("Explosion", AudioParams.Default.WithVolume(4f));

    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private RMCScuttleCinematicOverlay? _current;
    private TimeSpan? _cinematicExplosionAt;
    private bool _cinematicExplosionPlayed;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RMCScuttleCinematicEvent>(OnScuttleCinematic);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_current?.Finished == true)
        {
            Clear();
            return;
        }

        if (_current != null &&
            !_cinematicExplosionPlayed &&
            _cinematicExplosionAt is { } explosionAt &&
            _timing.CurTime >= explosionAt)
        {
            _cinematicExplosionPlayed = true;
            _audio.PlayGlobal(CinematicExplosionSound, Filter.Local(), true);
        }
    }

    private void OnScuttleCinematic(RMCScuttleCinematicEvent ev)
    {
        Clear();

        if (ev.Duration <= TimeSpan.Zero)
            return;

        _current = new RMCScuttleCinematicOverlay(_timing.CurTime, ev.Duration);
        _cinematicExplosionAt = _timing.CurTime + GetExplosionOffset(ev.Duration);
        _cinematicExplosionPlayed = false;
        _overlay.AddOverlay(_current);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        Clear();
    }

    private void Clear()
    {
        if (_current == null)
            return;

        _overlay.RemoveOverlay<RMCScuttleCinematicOverlay>();
        _current = null;
        _cinematicExplosionAt = null;
        _cinematicExplosionPlayed = false;
    }

    private static TimeSpan GetExplosionOffset(TimeSpan duration)
    {
        var introShip = TimeSpan.FromSeconds(Math.Min(1.5, duration.TotalSeconds * 0.1));
        var introNuke = TimeSpan.FromSeconds(Math.Min(3.0, duration.TotalSeconds * 0.2));
        return introShip + introNuke;
    }
}
