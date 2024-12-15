using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Content.Shared._RMC14.Item;

namespace Content.Shared._RMC14.Rules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCPlanetSystem))]
public sealed partial class RMCPlanetMapPrototypeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ResPath Map;

    [DataField, AutoNetworkedField]
    public CamouflageType Camouflage = CamouflageType.Jungle;

    [DataField(required: true), AutoNetworkedField]
    public string Announcement = string.Empty;
}
