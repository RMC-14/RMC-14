using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Surgery.Tools;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMHemostatComponent : Component, ICMSurgeryToolComponent
{
    public string ToolName => "a hemostat";
}
