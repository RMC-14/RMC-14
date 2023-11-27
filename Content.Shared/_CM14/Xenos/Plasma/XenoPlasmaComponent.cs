using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Xenos.Plasma;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoPlasmaSystem))]
public sealed partial class XenoPlasmaComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Plasma;

    [DataField(required: true), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxPlasma = 300;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PlasmaTransferDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier PlasmaTransferSound = new SoundCollectionSpecifier("XenoDrool");

    [DataField(required: true), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PlasmaRegenOnWeeds;
}
