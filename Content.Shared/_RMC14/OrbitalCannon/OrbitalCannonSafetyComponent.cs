using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.OrbitalCannon;

/// <summary>
///     Round-global safety interlock for every orbital cannon.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(OrbitalCannonSystem))]
public sealed partial class OrbitalCannonSafetyComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Engaged;
}
