using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Rangefinder.Spotting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TargetedComponent : Component
{
    /// <summary>
    ///     The entities targeting the entity with this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> TargetedBy = new();
}

[Serializable, NetSerializable]
public enum TargetedVisuals
{
    Targeted,
}

[Serializable, NetSerializable]
public enum TargetedEffects
{
    None = 0,
    Spotted = 1,
    Targeted= 2,
    TargetedIntense = 3,
}
