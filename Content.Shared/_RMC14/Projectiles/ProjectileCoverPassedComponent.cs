using Content.Shared._RMC14.Barricade;

namespace Content.Shared._RMC14.Projectiles;

[RegisterComponent]
[Access(typeof(SharedDirectionalAttackBlockSystem))]
public sealed partial class ProjectileCoverPassedComponent : Component
{
    public HashSet<NetEntity> Barricades = new();
}
