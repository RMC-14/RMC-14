using Content.Server.Movement.Systems;
using Content.Shared._RMC14.Movement;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Movement;

public sealed class RMCLagCompensationSystem : SharedRMCLagCompensationSystem
{
    [Dependency] private readonly LagCompensationSystem _lagCompensation = default!;

    public override (EntityCoordinates Coordinates, Angle Angle) GetCoordinatesAngle(EntityUid uid,
        ICommonSession? pSession,
        TransformComponent? xform = null)
    {
        return _lagCompensation.GetCoordinatesAngle(uid, pSession, xform);
    }

    public override Angle GetAngle(EntityUid uid, ICommonSession? session, TransformComponent? xform = null)
    {
        return _lagCompensation.GetAngle(uid, session, xform);
    }

    public override EntityCoordinates GetCoordinates(EntityUid uid, ICommonSession? session, TransformComponent? xform = null)
    {
        return _lagCompensation.GetCoordinates(uid, session, xform);
    }
}
