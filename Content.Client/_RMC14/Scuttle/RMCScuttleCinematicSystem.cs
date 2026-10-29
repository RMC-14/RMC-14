using Content.Client.Popups;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared._RMC14.Rules;
using Content.Shared.GameTicking;
using Robust.Shared.Audio.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
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
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private RMCScuttleCinematicOverlay? _current;
    private TimeSpan? _cinematicExplosionAt;
    private bool _cinematicExplosionPlayed;
    private readonly Dictionary<EntityUid, float> _mutedAudioGains = new();
    private readonly HashSet<EntityUid> _cinematicAudio = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RMCScuttleCinematicEvent>(OnScuttleCinematic);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<AudioComponent, ComponentAdd>(OnAudioAdd);
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
            AllowCinematicAudio(_audio.PlayGlobal(CinematicExplosionSound, Filter.Local(), true));
        }
    }

    private void OnScuttleCinematic(RMCScuttleCinematicEvent ev)
    {
        Clear();

        if (ev.Duration <= TimeSpan.Zero)
            return;

        var startedAt = ev.StartedAt > _timing.CurTime ? _timing.CurTime : ev.StartedAt;
        _current = new RMCScuttleCinematicOverlay(startedAt, ev.Duration);
        _cinematicExplosionAt = startedAt + RMCScuttleCinematicTiming.GetExplosionOffset(ev.Duration);
        _cinematicExplosionPlayed = false;
        _overlay.AddOverlay(_current);
        SetCinematicUiSuppressed(true);
        MuteExistingAudio();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        Clear();
    }

    private void OnAudioAdd(Entity<AudioComponent> ent, ref ComponentAdd args)
    {
        if (_current == null || _cinematicAudio.Contains(ent.Owner))
            return;

        MuteAudio(ent);
    }

    private void Clear()
    {
        SetCinematicUiSuppressed(false);
        RestoreMutedAudio();

        if (_current != null)
            _overlay.RemoveOverlay<RMCScuttleCinematicOverlay>();

        _current = null;
        _cinematicExplosionAt = null;
        _cinematicExplosionPlayed = false;
        _cinematicAudio.Clear();
    }

    private void SetCinematicUiSuppressed(bool suppressed)
    {
        _ui.GetUIController<ChatUIController>().SetSpeechBubblesSuppressed(suppressed);
        _popup.SetPopupsSuppressed(suppressed);
    }

    private void AllowCinematicAudio((EntityUid Entity, AudioComponent Component)? audio)
    {
        if (audio is not { } playing)
            return;

        _cinematicAudio.Add(playing.Entity);
        if (_mutedAudioGains.Remove(playing.Entity, out var gain))
            _audio.SetGain(playing.Entity, gain, playing.Component);
    }

    private void MuteExistingAudio()
    {
        var query = EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var uid, out var audio))
        {
            if (_cinematicAudio.Contains(uid))
                continue;

            MuteAudio((uid, audio));
        }
    }

    private void MuteAudio(Entity<AudioComponent> ent)
    {
        if (!_mutedAudioGains.ContainsKey(ent.Owner))
            _mutedAudioGains[ent.Owner] = ent.Comp.Gain;

        _audio.SetGain(ent.Owner, 0f, ent.Comp);
    }

    private void RestoreMutedAudio()
    {
        foreach (var (uid, gain) in _mutedAudioGains)
        {
            if (TryComp(uid, out AudioComponent? audio))
                _audio.SetGain(uid, gain, audio);
        }

        _mutedAudioGains.Clear();
    }
}
