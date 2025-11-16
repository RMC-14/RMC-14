using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Forge.Temperature;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TemperatureControllerComponent : Component
{
    /// <summary>
    /// Temperature zone that determines temperature parameters.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TemperatureZone Zone = TemperatureZone.Temperate;
}

[Serializable, NetSerializable]
public enum TemperatureZone : byte
{
    Temperate = 1,
    Desert = 2,
    Arctic = 3,
    Jungle = 4
}
