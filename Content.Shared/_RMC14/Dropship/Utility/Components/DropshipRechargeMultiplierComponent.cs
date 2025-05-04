using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DropshipUtilitySystem))]
public sealed partial class DropshipRechargeMultiplierComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Multiplier = 0.5f;
}
