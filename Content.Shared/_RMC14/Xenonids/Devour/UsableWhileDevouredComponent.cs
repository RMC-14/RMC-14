using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Devour;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoDevourSystem))]
public sealed partial class UsableWhileDevouredComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanUnequip = false;

    [DataField, AutoNetworkedField]
    public float DamageMult = 0.625f;
}
