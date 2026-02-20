using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._RMC14.Xenonids.Acid;

[RegisterComponent]
public sealed partial class RMCAcidCollisionWakeOverrideComponent : Component
{
    [DataField]
    public bool PreviousEnabled = true;
}
