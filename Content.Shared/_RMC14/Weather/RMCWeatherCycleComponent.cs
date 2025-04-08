using Content.Shared._RMC14.OrbitalCannon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weather;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWeatherSystem))]
public sealed partial class RMCWeatherCycleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<RMCWeatherEventComponent> WeatherEvent;
}
