using System.Numerics;
using Content.Shared._RMC14.Hallucination;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private readonly List<(EntityUid Uid, TimeSpan End)> _visuals = new();
    private readonly List<MovingVisual> _moving = new();
    private EntityUid? _owner;

    public override void Initialize()
    {
        SubscribeNetworkEvent<RMCHallucinationVisualEvent>(OnVisual);
        SubscribeNetworkEvent<RMCHallucinationMovingVisualEvent>(OnMovingVisual);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        InitializeAttacker();
        InitializeBodies();
        InitializeComms();
        InitializeDanger();
        InitializeHud();
        InitializeXeno();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ClearAll();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        ClearAll();
    }

    private void OnVisual(RMCHallucinationVisualEvent ev)
    {
        var coords = GetCoordinates(ev.Coordinates);
        if (!coords.IsValid(EntityManager))
            return;

        var uid = Spawn(ev.Prototype, coords);
        if (ev.State != null && TryComp(uid, out SpriteComponent? sprite))
            _sprite.LayerSetRsiState((uid, sprite), 0, ev.State);

        TrackOwner();
        _visuals.Add((uid, _timing.CurTime + ev.Duration));
    }

    private void OnMovingVisual(RMCHallucinationMovingVisualEvent ev)
    {
        var from = GetCoordinates(ev.From);
        var to = GetCoordinates(ev.To);
        if (!from.IsValid(EntityManager) || !to.IsValid(EntityManager) || from.EntityId != to.EntityId)
            return;

        var appearAt = _timing.CurTime + ev.Delay;
        TrackOwner();
        _moving.Add(new MovingVisual(ev.Prototype, ev.State, from, to.Position, appearAt, ev.Travel, appearAt + ev.Travel + ev.Linger, ev.SpinSpeed));
    }

    public override void FrameUpdate(float frameTime)
    {
        if (_owner != null && _player.LocalEntity != _owner)
        {
            ClearAll();
            return;
        }

        var time = _timing.CurTime;
        UpdateDanger(time);
        UpdateXeno(time, frameTime);
        UpdateVisuals(time);
        UpdateBodies(time);
        UpdateHud(time);
        UpdateAttackers(time);
    }

    private void UpdateVisuals(TimeSpan time)
    {
        for (var i = _visuals.Count - 1; i >= 0; i--)
        {
            var (uid, end) = _visuals[i];
            if (time < end)
                continue;

            _visuals.RemoveAt(i);
            if (!TerminatingOrDeleted(uid))
                QueueDel(uid);
        }

        for (var i = _moving.Count - 1; i >= 0; i--)
        {
            var visual = _moving[i];
            if (time >= visual.RemoveAt)
            {
                _moving.RemoveAt(i);
                if (visual.Uid is { } removed)
                    QueueDel(removed);

                continue;
            }

            if (time < visual.AppearAt)
                continue;

            if (visual.Uid is not { } uid || Deleted(uid))
            {
                uid = Spawn(visual.Prototype, visual.From);
                visual.Uid = uid;
                if (visual.State != null && TryComp(uid, out SpriteComponent? movingSprite))
                    _sprite.LayerSetRsiState((uid, movingSprite), 0, visual.State);

                var delta = visual.To - visual.From.Position;
                if (delta != Vector2.Zero)
                    _transform.SetLocalRotation(uid, CardinalDirection(delta).ToAngle());
            }

            var progress = visual.Travel <= TimeSpan.Zero
                ? 1f
                : Math.Clamp((float) ((time - visual.AppearAt) / visual.Travel), 0f, 1f);
            _transform.SetLocalPosition(uid, Vector2.Lerp(visual.From.Position, visual.To, progress));

            if (visual.SpinSpeed != 0 && progress < 1f && TryComp(uid, out SpriteComponent? spinSprite))
            {
                var spun = (float) (time - visual.AppearAt).TotalSeconds * visual.SpinSpeed;
                _sprite.SetRotation((uid, spinSprite), new Angle(spun));
            }
        }
    }

    private void ClearAll()
    {
        foreach (var (uid, _) in _visuals)
        {
            if (!TerminatingOrDeleted(uid))
                QueueDel(uid);
        }

        foreach (var visual in _moving)
        {
            if (visual.Uid is { } uid)
                QueueDel(uid);
        }

        _visuals.Clear();
        _moving.Clear();

        ClearAttackers();
        ClearBodies();
        ClearDanger();
        ClearHud();
        ClearXeno();
        _owner = null;
    }

    private void TrackOwner()
    {
        _owner ??= _player.LocalEntity;
    }

    private void CopyName(EntityUid fake, EntityUid clone)
    {
        _metaData.SetEntityName(fake, Identity.Name(clone, EntityManager, _player.LocalEntity));
    }

    private static float FadeOut(TimeSpan time, TimeSpan start, TimeSpan fade)
    {
        if (fade <= TimeSpan.Zero)
            return 0;

        return 1f - Math.Clamp((float) ((time - start) / fade), 0f, 1f);
    }

    private static Direction CardinalDirection(Vector2 delta)
    {
        return MathF.Abs(delta.X) > MathF.Abs(delta.Y)
            ? delta.X > 0 ? Direction.East : Direction.West
            : delta.Y > 0 ? Direction.North : Direction.South;
    }

    private sealed class MovingVisual(
        EntProtoId prototype,
        string? state,
        EntityCoordinates from,
        Vector2 to,
        TimeSpan appearAt,
        TimeSpan travel,
        TimeSpan removeAt,
        float spinSpeed)
    {
        public readonly EntProtoId Prototype = prototype;
        public readonly string? State = state;
        public readonly EntityCoordinates From = from;
        public readonly Vector2 To = to;
        public readonly TimeSpan AppearAt = appearAt;
        public readonly TimeSpan Travel = travel;
        public readonly TimeSpan RemoveAt = removeAt;
        public readonly float SpinSpeed = spinSpeed;
        public EntityUid? Uid;
    }
}
