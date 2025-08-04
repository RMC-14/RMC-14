using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.AcidBloodSplash;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(AcidBloodSplashSystem))]
public sealed partial class AcidBloodSplashComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 AcidSplashDamage;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MinimalTriggerDamage;

    [DataField, AutoNetworkedField]
    public float CloseSplashRadius = (float)(1 / Math.Sqrt(Math.PI)); // a circle equal in area to a square with a side of 1 (single tile)

    [DataField, AutoNetworkedField]
    public float StandardSplashRadius = (float)(3 / Math.Sqrt(Math.PI)); // a circle equal in area to a square with a side of 3 (3x3 tiles)

    [DataField, AutoNetworkedField]
    public float GibSplashRadius = (float)(5 / Math.Sqrt(Math.PI)); // a circle equal in area to a square with a side of 5 (5x5 tiles)

    // Substitution of gib chance
    // TODO: remove when xeno can be gibbed
    [DataField, AutoNetworkedField]
    public float BaseDeathSplashChance = 0.05f;

    [DataField, AutoNetworkedField]
    public TimeSpan SplashCooldown = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextSplashAvailable = new(0);
}
