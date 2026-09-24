using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Autodoc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAutodocSystem))]
public sealed partial class AutodocResearchUpgradeComponent : Component
{
    /// <summary>
    /// Which surgery tier this upgrade unlocks.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public AutodocUpgradeTier Tier;
}

[Serializable, NetSerializable]
public enum AutodocUpgradeTier : byte
{
    InternalBleeding = 1,
    BrokenBone = 2,
    OrganDamage = 3,
    LarvaExtraction = 4,
}
