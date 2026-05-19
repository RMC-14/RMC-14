using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Strain;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoStrainSystem))]
public sealed partial class XenoStrainComponent : Component, IComponentDebug
{
    [DataField(required: true), AutoNetworkedField]
    public LocId Name = string.Empty;

    [DataField, AutoNetworkedField]
    public LocId? Description;

    [DataField, AutoNetworkedField]
    public LocId? Popup;

    public string GetDebugString()
    {
        return $"Name: {Name}\r\nDesc: {Description}\r\nPopup: {Popup}";
    }
}
