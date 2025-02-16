using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Strain;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoStrainSystem))]
public sealed partial class XenoStrainComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public LocId Name = string.Empty;

    [DataField, AutoNetworkedField]
    public LocId? Description;

    [DataField, AutoNetworkedField]
    public LocId? Popup;
}
