using Content.Shared._RMC14.Barricade;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Projectiles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDirectionalAttackBlockSystem))]
public sealed partial class ProjectileCoverPassedComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<NetEntity> Barricades = new();
}
