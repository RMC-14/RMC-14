using Content.Shared._RMC14.Pulling;
using Content.Shared.Gravity;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._RMC14.Sound;

public sealed class CMSoundSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEmitSoundSystem _emitSound = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCEmitSoundOnSpawnComponent, MapInitEvent>(OnEmitSpawnOnInit);
        SubscribeLocalEvent<RandomSoundComponent, MapInitEvent>(OnRandomMapInit);

        SubscribeLocalEvent<SoundOnDeathComponent, MobStateChangedEvent>(OnDeathMobStateChanged);
        SubscribeLocalEvent<SoundOnDeathComponent, EntityTerminatingEvent>(OnDeathMobTerminating);

        SubscribeLocalEvent<SoundOnDeathSoundComponent, EntityTerminatingEvent>(OnDeathSoundTerminating);

        SubscribeLocalEvent<EmitSoundOnActionComponent, SoundActionEvent>(OnEmitSoundOnAction);

        SubscribeLocalEvent<SoundOnDragComponent, PullStartedMessage>(OnSoundOnDragPullStarted);
    }

    private void OnEmitSpawnOnInit(Entity<RMCEmitSoundOnSpawnComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Sound == null)
            return;

        ent.Comp.Entity = _audio.PlayPvs(ent.Comp.Sound, ent.Owner)?.Entity;

        var coordinates = _transform.GetMoverCoordinates(ent);
        if (TerminatingOrDeleted(coordinates.EntityId))
            return;

        if (ent.Comp.Entity == null)
            return;

        _transform.SetCoordinates(ent.Comp.Entity.Value, coordinates);
        QueueDel(ent.Owner);
    }

    private void OnRandomMapInit(Entity<RandomSoundComponent> ent, ref MapInitEvent args)
    {
        var min = ent.Comp.Min;
        var max = ent.Comp.Max;
        if (max <= min)
            max = min.Add(TimeSpan.FromTicks(1));

        ent.Comp.PlayAt = _timing.CurTime + _random.Next(min, max);
        Dirty(ent);
    }

    private void OnDeathMobStateChanged(Entity<SoundOnDeathComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!_net.IsServer)
            return;

        ent.Comp.Entity = _audio.PlayPvs(ent.Comp.Sound, ent)?.Entity;
        Dirty(ent);
    }

    private void OnDeathMobTerminating(Entity<SoundOnDeathComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Entity == null ||
            TerminatingOrDeleted(ent.Comp.Entity))
        {
            return;
        }

        var coordinates = _transform.GetMoverCoordinates(ent);
        if (TerminatingOrDeleted(coordinates.EntityId))
            return;

        _transform.SetCoordinates(ent.Comp.Entity.Value, coordinates);
        ent.Comp.Entity = null;
    }

    private void OnDeathSoundTerminating(Entity<SoundOnDeathSoundComponent> ent, ref EntityTerminatingEvent args)
    {
        var parent = ent.Comp.Parent;
        ent.Comp.Parent = null;

        if (!TryComp(parent, out SoundOnDeathComponent? death))
            return;

        death.Entity = null;
        Dirty(parent.Value, death);
    }

    private void OnEmitSoundOnAction(Entity<EmitSoundOnActionComponent> ent, ref SoundActionEvent args)
    {
        _emitSound.TryEmitSound(ent, ent, args.Performer);

        if (ent.Comp.Handle)
            args.Handled = true;
    }

    private void OnSoundOnDragPullStarted(Entity<SoundOnDragComponent> ent, ref PullStartedMessage args)
    {
        ent.Comp.DragSoundDistance = 0;
        ent.Comp.LastPosition = Transform(ent).Coordinates;
        ent.Comp.LastSoundTime = _timing.CurTime;
    }

    private bool TryGetSound(
        EntityUid uid,
        SoundOnDragComponent soundOnDrag,
        TransformComponent xform,
        [NotNullWhen(true)] out SoundSpecifier? sound)
    {
        sound = null;

        if (!_timing.IsFirstTimePredicted
            || !_timing.InSimulation
            || _gravity.IsWeightless(uid))
            return false;

        var coordinates = xform.Coordinates;
        var distanceNeeded = 1.0f; // TODO RMC14

        // Can happen when teleporting between grids.
        if (!coordinates.TryDistance(EntityManager, soundOnDrag.LastPosition, out var distance) ||
            distance > distanceNeeded)
        {
            soundOnDrag.DragSoundDistance = distanceNeeded;
        }
        else
        {
            soundOnDrag.DragSoundDistance += distance;
        }

        soundOnDrag.LastPosition = coordinates;

        if (soundOnDrag.DragSoundDistance < distanceNeeded)
            return false;

        soundOnDrag.DragSoundDistance -= distanceNeeded;

        sound = soundOnDrag.Sound;
        return sound != null;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var random = EntityQueryEnumerator<RandomSoundComponent>();
        while (random.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsDead(uid) || time <= comp.PlayAt)
                continue;

            comp.PlayAt = time + _random.Next(comp.Min, comp.Max);
            Dirty(uid, comp);

            _audio.PlayPvs(comp.Sound, uid);
        }

        var soundOnDrag = EntityQueryEnumerator<SoundOnDragComponent, BeingPulledComponent>();
        while (soundOnDrag.MoveNext(out var uid, out var comp, out var _))
        {
            if (!TryGetSound(uid, comp, Transform(uid), out var sound))
                continue;

            var timeFromLastSound = _timing.CurTime - comp.LastSoundTime;
            // Pitch up or down slightly based on roughly how fast the drag is.
            var pitchAdjustment = (float)Math.Clamp(13 / (12 + timeFromLastSound.TotalSeconds) - 1, -.05, .05);

            comp.LastSoundTime = _timing.CurTime;

            var audioParams = sound.Params
                .WithPitchScale(1 + pitchAdjustment);

            _audio.PlayPredicted(sound, uid, uid, audioParams);
        }
    }
}
