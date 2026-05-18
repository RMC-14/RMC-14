using Content.Client.Weather;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Weather;

public sealed class RMCWeatherScreenOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly WeatherSystem _weather = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new RMCWeatherFullscreenOverlay(
            EntityManager,
            _player,
            _weather,
            _map,
            _transform,
            _lookup,
            _timing,
            _prototypes));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<RMCWeatherFullscreenOverlay>();
    }
}
