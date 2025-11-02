using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class FlareSignalComponent : Component
{

    /// <summary>
    /// The color used when the flare is activated under cover
    /// </summary>
    [DataField]
    public Color? SignalFailedColor = null;
}
