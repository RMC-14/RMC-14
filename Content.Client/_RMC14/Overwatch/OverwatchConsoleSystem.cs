using System.Numerics;
using Content.Shared._RMC14.Overwatch;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Input.Binding;
using Robust.Client.Input;
using Robust.Shared.Map;
using Content.Client.Movement.Components;
using Content.Shared._RMC14.Input;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Client._RMC14.Overwatch;

public sealed class OverwatchConsoleSystem : SharedOverwatchConsoleSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] public readonly IPlayerManager _player = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<(Entity<AudioComponent, OverwatchRelayedSoundComponent> Audio, EntityCoordinates Position)> _toRelay = new();

    private Vector2? _overwatchTargetOffset = null;
    private readonly Vector2 _offsetLimit = new(offsetAmount, offsetAmount);
    private EntityUid? _overwatchActor = null;
    private OverwatchDirection? _pendingOffsetDirection = null;
    private const float offsetAmount = 10f;
    private const float zoomAmount = 1.5f;
    private string? _previousContext;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OverwatchConsoleComponent, AfterAutoHandleStateEvent>(OnOverwatchAfterState);

        SubscribeLocalEvent<OverwatchCameraAdjustOffsetMsg>(OnCameraAdjustOffset);
        SubscribeLocalEvent<OverwatchRelayedSoundComponent, ComponentRemove>(OnRelayedRemove);
        SubscribeLocalEvent<OverwatchRelayedSoundComponent, EntityTerminatingEvent>(OnRelayedRemove);

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCCameraAdjustNorth,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (_player.LocalEntity is { } player)
                    {
                        var netEntity = EntityManager.GetNetEntity(player);
                        OnCameraAdjustOffset(new OverwatchCameraAdjustOffsetMsg(netEntity, OverwatchDirection.North));
                        RaiseNetworkEvent(new OverwatchCameraAdjustOffsetEvent(netEntity, OverwatchDirection.North));
                    }
                }, handle: false))
            .Bind(CMKeyFunctions.RMCCameraAdjustWest,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (_player.LocalEntity is { } player)
                    {
                        var netEntity = EntityManager.GetNetEntity(player);
                        OnCameraAdjustOffset(new OverwatchCameraAdjustOffsetMsg(netEntity, OverwatchDirection.West));
                        RaiseNetworkEvent(new OverwatchCameraAdjustOffsetEvent(netEntity, OverwatchDirection.West));
                    }
                }, handle: false))
            .Bind(CMKeyFunctions.RMCCameraAdjustSouth,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (_player.LocalEntity is { } player)
                    {
                        var netEntity = EntityManager.GetNetEntity(player);
                        OnCameraAdjustOffset(new OverwatchCameraAdjustOffsetMsg(netEntity, OverwatchDirection.South));
                        RaiseNetworkEvent(new OverwatchCameraAdjustOffsetEvent(netEntity, OverwatchDirection.South));
                    }
                }, handle: false))
            .Bind(CMKeyFunctions.RMCCameraAdjustEast,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (_player.LocalEntity is { } player)
                    {
                        var netEntity = EntityManager.GetNetEntity(player);
                        OnCameraAdjustOffset(new OverwatchCameraAdjustOffsetMsg(netEntity, OverwatchDirection.East));
                        RaiseNetworkEvent(new OverwatchCameraAdjustOffsetEvent(netEntity, OverwatchDirection.East));
                    }
                }, handle: false))
            .Bind(CMKeyFunctions.RMCCameraReset,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (_player.LocalEntity is { } player)
                    {
                        var netEntity = EntityManager.GetNetEntity(player);
                        OnCameraAdjustOffset(new OverwatchCameraAdjustOffsetMsg(netEntity, OverwatchDirection.Reset));
                        RaiseNetworkEvent(new OverwatchCameraAdjustOffsetEvent(netEntity, OverwatchDirection.Reset));
                    }
                }, handle: false))
            .Register<OverwatchConsoleSystem>();
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

    protected override void Watch(Entity<ActorComponent?, EyeComponent?> watcher, Entity<OverwatchCameraComponent?> toWatch)
    {
        base.Watch(watcher, toWatch);

        // Add overwatch context to enable offset control keybinds
        if (_player.LocalEntity is { } local && watcher.Owner == local)
        {
            var contexts = _input?.Contexts;
            if (contexts != null)
            {
                _previousContext = contexts.ActiveContext?.Name;
                if (contexts.Exists("overwatch"))
                    contexts.SetActiveContext("overwatch");
            }
        }
    }

    protected override void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        ResetOverwatchOffset();
        base.Unwatch(watcher, player);

        // Restore previous context, preventing offset control keybinds from interfering with normal movement
        if (_player.LocalEntity is { } local && watcher.Owner == local)
        {
            var contexts = _input?.Contexts;
            if (contexts != null)
            {
                if (!string.IsNullOrEmpty(_previousContext) && contexts.Exists(_previousContext))
                {
                    contexts.SetActiveContext(_previousContext);
                    _previousContext = null;
                }
                else if (contexts.Exists("human"))
                {
                    contexts.SetActiveContext("human");
                }
            }
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
            ResetOverwatchOffset();
            var relayQuery = AllEntityQuery<OverwatchRelayedSoundComponent>();
            while (relayQuery.MoveNext(out var uid, out var relay))
            {
                TryDeleteRelayed(relay.Relay);
                RemCompDeferred<OverwatchRelayedSoundComponent>(uid);
            }

            return;
        }

        _overwatchActor = player;
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

        if (_overwatchActor != null)
        {
            var comp = EnsureComp<EyeCursorOffsetComponent>(_overwatchActor.Value);
            // Disable mouse offset logic to stop the eye from panning towards the mouse
            comp.DisableMouseOffset = true;
            comp.TargetPosition = _overwatchTargetOffset ?? Vector2.Zero;
            comp.CurrentPosition = comp.TargetPosition;
            Dirty(_overwatchActor.Value, comp);
            var eyeSystem = EntitySystem.Get<SharedContentEyeSystem>();
            // Set zoom back to default if there's no active offset
            float zoom = (comp.CurrentPosition != Vector2.Zero) ? zoomAmount : 1.0f;
            if (_player.LocalEntity != null && TryComp(_player.LocalEntity.Value, out ContentEyeComponent? eyeComp))
                eyeSystem.SetZoom(_player.LocalEntity.Value, new Vector2(zoom, zoom), ignoreLimits: true, eye: eyeComp);

            if (_pendingOffsetDirection != null && comp.CurrentPosition == Vector2.Zero)
            {
                var netEntity = EntityManager.GetNetEntity(_overwatchActor.Value);
                OnCameraAdjustOffset(new OverwatchCameraAdjustOffsetMsg(netEntity, _pendingOffsetDirection.Value));
                _pendingOffsetDirection = null;
            }
        }
    }

    private void OnCameraAdjustOffset(OverwatchCameraAdjustOffsetMsg msg)
    {
        if (!TryGetEntity(msg.Actor, out var actorUid) || _overwatchActor != actorUid)
            return;

        Vector2 offsetDelta = msg.Direction switch
        {
            OverwatchDirection.North => new Vector2(0, offsetAmount),
            OverwatchDirection.South => new Vector2(0, -offsetAmount),
            OverwatchDirection.East => new Vector2(offsetAmount, 0),
            OverwatchDirection.West => new Vector2(-offsetAmount, 0),
            _ => Vector2.Zero
        };

        // If there's no offset, return zoom back to default
        if (offsetDelta == Vector2.Zero)
        {
            _overwatchTargetOffset = Vector2.Zero;
            var comp = EnsureComp<EyeCursorOffsetComponent>(_overwatchActor.Value);
            comp.CurrentPosition = Vector2.Zero;
            comp.TargetPosition = Vector2.Zero;
            Dirty(_overwatchActor.Value, comp);
            var eyeSystem = EntitySystem.Get<SharedContentEyeSystem>();
            return;
        }
        _overwatchTargetOffset = offsetDelta;

        var clamped = new Vector2(
            Math.Clamp(_overwatchTargetOffset.Value.X, -_offsetLimit.X, _offsetLimit.X),
            Math.Clamp(_overwatchTargetOffset.Value.Y, -_offsetLimit.Y, _offsetLimit.Y)
        );
        _overwatchTargetOffset = clamped;

        if (_overwatchActor != null)
        {
            var comp = EnsureComp<EyeCursorOffsetComponent>(_overwatchActor.Value);
            Dirty(_overwatchActor.Value, comp);
            var eyeSystem = EntitySystem.Get<SharedContentEyeSystem>();
            if (_player.LocalEntity != null && TryComp(_player.LocalEntity.Value, out ContentEyeComponent? eyeComp))
                eyeSystem.SetZoom(_player.LocalEntity.Value, new Vector2(zoomAmount, zoomAmount), ignoreLimits: true, eye: eyeComp);
        }
    }

    // Applies offset from UI controls immediately, makes the zoom transition smoother
    public void ApplyCameraOffset(NetEntity actorNetEntity, OverwatchDirection direction)
    {
        OnCameraAdjustOffset(new OverwatchCameraAdjustOffsetMsg(actorNetEntity, direction));
    }

    private void ResetOverwatchOffset()
    {
        if (_overwatchActor == null)
            return;

        if (TerminatingOrDeleted(_overwatchActor.Value))
        {
            _overwatchTargetOffset = null;
            _overwatchActor = null;
            return;
        }

        var comp = EnsureComp<EyeCursorOffsetComponent>(_overwatchActor.Value);
        comp.CurrentPosition = Vector2.Zero;
        comp.TargetPosition = Vector2.Zero;
        comp.DisableMouseOffset = true;
        Dirty(_overwatchActor.Value, comp);
        var eyeSystem = EntitySystem.Get<SharedContentEyeSystem>();
        _overwatchTargetOffset = null;
        _overwatchActor = null;
    }
}
