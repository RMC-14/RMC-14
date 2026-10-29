using Content.Client.Damage;
using Content.Shared._RMC14.Hallucination;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private static readonly EntProtoId BodyPrototype = "RMCHallucinationBody";
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    private static readonly HumanoidVisualLayers[] BodyParts =
    [
        HumanoidVisualLayers.Chest,
        HumanoidVisualLayers.Head,
        HumanoidVisualLayers.LArm,
        HumanoidVisualLayers.RArm,
        HumanoidVisualLayers.LLeg,
        HumanoidVisualLayers.RLeg,
    ];

    private readonly List<FakeBody> _bodies = new();
    private readonly List<FixedPoint2> _bruteThresholds = new();

    private void InitializeBodies()
    {
        SubscribeNetworkEvent<RMCHallucinationBodyEvent>(OnBody);
    }

    private void OnBody(RMCHallucinationBodyEvent ev)
    {
        var coords = GetCoordinates(ev.Coordinates);
        if (!coords.IsValid(EntityManager))
            return;

        TrackOwner();
        _bodies.Add(new FakeBody(ev.Id, Spawn(BodyPrototype, coords), ev.Clone, _timing.CurTime + ev.Duration));
    }

    private void CopyBody(FakeBody body)
    {
        if (body.Copied ||
            !TryGetEntity(body.Clone, out var clone) ||
            !HasComp<SpriteComponent>(clone) ||
            !TryComp(body.Uid, out SpriteComponent? sprite))
        {
            return;
        }

        var ent = (body.Uid, sprite);
        _sprite.CopySprite((clone.Value, null), ent);
        CopyName(body.Uid, clone.Value);
        _sprite.SetColor(ent, sprite.Color.WithAlpha(1f));
        _sprite.SetRotation(ent, Angle.FromDegrees(_random.Prob(0.5f) ? 90 : 270));
        body.Copied = true;

        _bruteThresholds.Clear();
        if (TryComp(clone, out DamageVisualsComponent? damageVisuals))
        {
            foreach (var threshold in damageVisuals.Thresholds)
            {
                if (threshold > FixedPoint2.Zero)
                    _bruteThresholds.Add(threshold);
            }
        }

        if (_bruteThresholds.Count == 0)
            return;

        foreach (var part in BodyParts)
        {
            if (!_sprite.LayerMapTryGet(ent, $"{part}{BruteGroup}", out var layer, false))
                continue;

            _sprite.LayerSetRsiState(ent, layer, $"{part}_{BruteGroup}_{_random.Pick(_bruteThresholds)}");
            _sprite.LayerSetVisible(ent, layer, true);
        }
    }

    private void UpdateBodies(TimeSpan time)
    {
        for (var i = _bodies.Count - 1; i >= 0; i--)
        {
            var body = _bodies[i];
            if (time >= body.End || Deleted(body.Uid))
            {
                _bodies.RemoveAt(i);
                QueueDel(body.Uid);
                continue;
            }

            CopyBody(body);
        }
    }

    private void ClearBodies()
    {
        foreach (var body in _bodies)
        {
            QueueDel(body.Uid);
        }

        _bodies.Clear();
    }

    private sealed class FakeBody(int id, EntityUid uid, NetEntity clone, TimeSpan end)
    {
        public readonly int Id = id;
        public readonly EntityUid Uid = uid;
        public readonly NetEntity Clone = clone;
        public readonly TimeSpan End = end;
        public bool Copied;
    }
}
