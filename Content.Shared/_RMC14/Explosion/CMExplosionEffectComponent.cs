using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class CMExplosionEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? Explosion = "CMExplosionEffectGrenade";

    [DataField, AutoNetworkedField]
    public EntProtoId? ShockWave = "RMCExplosionEffectGrenadeShockWave";

    [DataField, AutoNetworkedField]
    public List<EntProtoId> ShrapnelEffects = new() { "CMExplosionEffectShrapnel1", "CMExplosionEffectShrapnel2" };

    [DataField, AutoNetworkedField]
    public int MinShrapnel = 5;

    [DataField, AutoNetworkedField]
    public int MaxShrapnel = 9;

    [DataField, AutoNetworkedField]
    public float ShrapnelSpeed = 5;
}
