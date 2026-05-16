using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weather;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCWeatherSystem))]
public sealed partial class RMCWeatherSirenComponent : Component
{
    [DataField]
    public RMCWeatherSirenKind Kind = RMCWeatherSirenKind.Weather;

    [DataField(required: true)]
    public SoundSpecifier WarningSound = default!;

    [DataField(required: true)]
    public LocId WarningMessage;
}

[Serializable, NetSerializable]
public enum RMCWeatherSirenKind : byte
{
    Weather,
    Storm,
}
