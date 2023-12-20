using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CM14.Xenos.Projectile;

public abstract class SharedXenoProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public bool TryShoot(
        EntityUid xeno,
        EntityCoordinates targetCoords,
        FixedPoint2 plasma,
        EntProtoId projectileId,
        SoundSpecifier? sound,
        int shots,
        Angle deviation,
        float speed)
    {
        var origin = _transform.GetMapCoordinates(xeno);
        var target = targetCoords.ToMap(EntityManager, _transform);

        if (origin.MapId != target.MapId ||
            origin.Position == target.Position)
        {
            return false;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno, plasma))
            return false;

        _audio.PlayPredicted(sound, xeno, xeno);

        var diff = target.Position - origin.Position;

        for (var i = 0; i < shots; i++)
        {
            if (deviation != Angle.Zero)
            {
                var angle = _random.NextAngle(-deviation / 2, deviation / 2);
                target = new MapCoordinates(origin.Position + angle.RotateVec(diff), target.MapId);
            }

            Shoot(xeno, projectileId, speed, origin, target);
        }

        return true;
    }

    protected virtual void Shoot(EntityUid xeno, EntProtoId projectileId, float speed, MapCoordinates origin, MapCoordinates target)
    {
    }
}
