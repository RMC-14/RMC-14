using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weather;

/// <summary>
///     Physical map siren that plays local weather warnings instead of a map-wide fallback sound.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCWeatherSystem))]
public sealed partial class RMCWeatherSirenComponent : Component
{
    /// <summary>
    ///     Lets map events target separate CM siren networks, such as colony weather alarms and storm alarms.
    /// </summary>
    [DataField]
    public RMCWeatherSirenKind Kind = RMCWeatherSirenKind.Weather;

    /// <summary>
    ///     Sound played from the siren entity when its matching weather event enters warning.
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier WarningSound = default!;

    /// <summary>
    ///     Localized popup shown to nearby players; the nearest siren wins when multiple are in range.
    /// </summary>
    [DataField(required: true)]
    public LocId WarningMessage;
}

[Serializable, NetSerializable]
public enum RMCWeatherSirenKind : byte
{
    Weather,
    Storm,
}
