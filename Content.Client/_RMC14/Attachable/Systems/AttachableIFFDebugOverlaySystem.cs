using System.Numerics;
using Content.Shared._RMC14.Attachable.Events;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Attachable.Systems;

public sealed class AttachableIFFDebugOverlaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private static readonly TimeSpan SampleLifetime = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MispredictCompareWindow = TimeSpan.FromMilliseconds(500);
    private const float MispredictPositionTolerance = 0.25f;

    private AttachableIFFDebugOverlay? _overlay;
    private IFFDebugSample? _clientSample;
    private IFFDebugSample? _serverSample;
    private bool _enabled;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableIFFDebugSampleEvent>(OnClientSample);
        SubscribeNetworkEvent<AttachableIFFDebugToggledEvent>(OnToggled);
        SubscribeNetworkEvent<AttachableIFFServerDebugSampleEvent>(OnServerSample);
    }

    public override void Shutdown()
    {
        if (_overlay != null)
        {
            _overlayManager.RemoveOverlay(_overlay);
            _overlay = null;
        }

        base.Shutdown();
    }

    private void OnToggled(AttachableIFFDebugToggledEvent ev)
    {
        _enabled = ev.Enabled;

        if (_enabled)
        {
            if (_overlay == null)
            {
                _overlay = new AttachableIFFDebugOverlay(this);
                _overlayManager.AddOverlay(_overlay);
            }
        }
        else
        {
            if (_overlay != null)
            {
                _overlayManager.RemoveOverlay(_overlay);
                _overlay = null;
            }

            _clientSample = null;
            _serverSample = null;
        }
    }

    private void OnClientSample(ref AttachableIFFDebugSampleEvent ev)
    {
        if (!_enabled || ev.IsServerSample)
            return;

        _clientSample = new IFFDebugSample(
            ev.From,
            ev.To,
            ev.HasHit,
            ev.Hit,
            ev.BlockedByFriendly,
            _timing.CurTime);
    }

    private void OnServerSample(AttachableIFFServerDebugSampleEvent ev)
    {
        if (!_enabled)
            return;

        var sample = new IFFDebugSample(
            ev.From,
            ev.To,
            ev.HasHit,
            ev.Hit,
            ev.BlockedByFriendly,
            _timing.CurTime);
        _serverSample = sample;

        TryLogMispredict(sample);
    }

    private void TryLogMispredict(IFFDebugSample server)
    {
        if (_clientSample is not { } client)
            return;

        if (server.Time - client.Time > MispredictCompareWindow)
            return;

        if (client.From.MapId != server.From.MapId ||
            client.To.MapId != server.To.MapId)
        {
            return;
        }

        if ((client.From.Position - server.From.Position).Length() > MispredictPositionTolerance ||
            (client.To.Position - server.To.Position).Length() > MispredictPositionTolerance)
        {
            return;
        }

        if (client.BlockedByFriendly == server.BlockedByFriendly &&
            client.HasHit == server.HasHit)
        {
            return;
        }

        Log.Warning(
            $"IFF mispredict detected: client blocked={client.BlockedByFriendly}, server blocked={server.BlockedByFriendly}, client hasHit={client.HasHit}, server hasHit={server.HasHit}");
    }

    public void Draw(in OverlayDrawArgs args)
    {
        if (!_enabled)
            return;

        var now = _timing.CurTime;
        DrawSample(args.WorldHandle, args.MapId, _clientSample, now, Color.DeepSkyBlue, Color.Red);
        DrawSample(args.WorldHandle, args.MapId, _serverSample, now, Color.Orange, Color.DarkRed);
    }

    private static void DrawSample(
        DrawingHandleWorld handle,
        MapId currentMap,
        IFFDebugSample? sample,
        TimeSpan now,
        Color lineColor,
        Color blockedColor)
    {
        if (sample is not { } value)
            return;

        if (now - value.Time > SampleLifetime || value.From.MapId != currentMap)
            return;

        var color = value.BlockedByFriendly ? blockedColor : lineColor;
        handle.DrawLine(value.From.Position, value.To.Position, color);
        handle.DrawCircle(value.From.Position, 0.08f, color);

        if (value.HasHit)
            handle.DrawCircle(value.Hit.Position, 0.12f, color, false);
    }

    private readonly record struct IFFDebugSample(
        MapCoordinates From,
        MapCoordinates To,
        bool HasHit,
        MapCoordinates Hit,
        bool BlockedByFriendly,
        TimeSpan Time);
}

public sealed class AttachableIFFDebugOverlay(AttachableIFFDebugOverlaySystem system) : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        system.Draw(args);
    }
}
