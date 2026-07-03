using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoSpitSystem))]
public sealed partial class XenoAcidOnHitComponent : Component
{
    [DataField]
    public EntProtoId Acid = "WeakAcid";

    [DataField]
    public TimeSpan ProlongDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public bool Enhance = false;

    [DataField]
    public int MaxTier = 1;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
