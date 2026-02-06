using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCOrbitalDeployableComponent : Component
{
    /// <summary>
    ///     The prototype to deploy, if this is null the entity itself will be deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? DeployPrototype;

    /// <summary>
    ///     Determines if the deployable falls slowly with a parachute or crashes to the floor.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UseParachute = true;

    /// <summary>
    ///     The maximum amount of times the prototype can be deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxDeployCount = 1;

    /// <summary>
    ///     The remaining amount of times the prototype can be deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int RemainingDeployCount = 1;

    /// <summary>
    ///     The effect to display at the landing location during the drop.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? LandingEffectId = "RMCEffectAlert";
}
