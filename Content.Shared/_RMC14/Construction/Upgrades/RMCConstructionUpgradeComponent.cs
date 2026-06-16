using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction.Upgrades;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCUpgradeSystem))]
public sealed partial class RMCConstructionUpgradeComponent : Component
{
    /// <summary>
    /// The material that is consumed when the upgrade is used (If null, nothing will be consumed)
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<StackPrototype>? Material = "CMSteel";

    [DataField, AutoNetworkedField]
    public int Amount = 2;

    [DataField, AutoNetworkedField]
    public EntProtoId UpgradedEntity;

    [DataField, AutoNetworkedField]
    public EntProtoId BaseEntity;

    [DataField, AutoNetworkedField]
    public LocId UpgradedPopup;

    [DataField, AutoNetworkedField]
    public LocId FailurePopup = "rmc-construction-no-metal";
}
