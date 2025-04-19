using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Rest;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoRestSystem))]
public sealed partial class ActionBlockIfRestingComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId Popup = "rmc-xeno-rest-cant";
}
