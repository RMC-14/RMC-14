using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Tantrum;

/// <summary>
/// For an entity with this component, it gets a speed boost instead of an armor boost
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TantrumSpeedBuffComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedIncrease = 1.33f;
}
