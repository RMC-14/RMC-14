
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship;

/// <summary>
///  A door designation that allows the Dropship Navigation Computer that is on the same grid
///  to lock said door
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Serializable, NetSerializable]
public sealed partial class LockableDropshipDoorComponent : Component
{
    /// <summary>
    /// The localized name that will show up on the Dropship Navigation Computer that may be chosen to lock 
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public LocId LocName;
}

