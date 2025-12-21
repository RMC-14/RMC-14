using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.OrbitalCannon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OrbitalCannonSystem))]
public sealed partial class OrbitalCannonTrayComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedCannon;

    [DataField, AutoNetworkedField]
    public string? WarheadType;

    [DataField, AutoNetworkedField]
    public int FuelAmount;
}

[Serializable, NetSerializable]
public enum OrbitalCannonTrayVisuals
{
    Warhead,
    Fuel,
}
