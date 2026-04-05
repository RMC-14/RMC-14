using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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

    /// <summary>
    ///     The effect visual to show on the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TargetedEffects TargetType;

    /// <summary>
    ///     If the direction towards the targeting entity should be displayed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowDirection;

    [DataField]
    public ResPath RsiPath = new("/Textures/_RMC14/Effects/targeted.rsi");

    [DataField]
    public string LockOnState = "sniper_lockon";

    [DataField]
    public string LockOnStateDirection = "sniper_lockon_direction";

    [DataField]
    public string LockOnStateIntense = "sniper_lockon_intense";

    [DataField]
    public string LockOnStateIntenseDirection = "sniper_lockon_intense_direction";

    [DataField]
    public string SpotterState = "spotter_lockon";
}

[Serializable, NetSerializable]
public enum TargetedEffects : byte
{
    None = 0,
    Spotted,
    Targeted,
    TargetedIntense,
}
