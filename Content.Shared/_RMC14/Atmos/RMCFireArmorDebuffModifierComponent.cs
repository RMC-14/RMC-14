using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

/// <summary>
/// Modifies the armor debuff of stepping on fire
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCFireArmorDebuffModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DebuffModifier;
}
