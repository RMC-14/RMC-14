using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Speech.Hushed;

/// <summary>
/// Component placed on the status effect entity itself to identify it as a Hushed effect.
/// Used to trigger adding/removing RMCHushedComponent on the target entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCHushedStatusEffectComponent : Component
{
}
