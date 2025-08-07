using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.HUD.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HolocardContainerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Prefix = "bodybag";

    [DataField, AutoNetworkedField]
    public bool HideOnOpen = true;
}

[Serializable, NetSerializable]
public enum HolocardContainerVisualLayers : byte
{
    Base,
}

[Serializable, NetSerializable]
public enum HolocardContainerVisuals : byte
{
    State,
}
