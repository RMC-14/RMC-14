using System.Numerics;
using Content.Shared._RMC14.Hallucination;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private static readonly EntProtoId AttackerPrototype = "RMCHallucinationAttacker";
    private static readonly Angle DefaultSwingAngle = Angle.FromDegrees(60);
    private static readonly Angle CollapseRotation = Angle.FromDegrees(90);

    private const float MaxExtrapolation = 0.3f;
    private const float BlendTime = 0.1f;
    private const float DefaultSwingRange = 1.5f;
    private const float SwingBuffer = 0.2f;

    private readonly Dictionary<int, Attacker> _attackers = new();

    private void InitializeAttacker()
    {
        SubscribeNetworkEvent<RMCHallucinationAttackerSpawnEvent>(OnAttackerSpawn);
        SubscribeNetworkEvent<RMCHallucinationAttackerMoveEvent>(OnAttackerMove);
        SubscribeNetworkEvent<RMCHallucinationAttackerSwingEvent>(OnAttackerSwing);
        SubscribeNetworkEvent<RMCHallucinationAttackerCollapseEvent>(OnAttackerCollapse);
        SubscribeNetworkEvent<RMCHallucinationAttackerRemoveEvent>(OnAttackerRemove);

        SubscribeLocalEvent<RMCHallucinationAttackerComponent, AttackedEvent>(OnAttackerAttacked);
    }

    private void OnAttackerSpawn(RMCHallucinationAttackerSpawnEvent ev)
    {
        var coords = GetCoordinates(ev.Coordinates);
        if (!coords.IsValid(EntityManager))
            return;

        if (_attackers.Remove(ev.Id, out var existing))
            QueueDel(existing.Uid);

        var uid = Spawn(AttackerPrototype, coords);
        EnsureComp<RMCHallucinationAttackerComponent>(uid).Id = ev.Id;

        TrackOwner();
        _attackers[ev.Id] = new Attacker(uid, ev.Clone)
        {
            Parent = coords.EntityId,
            ServerPosition = coords.Position,
            BlendFrom = coords.Position,
            ReceivedAt = _timing.CurTime,
        };
    }

    private void OnAttackerMove(RMCHallucinationAttackerMoveEvent ev)
    {
        if (!_attackers.TryGetValue(ev.Id, out var attacker) || Deleted(attacker.Uid))
            return;

        var coords = GetCoordinates(ev.Coordinates);
        if (!coords.IsValid(EntityManager))
            return;

        var current = Transform(attacker.Uid).Coordinates;
        attacker.BlendFrom = current.EntityId == coords.EntityId ? current.Position : coords.Position;
        attacker.Parent = coords.EntityId;
        attacker.ServerPosition = coords.Position;
        attacker.Velocity = ev.Velocity;
        attacker.ReceivedAt = _timing.CurTime;
        _transform.SetLocalRotation(attacker.Uid, ev.Direction.ToAngle());
    }

    private void OnAttackerSwing(RMCHallucinationAttackerSwingEvent ev)
    {
        if (!_attackers.TryGetValue(ev.Id, out var attacker) ||
            Deleted(attacker.Uid) ||
            _player.LocalEntity is not { } local)
        {
            return;
        }

        var weapon = TryGetEntity(ev.Weapon, out var held) && !TerminatingOrDeleted(held) ? held.Value : attacker.Uid;
        var angle = DefaultSwingAngle;
        var range = DefaultSwingRange;
        if (TryComp(weapon, out MeleeWeaponComponent? melee))
        {
            angle = melee.Angle;
            range = melee.Range;
        }

        var xform = Transform(attacker.Uid);
        var localPos = Vector2.Transform(_transform.GetWorldPosition(local), _transform.GetInvWorldMatrix(xform));
        if (localPos.LengthSquared() <= 0f)
            return;

        localPos = xform.LocalRotation.RotateVec(localPos);
        var visualLength = range - SwingBuffer;
        if (localPos.Length() > visualLength)
            localPos = localPos.Normalized() * visualLength;

        _melee.DoLunge(attacker.Uid, weapon, angle, localPos, ev.Animation?.Id);
    }

    private void OnAttackerCollapse(RMCHallucinationAttackerCollapseEvent ev)
    {
        if (!_attackers.TryGetValue(ev.Id, out var attacker) || !TryComp(attacker.Uid, out SpriteComponent? sprite))
            return;

        _sprite.SetRotation((attacker.Uid, sprite), CollapseRotation);
        attacker.FadeStart = _timing.CurTime;
        attacker.Fade = ev.Fade;
    }

    private void OnAttackerRemove(RMCHallucinationAttackerRemoveEvent ev)
    {
        if (!_attackers.Remove(ev.Id, out var attacker))
            return;

        QueueDel(attacker.Uid);
    }

    private void OnAttackerAttacked(Entity<RMCHallucinationAttackerComponent> ent, ref AttackedEvent args)
    {
        if (!_timing.IsFirstTimePredicted ||
            args.User != _player.LocalEntity ||
            args.Used == args.User)
        {
            return;
        }

        RaiseNetworkEvent(new RMCHallucinationAttackerHitEvent(ent.Comp.Id, GetNetEntity(args.Used)));
    }

    private void UpdateAttackers(TimeSpan time)
    {
        foreach (var attacker in _attackers.Values)
        {
            if (Deleted(attacker.Uid) || !TryComp(attacker.Uid, out SpriteComponent? sprite))
                continue;

            if (!attacker.Copied && TryGetEntity(attacker.Clone, out var clone) && HasComp<SpriteComponent>(clone))
            {
                _sprite.CopySprite((clone.Value, null), (attacker.Uid, sprite));
                CopyName(attacker.Uid, clone.Value);
                if (attacker.FadeStart != null)
                    _sprite.SetRotation((attacker.Uid, sprite), CollapseRotation);

                attacker.Copied = true;
            }

            var elapsed = (float) (time - attacker.ReceivedAt).TotalSeconds;
            var predicted = attacker.ServerPosition + attacker.Velocity * MathF.Min(elapsed, MaxExtrapolation);
            var blend = MathF.Min(1f, elapsed / BlendTime);
            var position = predicted + (attacker.BlendFrom - attacker.ServerPosition) * (1f - blend);
            if (Transform(attacker.Uid).ParentUid == attacker.Parent)
                _transform.SetLocalPosition(attacker.Uid, position);
            else
                _transform.SetCoordinates(attacker.Uid, new EntityCoordinates(attacker.Parent, position));

            if (attacker.FadeStart is { } fadeStart)
                _sprite.SetColor((attacker.Uid, sprite), sprite.Color.WithAlpha(FadeOut(time, fadeStart, attacker.Fade)));
        }
    }

    private void ClearAttackers()
    {
        foreach (var attacker in _attackers.Values)
        {
            QueueDel(attacker.Uid);
        }

        _attackers.Clear();
    }

    private sealed class Attacker(EntityUid uid, NetEntity clone)
    {
        public readonly EntityUid Uid = uid;
        public readonly NetEntity Clone = clone;
        public bool Copied;
        public EntityUid Parent;
        public Vector2 ServerPosition;
        public Vector2 Velocity;
        public Vector2 BlendFrom;
        public TimeSpan ReceivedAt;
        public TimeSpan? FadeStart;
        public TimeSpan Fade;
    }
}
