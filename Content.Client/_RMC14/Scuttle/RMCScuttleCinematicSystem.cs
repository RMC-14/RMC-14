using Content.Shared._RMC14.Rules;
using Content.Shared.GameTicking;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Scuttle;

public sealed class RMCScuttleCinematicSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private RMCScuttleCinematicOverlay? _current;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RMCScuttleCinematicEvent>(OnScuttleCinematic);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_current?.Finished == true)
            Clear();
    }

    private void OnScuttleCinematic(RMCScuttleCinematicEvent ev)
    {
        Clear();

        if (ev.Duration <= TimeSpan.Zero)
            return;

        _current = new RMCScuttleCinematicOverlay(_timing.CurTime, ev.Duration);
        _overlay.AddOverlay(_current);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        Clear();
    }

    private void Clear()
    {
        if (_current == null)
            return;

        _overlay.RemoveOverlay<RMCScuttleCinematicOverlay>();
        _current = null;
    }
}
