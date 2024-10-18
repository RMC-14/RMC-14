using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Weapons.Ranged.Stacks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(GunStacksSystem))]
public sealed partial class GunStacksComponent : Component
{
    [DataField, AutoNetworkedField]
    public int IncreaseAP = 10;

    [DataField, AutoNetworkedField]
    public int MaxAP = 50;

    [DataField, AutoNetworkedField]
    public FixedPoint2 DamageIncrease = FixedPoint2.New(0.5);

    [DataField, AutoNetworkedField]
    public float SetFireRate = 1.4285f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastHitAt;

    [DataField, AutoNetworkedField]
    public TimeSpan StacksExpire = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public int Hits;
}
