using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor.ThermalCloak;

[RegisterComponent, NetworkedComponent]
public sealed partial class CancelUseWithCloakComponent : Component
{
    [DataField]
    public string CancelMessage = "rmc-cloak-attempt-prime";
}
