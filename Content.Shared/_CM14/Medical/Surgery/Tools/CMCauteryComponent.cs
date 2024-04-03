using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Surgery.Tools;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMCauteryComponent : Component, ICMSurgeryToolComponent
{
    public string ToolName => "a cautery";
}
