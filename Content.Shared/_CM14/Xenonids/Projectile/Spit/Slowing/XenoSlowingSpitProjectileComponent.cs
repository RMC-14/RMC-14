using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenonids.Projectile.Spit.Slowing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoSpitSystem), typeof(XenoProjectileSystem))]
public sealed partial class XenoSlowingSpitProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Slow = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan Paralyze = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public bool ArmorResistsKnockdown = true;
}
