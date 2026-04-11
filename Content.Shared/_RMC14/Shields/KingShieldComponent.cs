using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Shields;

[RegisterComponent, NetworkedComponent]
public sealed partial class KingShieldComponent : Component
{
    [DataField]
    public float MaxDamagePercent = 0.1f;
}
