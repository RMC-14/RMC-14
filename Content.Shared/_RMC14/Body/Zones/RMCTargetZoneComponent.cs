using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Body.Zones;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCTargetZoneSystem))]
public sealed partial class RMCTargetZoneComponent : Component
{
    // Nobody else needs to know where a player is aiming, so it is only networked to its owner.
    public override bool SendOnlyToOwner => true;

    [DataField, AutoNetworkedField]
    public RMCTargetZone Selected = RMCTargetZone.Chest;
}
