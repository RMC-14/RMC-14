using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Devour;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoDevourSystem))]
public sealed partial class XenoDevourComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DevourDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string DevourContainerId = "cm_xeno_devour";

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier RegurgitateSound = new SoundCollectionSpecifier("XenoDrool");
}
