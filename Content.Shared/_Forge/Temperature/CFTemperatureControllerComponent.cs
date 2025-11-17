using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Forge.Temperature;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFTemperatureControllerComponent : Component
{
    /// <summary>
    /// Temperature zone that determines temperature parameters.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CFTemperatureZone Zone = CFTemperatureZone.Temperate;
}

[Serializable, NetSerializable]
public enum CFTemperatureZone : byte
{
    Temperate = 1,
    Desert = 2,
    Arctic = 3,
    Jungle = 4
}
