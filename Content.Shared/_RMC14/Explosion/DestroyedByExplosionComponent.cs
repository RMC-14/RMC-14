using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class DestroyedByExplosionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsExplodable = true;

    [DataField, AutoNetworkedField]
    public FixedPoint2 LowIntensityThreshold = 200;

    [DataField, AutoNetworkedField]
    public FixedPoint2 HighIntensityThreshold = 400;

    [DataField, AutoNetworkedField]
    public float LowIntensityDestroyChance= 0.05f;

    [DataField, AutoNetworkedField]
    public float MediumIntensityDestroyChance = 0.5f;

    [DataField, AutoNetworkedField]
    public float HighIntensityDestroyChance = 1f;
}
