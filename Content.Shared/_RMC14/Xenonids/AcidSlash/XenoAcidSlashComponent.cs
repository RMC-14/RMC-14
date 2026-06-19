using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.AcidSlash;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoAcidSlashSystem))]
public sealed partial class XenoAcidSlashComponent : Component
{
    [DataField]
    public ProtoId<XenoAcidPrototype> Acid = "WeakAcid";
}
