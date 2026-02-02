using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Gibbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCGibOnDeathComponent : Component
{
    /// <summary>
    /// Chance to gib on death. Set to 0 if you do not want this mob to gib on death.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GibChance = 0.05f;

    /// <summary>
    /// Substitution of coefficient that increases chance of gib depending on health after death
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageGibMultiplier = 0.005f; // * 0.01 to get probability, / 2 by parity

    /// <summary>
    /// If organs should be dropped on gibbing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DropOrgans = false;
}
