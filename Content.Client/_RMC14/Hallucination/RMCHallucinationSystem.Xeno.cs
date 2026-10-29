using System.Numerics;
using Content.Client._RMC14.IconSmoothing;
using Content.Client.IconSmoothing;
using Content.Shared._RMC14.Hallucination;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Sprite;
using Content.Shared.Stunnable;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private static readonly EntProtoId WeedsPrototype = "RMCHallucinationWeeds";
    private static readonly EntProtoId WeedNodePrototype = "RMCHallucinationWeedNode";

    private static readonly (Vector2i Offset, int Flag)[] CardinalFlagDirections =
    [
        (new Vector2i(0, 1), 1),
        (new Vector2i(0, -1), 2),
        (new Vector2i(1, 0), 4),
        (new Vector2i(-1, 0), 8),
    ];

    private readonly List<Disguise> _disguises = new();
    private readonly List<FaceVisual> _faces = new();
    private readonly List<Lurker> _lurkers = new();
    private readonly List<(EntityUid Uid, EntityUid Grid, RMCHallucinationParasiteChase Chase)> _parasites = new();
    private readonly List<WeedPatch> _weeds = new();

    private void InitializeXeno()
    {
        SubscribeNetworkEvent<RMCHallucinationDisguiseEvent>(OnDisguise);
        SubscribeNetworkEvent<RMCHallucinationFaceEvent>(OnFace);
        SubscribeNetworkEvent<RMCHallucinationLurkerEvent>(OnLurker);
        SubscribeNetworkEvent<RMCHallucinationParasiteEvent>(OnParasite);
        SubscribeNetworkEvent<RMCHallucinationWeedsEvent>(OnWeeds);
    }

    private void OnDisguise(RMCHallucinationDisguiseEvent ev)
    {
        TrackOwner();
        var disguise = new Disguise(ev.Target, ev.Xeno, _timing.CurTime + ev.Duration);
        for (var i = _disguises.Count - 1; i >= 0; i--)
        {
            var existing = _disguises[i];
            if (existing.Target != ev.Target)
                continue;

            if (existing.Xeno is { } xeno)
                QueueDel(xeno);

            disguise.OriginalAlpha = existing.OriginalAlpha;
            _disguises.RemoveAt(i);
        }

        _disguises.Add(disguise);
    }

    private void OnFace(RMCHallucinationFaceEvent ev)
    {
        if (_player.LocalEntity is not { } local)
            return;

        TrackOwner();
        var uid = SpawnAttachedTo(ev.Prototype, new EntityCoordinates(local, Vector2.Zero));
        var end = _timing.CurTime + ev.Duration;
        _faces.Add(new FaceVisual(uid, end - ev.Fade, ev.Fade, end));
    }

    private void OnLurker(RMCHallucinationLurkerEvent ev)
    {
        var coords = GetCoordinates(ev.Coordinates);
        if (!coords.IsValid(EntityManager) || _player.LocalEntity is not { } local)
            return;

        TrackOwner();
        var uid = Spawn(ev.Prototype, coords);
        var delta = _transform.GetWorldPosition(local) - _transform.GetWorldPosition(uid);
        if (delta != Vector2.Zero)
            _transform.SetWorldRotation(uid, CardinalDirection(delta).ToAngle());

        _lurkers.Add(new Lurker(uid, _timing.CurTime + ev.Duration, ev.VanishRange, ev.HoverRange, ev.Fade));
    }

    private void OnParasite(RMCHallucinationParasiteEvent ev)
    {
        var start = GetCoordinates(ev.Start);
        if (!start.IsValid(EntityManager) || !HasComp<MapGridComponent>(start.EntityId))
            return;

        TrackOwner();
        var uid = Spawn(ev.Prototype, start);
        var chase = new RMCHallucinationParasiteChase(start.Position, ev.RunSpeed, ev.LeapRange, ev.LeapDelay, ev.LeapTime, _timing.CurTime + ev.GiveUp);
        _parasites.Add((uid, start.EntityId, chase));
    }

    private void OnWeeds(RMCHallucinationWeedsEvent ev)
    {
        if (!TryGetEntity(ev.Grid, out var grid) || !HasComp<MapGridComponent>(grid))
            return;

        IconSmoothComponent? smooth = null;
        IconSmoothRandomComponent? smoothRandom = null;
        RandomSpriteComponent? randomSprite = null;
        if (_prototypes.TryIndex(ev.WeedsPrototype, out var weedsPrototype))
        {
            weedsPrototype.TryGetComponent(out smooth, _compFactory);
            weedsPrototype.TryGetComponent(out smoothRandom, _compFactory);
            weedsPrototype.TryGetComponent(out randomSprite, _compFactory);
        }

        if (smooth is { Mode: not IconSmoothingMode.CardinalFlags })
            smooth = null;

        TrackOwner();
        var time = _timing.CurTime;
        var patch = new WeedPatch(grid.Value, time + ev.FadeAt, ev.Fade, smooth, smoothRandom?.Overrides);
        for (var i = 0; i < ev.Tiles.Count && i < ev.Steps.Count; i++)
        {
            var tile = ev.Tiles[i];
            var variant = randomSprite == null ? null : PickRandomState(randomSprite);
            patch.Tiles[tile] = new FakeWeed(tile, time + ev.SpreadDelay * ev.Steps[i], tile == ev.Node, variant);
        }

        _weeds.Add(patch);
    }

    private void UpdateXeno(TimeSpan time, float frameTime)
    {
        UpdateDisguises(time);
        UpdateFaces(time);
        UpdateLurkers(time);
        UpdateParasites(time, frameTime);
        UpdateWeeds(time);
    }

    private void UpdateDisguises(TimeSpan time)
    {
        for (var i = _disguises.Count - 1; i >= 0; i--)
        {
            var disguise = _disguises[i];
            if (time >= disguise.End)
            {
                _disguises.RemoveAt(i);
                EndDisguise(disguise);
                continue;
            }

            UpdateDisguise(disguise);
        }
    }

    private void UpdateDisguise(Disguise disguise)
    {
        if (!TryGetEntity(disguise.Target, out var target) ||
            TerminatingOrDeleted(target) ||
            !TryComp(target, out SpriteComponent? sprite) ||
            Transform(target.Value).MapID == MapId.Nullspace)
        {
            if (disguise.Xeno is { } orphan)
                QueueDel(orphan);

            disguise.Xeno = null;
            return;
        }

        if (sprite.Color.A > 0)
        {
            disguise.OriginalAlpha ??= sprite.Color.A;
            _sprite.SetColor((target.Value, sprite), sprite.Color.WithAlpha(0));
        }

        EnsureComp<RMCHallucinationDisguisedComponent>(target.Value);

        if (disguise.Xeno is not { } xeno || Deleted(xeno) || Transform(xeno).ParentUid != target)
        {
            if (disguise.Xeno is { } stale)
                QueueDel(stale);

            xeno = SpawnAttachedTo(disguise.Prototype, new EntityCoordinates(target.Value, Vector2.Zero));
            disguise.Xeno = xeno;
            disguise.State = null;
        }

        var state = GetDisguiseState(target.Value);
        if (state == disguise.State || !TryComp(xeno, out SpriteComponent? xenoSprite))
            return;

        _sprite.LayerSetRsiState((xeno, xenoSprite), 0, state);
        disguise.State = state;
    }

    private string GetDisguiseState(EntityUid target)
    {
        if (TryComp(target, out MobStateComponent? mobState))
        {
            if (mobState.CurrentState == MobState.Dead)
                return "dead";

            if (mobState.CurrentState == MobState.Critical)
                return "crit";
        }

        if (HasComp<KnockedDownComponent>(target))
            return "crit";

        if (_standing.IsDown(target))
            return "sleeping";

        return "alive";
    }

    private void EndDisguise(Disguise disguise)
    {
        if (disguise.Xeno is { } xeno)
            QueueDel(xeno);

        if (!TryGetEntity(disguise.Target, out var target) || TerminatingOrDeleted(target))
            return;

        RemComp<RMCHallucinationDisguisedComponent>(target.Value);
        if (TryComp(target, out SpriteComponent? sprite))
            _sprite.SetColor((target.Value, sprite), sprite.Color.WithAlpha(disguise.OriginalAlpha ?? 1f));
    }

    private void UpdateFaces(TimeSpan time)
    {
        for (var i = _faces.Count - 1; i >= 0; i--)
        {
            var face = _faces[i];
            if (time >= face.End || !TryComp(face.Uid, out SpriteComponent? faceSprite))
            {
                _faces.RemoveAt(i);
                QueueDel(face.Uid);
                continue;
            }

            if (TryComp(Transform(face.Uid).ParentUid, out SpriteComponent? parentSprite))
                _sprite.SetRotation((face.Uid, faceSprite), parentSprite.Rotation);

            if (time >= face.FadeStart)
                _sprite.SetColor((face.Uid, faceSprite), faceSprite.Color.WithAlpha(FadeOut(time, face.FadeStart, face.Fade)));
        }
    }

    private void UpdateLurkers(TimeSpan time)
    {
        MapCoordinates? mouse = null;
        if (_lurkers.Count > 0)
            mouse = _eye.PixelToMap(_input.MouseScreenPosition);

        for (var i = _lurkers.Count - 1; i >= 0; i--)
        {
            var lurker = _lurkers[i];
            if (time >= lurker.End || !TryComp(lurker.Uid, out SpriteComponent? sprite))
            {
                _lurkers.RemoveAt(i);
                QueueDel(lurker.Uid);
                continue;
            }

            if (lurker.FadeStart is not { } fadeStart)
            {
                var position = _transform.GetMapCoordinates(lurker.Uid);
                var close = _player.LocalEntity is { } local &&
                            _transform.GetMapCoordinates(local) is var localPosition &&
                            localPosition.MapId == position.MapId &&
                            (localPosition.Position - position.Position).Length() < lurker.VanishRange;
                var hovered = mouse is { } mousePosition &&
                              mousePosition.MapId == position.MapId &&
                              (mousePosition.Position - position.Position).Length() < lurker.HoverRange;

                if (close || hovered)
                    lurker.FadeStart = time;

                continue;
            }

            var alpha = FadeOut(time, fadeStart, lurker.Fade);
            if (alpha <= 0)
            {
                _lurkers.RemoveAt(i);
                QueueDel(lurker.Uid);
                continue;
            }

            _sprite.SetColor((lurker.Uid, sprite), sprite.Color.WithAlpha(alpha));
        }
    }

    private void UpdateParasites(TimeSpan time, float frameTime)
    {
        for (var i = _parasites.Count - 1; i >= 0; i--)
        {
            var (uid, grid, chase) = _parasites[i];
            if (time >= chase.GiveUpAt ||
                _player.LocalEntity is not { } local ||
                Transform(local).GridUid != grid ||
                !TryComp(uid, out SpriteComponent? sprite))
            {
                _parasites.RemoveAt(i);
                QueueDel(uid);
                continue;
            }

            var target = Vector2.Transform(_transform.GetWorldPosition(local), _transform.GetInvWorldMatrix(grid));
            if (chase.Update(target, time, frameTime) || chase.Phase == RMCHallucinationParasitePhase.None)
            {
                _parasites.RemoveAt(i);
                QueueDel(uid);
                continue;
            }

            _transform.SetLocalPosition(uid, chase.Position);
            var delta = target - chase.Position;
            if (delta != Vector2.Zero)
                _transform.SetLocalRotation(uid, CardinalDirection(delta).ToAngle());

            _sprite.LayerSetRsiState((uid, sprite), 0, chase.Phase == RMCHallucinationParasitePhase.Leaping ? "thrown" : "running");
        }
    }

    private void UpdateWeeds(TimeSpan time)
    {
        for (var i = _weeds.Count - 1; i >= 0; i--)
        {
            var patch = _weeds[i];
            if (time >= patch.FadeAt + patch.Fade || !TryComp(patch.Grid, out MapGridComponent? grid))
            {
                _weeds.RemoveAt(i);
                DeleteWeeds(patch);
                continue;
            }

            var grown = false;
            foreach (var weed in patch.Tiles.Values)
            {
                if (weed.Uid != null || time < weed.AppearAt)
                    continue;

                var coords = _map.GridTileToLocal(patch.Grid, grid, weed.Tile);
                var uid = Spawn(weed.Node ? WeedNodePrototype : WeedsPrototype, coords);
                weed.Uid = uid;
                if (weed.Variant != null && TryComp(uid, out SpriteComponent? weedSprite))
                    _sprite.LayerSetRsiState((uid, weedSprite), 0, weed.Variant);

                grown = true;
            }

            if (grown)
                SmoothWeeds(patch, grid);

            if (time < patch.FadeAt)
                continue;

            var alpha = FadeOut(time, patch.FadeAt, patch.Fade);
            foreach (var weed in patch.Tiles.Values)
            {
                if (TryComp(weed.Uid, out SpriteComponent? sprite))
                    _sprite.SetColor((weed.Uid.Value, sprite), sprite.Color.WithAlpha(alpha));
            }
        }
    }

    private void SmoothWeeds(WeedPatch patch, MapGridComponent grid)
    {
        if (patch.Smooth is not { } smooth)
            return;

        foreach (var weed in patch.Tiles.Values)
        {
            if (weed.Uid is not { } uid || !TryComp(uid, out SpriteComponent? sprite))
                continue;

            var flags = 0;
            foreach (var (offset, flag) in CardinalFlagDirections)
            {
                if (HasSmoothNeighbor(patch, grid, smooth, weed.Tile + offset))
                    flags |= flag;
            }

            var state = $"{smooth.StateBase}{flags}";
            if (weed.Variant != null && patch.Overrides?.Contains(state) == true)
                state = weed.Variant;

            _sprite.LayerSetRsiState((uid, sprite), 0, state);
        }
    }

    private bool HasSmoothNeighbor(WeedPatch patch, MapGridComponent grid, IconSmoothComponent smooth, Vector2i tile)
    {
        if (patch.Tiles.TryGetValue(tile, out var fake) && fake.Uid != null)
            return true;

        var anchored = _map.GetAnchoredEntitiesEnumerator(patch.Grid, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (TryComp(uid, out IconSmoothComponent? other) &&
                other.Enabled &&
                other.SmoothKey != null &&
                (other.SmoothKey == smooth.SmoothKey || smooth.AdditionalKeys.Contains(other.SmoothKey)))
            {
                return true;
            }
        }

        return false;
    }

    private string? PickRandomState(RandomSpriteComponent random)
    {
        if (random.Available.Count == 0)
            return null;

        foreach (var states in _random.Pick(random.Available).Values)
        {
            if (states.Count > 0)
                return _random.Pick(states.Keys);
        }

        return null;
    }

    private void DeleteWeeds(WeedPatch patch)
    {
        foreach (var weed in patch.Tiles.Values)
        {
            if (weed.Uid is { } uid)
                QueueDel(uid);
        }
    }

    private void ClearXeno()
    {
        foreach (var disguise in _disguises)
        {
            EndDisguise(disguise);
        }

        foreach (var face in _faces)
        {
            QueueDel(face.Uid);
        }

        foreach (var lurker in _lurkers)
        {
            QueueDel(lurker.Uid);
        }

        foreach (var (uid, _, _) in _parasites)
        {
            QueueDel(uid);
        }

        foreach (var patch in _weeds)
        {
            DeleteWeeds(patch);
        }

        _disguises.Clear();
        _faces.Clear();
        _lurkers.Clear();
        _parasites.Clear();
        _weeds.Clear();
    }

    private sealed class Disguise(NetEntity target, EntProtoId prototype, TimeSpan end)
    {
        public readonly NetEntity Target = target;
        public readonly EntProtoId Prototype = prototype;
        public readonly TimeSpan End = end;
        public EntityUid? Xeno;
        public float? OriginalAlpha;
        public string? State;
    }

    private sealed record FaceVisual(EntityUid Uid, TimeSpan FadeStart, TimeSpan Fade, TimeSpan End);

    private sealed class Lurker(EntityUid uid, TimeSpan end, float vanishRange, float hoverRange, TimeSpan fade)
    {
        public readonly EntityUid Uid = uid;
        public readonly TimeSpan End = end;
        public readonly float VanishRange = vanishRange;
        public readonly float HoverRange = hoverRange;
        public readonly TimeSpan Fade = fade;
        public TimeSpan? FadeStart;
    }

    private sealed class WeedPatch(EntityUid grid, TimeSpan fadeAt, TimeSpan fade, IconSmoothComponent? smooth, HashSet<string>? overrides)
    {
        public readonly EntityUid Grid = grid;
        public readonly TimeSpan FadeAt = fadeAt;
        public readonly TimeSpan Fade = fade;
        public readonly IconSmoothComponent? Smooth = smooth;
        public readonly HashSet<string>? Overrides = overrides;
        public readonly Dictionary<Vector2i, FakeWeed> Tiles = new();
    }

    private sealed class FakeWeed(Vector2i tile, TimeSpan appearAt, bool node, string? variant)
    {
        public readonly Vector2i Tile = tile;
        public readonly TimeSpan AppearAt = appearAt;
        public readonly bool Node = node;
        public readonly string? Variant = variant;
        public EntityUid? Uid;
    }
}
