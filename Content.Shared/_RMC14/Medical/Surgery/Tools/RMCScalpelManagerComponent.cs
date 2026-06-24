using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Surgery.Tools;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class RMCScalpelManagerComponent : Component, ICMSurgeryToolComponent
{
    public string ToolName => "an incision management system";
}