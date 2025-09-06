using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Movement;

public abstract class SharedRMCLagCompensationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public float InteractionMarginTiles { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config, RMCCVars.RMCLagCompensationInteractionMarginTiles, v => InteractionMarginTiles = v, true);
    }

    public virtual (EntityCoordinates Coordinates, Angle Angle) GetCoordinatesAngle(EntityUid uid,
        ICommonSession? pSession,
        TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return (EntityCoordinates.Invalid, Angle.Zero);

        return (xform.Coordinates, xform.LocalRotation);
    }

    public virtual Angle GetAngle(EntityUid uid, ICommonSession? session, TransformComponent? xform = null)
    {
        var (_, angle) = GetCoordinatesAngle(uid, session, xform);
        return angle;
    }

    public virtual EntityCoordinates GetCoordinates(EntityUid uid,
        ICommonSession? session,
        TransformComponent? xform = null)
    {
        var (coordinates, _) = GetCoordinatesAngle(uid, session, xform);
        return coordinates;
    }

    public bool IsWithin(Entity<TransformComponent?> ent, EntityCoordinates coordinates, ICommonSession? session, float range)
    {
        return _transform.InRange(GetCoordinates(ent, session), coordinates, range);
    }
}
