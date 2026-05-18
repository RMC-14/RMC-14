using Content.Shared.Damage;
using Content.Shared.Weather;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
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
    public TimeSpan MinTimeBetweenEvents;

    [DataField, AutoNetworkedField]
    public TimeSpan WarnTime = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan MinTimeBetweenChecks = TimeSpan.FromMinutes(15);

    [DataField, AutoNetworkedField]
    public TimeSpan MinCheckVariance = TimeSpan.FromMinutes(10);

    [DataField, AutoNetworkedField]
    public float StartChance = 1;

    [DataField, AutoNetworkedField]
    public RMCWeatherCycleState State = RMCWeatherCycleState.Idle;

    [DataField, AutoNetworkedField]
    public RMCWeatherScreenOverlay CurrentScreenOverlay = RMCWeatherScreenOverlay.None;

    [DataField]
    public int? CurrentEventIndex;

    [DataField]
    public RMCWeatherEvent? ForcedEvent;

    [DataField]
    public bool AdminForcedEvent;

    [DataField]
    public TimeSpan CheckCooldown;

    [DataField]
    public TimeSpan WarningRemaining;

    [DataField]
    public TimeSpan EventRemaining;

    [DataField]
    public TimeSpan LightningCooldown;

    [DataField]
    public TimeSpan EffectCooldown;

    [DataField]
    public TimeSpan CleanCooldown;

    [DataField]
    public bool FirstDropComplete;

    [DataField]
    public TimeSpan EventStartedAt;

    [DataField]
    public int EventSequence;
}

[DataDefinition]
public sealed partial class RMCWeatherEvent
{
    [DataField]
    public string Name = "rmcWeatherEvent";

    [DataField]
    public string? DisplayName;

    [DataField]
    public TimeSpan Duration;

    [DataField]
    public ProtoId<WeatherPrototype> WeatherType;

    [DataField]
    public float LightningChance;

    [DataField]
    public TimeSpan LightningDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan LightningCooldownDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public List<string> LightningEffects = new() { "RMCColorSequenceLightningSharpPeak", "RMCColorSequenceLightningFlicker" };

    [DataField]
    public SoundSpecifier LightningSound = new SoundCollectionSpecifier("RMCThunder");

    [DataField]
    public int FireSmotheringStrength;

    [DataField]
    public bool CleansDecals = true;

    [DataField]
    public DamageSpecifier ExposureDamage = new();

    [DataField]
    public LocId? EffectMessage;

    [DataField]
    public float EffectMessageChance = 1f / 30f;

    [DataField]
    public RMCWeatherWarningMode WarningMode = RMCWeatherWarningMode.Default;

    [DataField]
    public RMCWeatherSirenKind? WarningSirenKind;

    [DataField]
    public SoundSpecifier? WarningSound;

    [DataField]
    public RMCWeatherScreenOverlay ScreenOverlay = RMCWeatherScreenOverlay.None;
}

[RegisterComponent]
[Access(typeof(RMCWeatherSystem))]
public sealed partial class RMCWeatherDilutedAcidComponent : Component
{
    [DataField]
    public int LastWeatherSequence;
}

[Serializable, NetSerializable]
public enum RMCWeatherCycleState : byte
{
    Idle,
    Warning,
    Running,
    Cooldown,
}

[Serializable, NetSerializable]
public enum RMCWeatherWarningMode : byte
{
    Default,
    SirenOnly,
    None,
}

[Serializable, NetSerializable]
public enum RMCWeatherScreenOverlay : byte
{
    None,
    Low,
    Medium,
    High,
}

public static class RMCWeatherScreenOverlayData
{
    public const float FullscreenTiles = 15f;

    public static RMCWeatherScreenOverlayRadii GetRadii(RMCWeatherScreenOverlay overlay)
    {
        return overlay switch
        {
            // CMSS13 uses 15x15 fullscreen states and stretches them to the client view.
            // RMC keeps the same idea, with a slightly stronger vertical frame for widescreen clients.
            RMCWeatherScreenOverlay.Low => new(13f, 11f, 13.4f, 11.6f),
            RMCWeatherScreenOverlay.Medium => new(11f, 9f, 11.5f, 9.6f),
            RMCWeatherScreenOverlay.High => new(9f, 7.5f, 9.5f, 8.1f),
            _ => new(FullscreenTiles, FullscreenTiles, FullscreenTiles, FullscreenTiles),
        };
    }

    public static float GetClearRange(RMCWeatherScreenOverlay overlay)
    {
        return GetRadii(overlay).ClearY;
    }
}

public readonly record struct RMCWeatherScreenOverlayRadii(float ClearX, float ClearY, float FullX, float FullY);

[ByRefEvent]
public readonly record struct RMCWeatherStartedEvent(MapId MapId, string Name, TimeSpan? Duration, bool AdminForced);

[ByRefEvent]
public readonly record struct RMCWeatherEndedEvent(MapId MapId, string Name, TimeSpan? Elapsed, bool AdminForced);
