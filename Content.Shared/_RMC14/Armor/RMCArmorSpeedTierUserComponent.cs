using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMArmorSystem))]
public sealed partial class RMCArmorSpeedTierUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? SpeedTier;
}
