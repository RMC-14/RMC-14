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
    /// <summary>
    ///     Map-authored weather events this cycle can choose from.
    /// </summary>
    [DataField]
    public List<RMCWeatherEvent> WeatherEvents = new();

    /// <summary>
    ///     Cooldown after an event ends before random checks can resume.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MinTimeBetweenEvents;

    /// <summary>
    ///     Warning window for sirens and faction announcements before the WeatherPrototype starts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan WarnTime = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Base delay between idle random weather rolls.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MinTimeBetweenChecks = TimeSpan.FromMinutes(15);

    /// <summary>
    ///     Random jitter applied around the base check delay so weather does not feel clockwork.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MinCheckVariance = TimeSpan.FromMinutes(10);

    /// <summary>
    ///     Chance that an idle check starts a configured weather event.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StartChance = 1;

    /// <summary>
    ///     Current lifecycle stage: idle, warning, running, or cooldown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCWeatherCycleState State = RMCWeatherCycleState.Idle;

    /// <summary>
    ///     Active fullscreen obstruction level; client overlay and examine range both read this value.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCWeatherScreenOverlay CurrentScreenOverlay = RMCWeatherScreenOverlay.None;

    /// <summary>
    ///     Index into WeatherEvents for normal randomly selected events.
    /// </summary>
    [DataField]
    public int? CurrentEventIndex;

    /// <summary>
    ///     Admin-started event payload, kept separate so commands can start events by copied data.
    /// </summary>
    [DataField]
    public RMCWeatherEvent? ForcedEvent;

    /// <summary>
    ///     Marks whether the active/queued event came from an admin command for audit announcements.
    /// </summary>
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

    /// <summary>
    ///     Prevents rain cleaning decals before the first dropship makes the ground map active.
    /// </summary>
    [DataField]
    public bool FirstDropComplete;

    [DataField]
    public TimeSpan EventStartedAt;

    /// <summary>
    ///     Increments whenever an event starts so one-shot effects can mark entities per storm.
    /// </summary>
    [DataField]
    public int EventSequence;
}

/// <summary>
///     Per-map configuration payload for one possible RMC weather event.
/// </summary>
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

    /// <summary>
    ///     Per-effect-tick strength for smothering fire and diluting exposed acid.
    /// </summary>
    [DataField]
    public int FireSmotheringStrength;

    [DataField]
    public bool CleansDecals = true;

    [DataField]
    public DamageSpecifier ExposureDamage = new();

    [DataField]
    public LocId? EffectMessage;

    /// <summary>
    ///     Random roll chance per effect tick before the per-mob popup cooldown is applied.
    /// </summary>
    [DataField]
    public float EffectMessageChance = 1f / 30f;

    [DataField]
    public RMCWeatherWarningMode WarningMode = RMCWeatherWarningMode.Default;

    /// <summary>
    ///     Physical siren network to target when the event uses siren-only CM warning behavior.
    /// </summary>
    [DataField]
    public RMCWeatherSirenKind? WarningSirenKind;

    [DataField]
    public SoundSpecifier? WarningSound;

    /// <summary>
    ///     CM fullscreen obstruction severity for local overlay, examine range, and click-through checks.
    /// </summary>
    [DataField]
    public RMCWeatherScreenOverlay ScreenOverlay = RMCWeatherScreenOverlay.None;
}

[RegisterComponent]
[Access(typeof(RMCWeatherSystem))]
public sealed partial class RMCWeatherDilutedAcidComponent : Component
{
    /// <summary>
    ///     Last event sequence that diluted this acid so it is not repeatedly scaled every effect tick.
    /// </summary>
    [DataField]
    public int LastWeatherSequence;
}

/// <summary>
///     Per-mob cooldown for random weather flavor popups.
/// </summary>
[RegisterComponent]
[Access(typeof(RMCWeatherSystem))]
public sealed partial class RMCWeatherEffectPopupCooldownComponent : Component
{
    public TimeSpan NextPopupAt;
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

/// <summary>
///     Converts weather severity into the shared clear/full radii used by drawing and examine checks.
/// </summary>
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
