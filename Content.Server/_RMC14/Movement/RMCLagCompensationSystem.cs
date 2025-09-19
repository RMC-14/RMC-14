using Content.Server.Movement.Systems;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Movement;
using Robust.Server.GameStates;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Movement;

public sealed class RMCLagCompensationSystem : SharedRMCLagCompensationSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly LagCompensationSystem _lagCompensation = default!;

    private bool _offsetBuffer;
    private int _maxBufferSize;
    private int _defaultBufferSize;
    private readonly Dictionary<NetUserId, int> _bufferSizes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RMCNetCVarsEvent>(OnNetCVars);

        Subs.CVar(_config,
            RMCCVars.RMCLagCompensationMilliseconds,
            v => _lagCompensation.BufferTime = TimeSpan.FromMilliseconds(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCLagCompensationOffsetBuffer,
            v => _offsetBuffer = v,
            true);

        Subs.CVar(_config,
            RMCCVars.RMCLagCompensationMaxBufferSize,
            v => _maxBufferSize = v,
            true);

        Subs.CVar(_config,
            RMCCVars.RMCLagCompensationDefaultBufferSize,
            v => _defaultBufferSize = v,
            true);
    }

    private void OnNetCVars(RMCNetCVarsEvent msg, EntitySessionEventArgs args)
    {
        _bufferSizes[args.SenderSession.UserId] = msg.BufferSize;
    }

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

    public int GetBufferSize(NetUserId session)
    {
        if (!_offsetBuffer)
            return 0;

        return Math.Min(_maxBufferSize, _bufferSizes.GetValueOrDefault(session, _defaultBufferSize));
    }
}
