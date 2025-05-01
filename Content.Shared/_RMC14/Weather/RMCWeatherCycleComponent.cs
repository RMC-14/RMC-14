using Content.Shared.Weather;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weather;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWeatherSystem))]
public sealed partial class RMCWeatherCycleComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<RMCWeatherEventComponent> WeatherEvents = new();

    [DataField, AutoNetworkedField]
    public RMCWeatherEventComponent? CurrentEvent;

    [DataField, AutoNetworkedField]
    public TimeSpan MinTimeBetweenEvents;

    [DataField, AutoNetworkedField]
    public TimeSpan MinTimeVariance = TimeSpan.FromMinutes(10);

    [DataField, AutoNetworkedField]
    public TimeSpan LastEventCooldown;

    [DataDefinition]
    [Serializable, NetSerializable]
    public partial struct RMCWeatherEventComponent()
    {
        [DataField, AutoNetworkedField]
        public string Name = "rmcWeatherEvent";

        [DataField, AutoNetworkedField]
        public TimeSpan Duration;

        [DataField, AutoNetworkedField]
        public ProtoId<WeatherPrototype> WeatherType;

        [DataField, AutoNetworkedField]
        public float LightningChance = 0.0f;

        [DataField, AutoNetworkedField]
        public TimeSpan LightningDuration = TimeSpan.FromSeconds(2);

        [DataField, AutoNetworkedField]
        public TimeSpan LightningCooldown = TimeSpan.FromSeconds(5);

        [DataField, AutoNetworkedField]
        public List<string> LightningEffects = new(){"RMCColorSequenceLightningSharpPeak", "RMCColorSequenceLightningFlicker"};
    }
}


