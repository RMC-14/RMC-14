using Content.Shared._RMC14.Overwatch;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Map;

namespace Content.Client._RMC14.Overwatch;

public sealed class OverwatchConsoleSystem : SharedOverwatchConsoleSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<(Entity<AudioComponent, OverwatchRelayedSoundComponent> Audio, EntityCoordinates Position)> _toRelay = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OverwatchConsoleComponent, AfterAutoHandleStateEvent>(OnOverwatchAfterState);

        SubscribeLocalEvent<OverwatchRelayedSoundComponent, ComponentRemove>(OnRelayedRemove);
        SubscribeLocalEvent<OverwatchRelayedSoundComponent, EntityTerminatingEvent>(OnRelayedRemove);
    }

    private void OnOverwatchAfterState(Entity<OverwatchConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is OverwatchConsoleBui overwatchUi)
                    overwatchUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(OverwatchConsoleBui)}\n{e}");
        }
    }

    private void OnRelayedRemove<T>(Entity<OverwatchRelayedSoundComponent> ent, ref T args)
    {
        TryDeleteRelayed(ent.Comp.Relay);
    }

    private void TryDeleteRelayed(EntityUid? relay)
    {
        if (relay == null)
            return;

        if (IsClientSide(relay.Value))
            QueueDel(relay);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalEntity is not { } player ||
            !HasComp<OverwatchWatchingComponent>(player) ||
            !TryComp(player, out TransformComponent? playerTransform))
        {
            var relayQuery = AllEntityQuery<OverwatchRelayedSoundComponent>();
            while (relayQuery.MoveNext(out var uid, out var relay))
            {
                TryDeleteRelayed(relay.Relay);
                RemCompDeferred<OverwatchRelayedSoundComponent>(uid);
            }

            return;
        }

        _toRelay.Clear();

        var eyePosition = _eye.CurrentEye.Position;
        var playerCoords = playerTransform.Coordinates;
        var query = AllEntityQuery<AudioComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var audio, out var xform))
        {
            var audioCoords = xform.Coordinates;
            if (!audioCoords.TryDelta(EntityManager, _transform, playerCoords, out var delta))
                continue;

            if (eyePosition.MapId == xform.MapID &&
                delta.LengthSquared() <= audio.MaxDistance * audio.MaxDistance)
            {
                RemCompDeferred<OverwatchRelayedSoundComponent>(uid);
                continue;
            }

            var position = eyePosition.Offset(delta);
            var relayed = EnsureComp<OverwatchRelayedSoundComponent>(uid);
            if (relayed.Relay != null && !TerminatingOrDeleted(relayed.Relay))
            {
                _transform.SetMapCoordinates(relayed.Relay.Value, position);
                continue;
            }

            var entityPosition = _transform.ToCoordinates(position);
            _toRelay.Add(((uid, audio, relayed), entityPosition));
        }

        foreach (var (audio, coordinates) in _toRelay)
        {
            var relayedAudio = _audio.PlayStatic(
                new SoundPathSpecifier(audio.Comp1.FileName),
                player,
                coordinates,
                audio.Comp1.Params
            );

            if (relayedAudio is not { Entity: var relayedAudioEnt })
                continue;

            _audio.SetPlaybackPosition(relayedAudioEnt, audio.Comp1.PlaybackPosition);
            audio.Comp2.Relay = relayedAudioEnt;
        }
    }
}
