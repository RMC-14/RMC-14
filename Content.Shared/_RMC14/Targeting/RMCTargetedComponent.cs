using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Targeting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCTargetedComponent : Component
{
    /// <summary>
    ///     The entities targeting the entity with this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> TargetedBy = new();

    /// <summary>
    ///     A dictionary storing the alpha multipliers for every laser and their originating entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, float> AlphaMultipliers = new();
}

[Serializable, NetSerializable]
public enum TargetedVisuals : byte
{
    Targeted,
    TargetedDirection,
    TargetedDirectionIntense,
}

[Serializable, NetSerializable]
public enum TargetedEffects : byte
{
    None = 0,
    Spotted,
    Targeted,
    TargetedIntense,
}

public enum DirectionTargetedEffects : byte
{
    None = 0,
    DirectionTargeted,
    DirectionTargetedIntense,
}
