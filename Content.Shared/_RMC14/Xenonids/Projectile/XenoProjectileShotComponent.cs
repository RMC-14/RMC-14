using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Projectile;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoProjectileSystem))]
public sealed partial class XenoProjectileShotComponent : Component
{
    public ICommonSession? Shooter;

    [DataField, AutoNetworkedField]
    public int Id;

    [DataField, AutoNetworkedField]
    public EntityUid? ShooterEnt;
}
