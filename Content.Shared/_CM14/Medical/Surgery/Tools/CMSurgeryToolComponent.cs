using Content.Shared.Body.Part;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Surgery.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMSurgeryToolComponent : Component
{
    [DataField, AutoNetworkedField]
    public BodyPartType? Target;
}
