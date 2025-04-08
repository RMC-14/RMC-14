using Content.Shared.Weather;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weather;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWeatherSystem))]
public sealed partial class RMCWeatherCycleComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntProtoId<RMCWeatherEventComponent>> WeatherEvent;

    [DataField, AutoNetworkedField]
    public TimeSpan MinTimeBetweenEvents;

    [DataField, AutoNetworkedField]
    public TimeSpan LastEventCooldown;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWeatherSystem))]
public sealed partial class RMCWeatherEventComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Name;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration;

    [DataField, AutoNetworkedField]
    public ProtoId<WeatherPrototype> WeatherType;
}

