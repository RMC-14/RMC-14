using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class StunOnExplosionReceivedComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Weak;

    [DataField, AutoNetworkedField]
    public TimeSpan BlindTime = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public TimeSpan BlurTime = TimeSpan.FromSeconds(5);
}
