using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Surgery.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMSurgeryToolComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? StartSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? EndSound;
}
