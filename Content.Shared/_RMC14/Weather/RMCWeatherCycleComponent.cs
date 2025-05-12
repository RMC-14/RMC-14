using Content.Shared.Weather;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weather;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWeatherSystem))]
public sealed partial class RMCWeatherCycleComponent : Component
{
    [DataField]
    public List<RMCWeatherEvent> WeatherEvents = new();

    [DataField, AutoNetworkedField]
    public RMCWeatherEvent? CurrentEvent;

    [DataField, AutoNetworkedField]
    public TimeSpan MinTimeBetweenEvents;

    [DataField, AutoNetworkedField]
    public TimeSpan MinTimeVariance = TimeSpan.FromMinutes(10);

    [DataField, AutoNetworkedField]
    public TimeSpan LastEventCooldown;

}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class RMCWeatherEvent
{
    [DataField]
    public string Name = "rmcWeatherEvent";

    [DataField]
    public TimeSpan Duration;

    [DataField]
    public ProtoId<WeatherPrototype> WeatherType;

    [DataField]
    public float LightningChance = 0.0f;

    [DataField]
    public TimeSpan LightningDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan LightningCooldown;

    [DataField]
    public TimeSpan LightningCooldownDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public List<string> LightningEffects = new(){"RMCColorSequenceLightningSharpPeak", "RMCColorSequenceLightningFlicker"};

    [DataField]
    public SoundSpecifier LightningSound = new SoundCollectionSpecifier("RMCThunder");
}


